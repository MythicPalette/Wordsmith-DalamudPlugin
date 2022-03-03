using System.Net.Http;
using HtmlAgilityPack;

namespace Wordsmith.Helpers;

public class SearchHelper : IDisposable
{
    public bool Loading { get; private set; } = false;
    protected float _progress = 0.0f;
    public float Progress => _progress;

    public Exception? Error { get; private set; } = null;

    protected List<Data.WordSearchResult> _history = new List<Data.WordSearchResult>();
    public Data.WordSearchResult[] History => _history.ToArray();

    /// <summary>
    /// Adds a searched item to the history.
    /// </summary>
    /// <param name="entry">The entry to add to the history.</param>
    protected void AddHistoryEntry(Data.WordSearchResult entry)
    {
        // Add the latest to the history
        _history.Insert(0, entry);
        PluginLog.LogDebug($"Added {entry.Query} to history.");

        // If over allowed amount remove oldest.
        while (_history.Count >= Wordsmith.Configuration.SearchHistoryCount)
            _history.RemoveAt(_history.Count - 1);
    }

    /// <summary>
    /// Deletes an item from the search history.
    /// </summary>
    /// <param name="entry">Entry to be removed from history</param>
    public void DeleteResult(Data.WordSearchResult? entry)
    {
        if (entry == null)
            return;

        else if (_history.Contains(entry))
            _history.Remove(entry);
    }

    /// <summary>
    /// The client used by Wordsmith Thesaurus to get the web pages for scraping.
    /// </summary>
    protected HttpClient _client;

    /// <summary>
    /// Instantiates a new SearchHelper object
    /// </summary>
    public SearchHelper()
    {
        _client = new HttpClient();
    }

    public void SearchThesaurus(string query)
    {
        // Lowercase and trim the query string.
        query = query.ToLower().Trim();

        // Nullify the previous error.
        Error = null;

        // Check history first and if we don't find anything there
        // Trying web scraping for the information.
        if (!SearchHistory(query))
            ScrapeWeb(query);

    }

    /// <summary>
    /// Searches through the user's history for the query.
    /// </summary>
    /// <param name="query">Search string to locate</param>
    /// <returns>True if the query was found in the history.</returns>
    protected bool SearchHistory(string query)
    {
        try
        {
            // If there is no history return false.
            if (History.Length == 0)
                return false;

            PluginLog.LogDebug($"Checking History for {query}");

            _progress = 0f;
            // If searching the same thing twice, return
            if (History[0].Query == query)
                return true;

            // Check if current query is in the history
            Data.WordSearchResult? result = History.FirstOrDefault(r => r.Query == query);

            // If a match is found
            if (result != null)
            {
                // If the user doesn't want to move results to the top return
                if (Wordsmith.Configuration.ResearchToTop)
                    _history.Move(result, 0); // Move the result object to the top of the list.

                return true;
            }
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex.Message);
        }
        return false;
    }

    /// <summary>
    /// Searches www.merriam-webster.com/thesaurus for thesaurus entries.
    /// </summary>
    /// <param name="query">The string to search.</param>
    protected async void ScrapeWeb(string query)
    { 
        try
        {
            PluginLog.LogDebug($"Scraping web for {query}.");

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

                // Add the entry to the result object.
                result.AddEntry(tEntry);
            }

            // Add the result to the history
            AddHistoryEntry(result);
        }
        catch (HttpRequestException)
        {
            Error = new Exception($"{query} was not found in the thesaurus.");
        }
        catch (Exception ex)
        {
            Error = ex;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
