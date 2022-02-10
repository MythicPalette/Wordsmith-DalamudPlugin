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
        public bool Loading { get; private set; } = false;
        protected float _progress = 0.0f;
        public float Progress => _progress;

        protected Data.WordSearchResult? _result;
        public Data.WordSearchResult? Result => _result;

        protected List<Data.WordSearchResult> _history = new List<Data.WordSearchResult>();
        protected void AddHistoryEntry(Data.WordSearchResult entry)
        {
            // Add the latest to the history
            _history.Insert(0, entry);

            // If over allowed amount remove oldest.
            while (_history.Count >= Wordsmith.Configuration.SearchHistoryCount)
                _history.RemoveAt(_history.Count - 1);
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

        public SearchHelper()
        {
            _client = new HttpClient();
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
                _progress = 0f;
                // If searching the same thing twice, return
                if (Result?.Query.ToLower() == query.ToLower())
                    return;

                // Check if current query is in the history
                int idx = InHistory(query);

                // If a match is found
                if (idx > -1)
                {

                    // If the user doesn't want to move results to the top return
                    if (!Wordsmith.Configuration.ResearchToTop)
                        return;

                    // If the last search was not null or an error
                    if (Result != null && !Result.SearchError)
                    {
                        // Move the last search to the history
                        AddHistoryEntry(Result);

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
                Dalamud.Logging.PluginLog.LogError(ex.Message);
            }

            Loading = true;
            if (Result != null && !Result.SearchError)
                AddHistoryEntry(Result);

            try
            {
                // Get the HTMl as a string
                var html = await _client.GetStringAsync("https://www.merriam-webster.com/thesaurus/" + query);
                _progress = 0.1f;

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

                for (int i = 0; i < variants.Count; ++i)
                {
                    _progress += (0.91f / (float)variants.Count);

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

                    // Get the synonym list
                    HtmlNode worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list syn-list']");
                    if (worker != null)
                        tEntry.AddSynonyms(worker.SelectNodes(".//a").Select(n => n.InnerText).ToArray());

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

                    // Get the antonyms list
                    worker = entrynodes[i].SelectSingleNode(".//span[@class='thes-list ant-list']");
                    if (worker != null)
                        tEntry.AddAntonyms(
                            worker.SelectNodes(".//a")?.Select(n => n.InnerText).ToArray() ??
                            worker.SelectSingleNode(".//ul[@class='mw-list']").SelectNodes(".//span").Select(n => n.InnerText).ToArray());


                    // This is a comment that shows the debug window
                    Dalamud.Logging.PluginLog.Log($"{query}##{result.ID}",$"{tEntry.Type} - {tEntry.Definition} - Synonyms: - {tEntry.SynonymString}--Related Words:-{tEntry.RelatedString}--Near Antonyms:-{tEntry.NearAntonymString}--Antonyms:-{tEntry.AntonymString}");
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
