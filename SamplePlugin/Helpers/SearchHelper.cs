using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;

namespace Wordsmith.Helpers
{
    public class SearchHelper : IDisposable
    {
        protected Plugin Plugin;
        public bool Loading { get; private set; } = false;

        protected Data.WordSearchResult? _result;
        public Data.WordSearchResult? Result => _result;

        protected List<Data.WordSearchResult> _history = new List<Data.WordSearchResult>();
        protected void AddHistoryEntry(Data.WordSearchResult entry)
        {
            // Add the latest to the history
            _history.Add(entry);

            // If over allowed amount remove oldest.
            if (_history.Count > Plugin.Configuration.SearchHistoryCount)
                _history.Remove(_history.Last());
        }

        public void DeleteResult(Data.WordSearchResult? entry)
        {
            if (entry == null)
                return;

            if (_result == entry)
                _result = null;

            else if (_history.Contains(entry))
                _history.Remove(entry);
        }
        public Data.WordSearchResult[] History => _history.ToArray();

        protected HttpClient _client;

        public SearchHelper(Plugin plugin)
        {
            _client = new HttpClient();
            Plugin = plugin;
        }

        protected int InHistory(string query)
        {
            for(int i=0; i<History.Length; ++i)
            {
                if (History[i].Query.ToLower() == query.ToLower())
                    return i;
            }
            return -1;
        }
        public async void SearchThesaurus(string query)
        {
            try
            {
                // If searching the same thing twice, return
                if (_result?.Query.ToLower() == query.ToLower())
                    return;

                // Check if current query is in the history
                int idx = InHistory(query);

                // If a match is found
                if (idx > -1)
                {

                    // If the user doesn't want to move results to the top return
                    if (!Plugin.Configuration.ResearchToTop)
                        return;

                    // If the last search was not null or an error
                    if (_result != null && !_result.SearchError)
                    {
                        // Move the last search to the history
                        _history.Insert(0, _result);

                        // Move the returned index down by one.
                        ++idx;
                    }

                    // Get the resulting history item
                    _result = _history[idx];
                    _history.RemoveAt(idx);
                    return;
                }
            }
            catch (Exception ex)
            {
                PluginUI.Alert?.AppendMessage(ex.Message);
            }
            Loading = true;
            if (_result != null && !_result.SearchError)
                _history.Insert(0, _result);

            try
            {
                // Get the HTMl as a string
                var html = await _client.GetStringAsync("https://www.merriam-webster.com/thesaurus/" + query);

                // Convert it to a document.
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Remove scripts
                doc.DocumentNode.Descendants()
                    .Where(node => node.Name == "script")
                    .ToList()
                    .ForEach(node => node.Remove());

                // Empty out any previous results.
                Data.WordSearchResult result = new Data.WordSearchResult(query);

                // Get the Word variants;
                List<HtmlNode> variants = doc.DocumentNode.SelectNodes("//div[@class='row entry-header thesaurus']").ToList();

                // Get the thesaurus entries
                List<HtmlNode> entrynodes = doc.DocumentNode.SelectNodes("//div[@class='thesaurus-entry']").ToList();

                PluginUI.Alert?.NewMessage($"{variants.Count}|{entrynodes.Count}");
                for (int i = 0; i < variants.Count; ++i)
                {
                    PluginUI.Alert?.AppendMessage($"Attempting variant {i}");
                    Data.ThesaurusEntry tEntry = new Data.ThesaurusEntry();

                    tEntry.Word = query;

                    // Get the variant's word type
                    tEntry.Type = variants[i].SelectSingleNode(".//a").InnerText;
                    
                    tEntry.Definition = entrynodes[i].SelectSingleNode(".//span[@class='dt ']/text()")?.InnerText.Trim() ?? "";
                    if (tEntry.Definition == "as in")
                    {
                        string[] asin = entrynodes[i].SelectSingleNode(".//span[@class='dt ']").SelectNodes(".//em").ToList().Select(n => n.InnerText).ToArray();
                        tEntry.Definition += $" {string.Join(", ", asin)}.";
                    }
                    PluginUI.Alert?.AppendMessage($"Done with definition {{{tEntry.Definition}}}");

                    // Get the synonym list
                    HtmlNode worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list syn-list']");
                    if (worker != null)
                        tEntry.AddSynonyms(worker.SelectNodes(".//a").Select(n => n.InnerText).ToArray());
                    PluginUI.Alert?.AppendMessage($"Done with synonyms {{{tEntry.SynonymString}}}");

                    // Get the related words list
                    worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list rel-list']");
                    if (worker != null)
                        tEntry.AddRelatedWords(worker.SelectNodes(".//a").Select(n => n.InnerText).ToArray());
                    else
                    {
                        // Check if the page uses the "Similar" list.
                        worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list sim-list']");
                        if (worker != null)
                            tEntry.AddSynonyms(worker.SelectNodes(".//a").Select(n => n.InnerText).ToArray());
                    }
                    PluginUI.Alert?.AppendMessage($"Done with related words {{{tEntry.RelatedString}}}");

                    // Get the near antonyms list
                    worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list near-list']");
                    if (worker != null)
                        tEntry.AddNearAntonyms(worker.SelectNodes(".//a").Select(n => n.InnerText).ToArray());
                    else
                    {
                        // Check if the page uses the "Opposites" list.
                        worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list opp-list']");
                        if (worker != null)
                            tEntry.AddNearAntonyms(worker.SelectNodes(".//a").Select(n => n.InnerText).ToArray());
                    }
                    PluginUI.Alert?.AppendMessage($"Done with near antonyms {{{tEntry.NearAntonymString}}}");

                    // Get the antonyms list
                    worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list ant-list']");
                    if (worker != null)
                        tEntry.AddAntonyms(
                            worker.SelectNodes(".//a")?.Select(n => n.InnerText).ToArray() ??
                            worker.SelectSingleNode(".//ul[@class='mw-list']").SelectNodes(".//span").Select(n => n.InnerText).ToArray());

                    PluginUI.Alert?.AppendMessage($"Done with antonyms {{{tEntry.AntonymString}}}");

                    // This is a comment that shows the debug window
                    PluginUI.Alert?.AppendMessage($"{tEntry.Type}\n{tEntry.Definition}\n\nSynonyms:\n{tEntry.SynonymString}\n\nRelated Words:\n{tEntry.RelatedString}\n\nNear Antonyms:\n{tEntry.NearAntonymString}\n\nAntonyms:\n{tEntry.AntonymString}");
                    result.AddEntry(tEntry);

                    _result = result;
                }
            }
            catch (HttpRequestException ex)
            {
                _result = new Data.WordSearchResult(query) { Exception = new Exception($"{query} was not found in the thesaurus.") };
            }
            catch (Exception ex)
            {
                _result = new Data.WordSearchResult(query) { Exception = ex};
            }

            Loading = false;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
