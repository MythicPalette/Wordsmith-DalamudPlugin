using System.Net.Http;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Wordsmith.Helpers;

internal enum ApiState { Idle, Searching, Failed}
internal sealed class MerriamWebsterAPI : IDisposable
{
    public bool Loading { get; private set; } = false;
    private float _progress = 0.0f;
    public float Progress => _progress;

    internal ApiState State { get; private set; }

    private List<WordSearchResult> _history = new List<WordSearchResult>();

    public List<WordSearchResult> History => _history;

    /// <summary>
    /// Adds a searched item to the history.
    /// </summary>
    /// <param name="entry">The entry to add to the history.</param>
    private void AddHistoryEntry(WordSearchResult entry)
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
    public void DeleteResult(WordSearchResult? entry)
    {
        if (entry == null)
            return;

        else if (_history.Contains(entry))
            _history.Remove(entry);
    }

    /// <summary>
    /// The client used by Wordsmith Thesaurus to get the web pages for scraping.
    /// </summary>
    private HttpClient _client;

    /// <summary>
    /// Instantiates a new SearchHelper object
    /// </summary>
    public MerriamWebsterAPI() {  _client = new HttpClient(); }

    public void SearchThesaurus( string query )
    {
        BackgroundWorker worker = new BackgroundWorker();
        worker.DoWork += SearchWorkerDoWork;
        worker.RunWorkerCompleted += SearchWorkerCompleted;

        this.State = ApiState.Searching;
        worker.RunWorkerAsync( argument: query );

    }

    private void SearchWorkerDoWork(object? sender, DoWorkEventArgs e)
    {
        if ( sender is BackgroundWorker )
        {
            if ( e.Argument == null )
                return;

            // Get the search data
            string query = (string) e.Argument;

            // Check if the search already exists
            if ( !SearchHistory( query ) )
            {
                string request = $"https://dictionaryapi.com/api/v3/references/thesaurus/json/{query.ToLower().Trim()}?key={Wordsmith.Configuration.MwApiKey}";
                string html = _client.GetStringAsync( request ).Result;

                try
                {
                    XDocument? doc = JsonConvert.DeserializeXNode($"{{ \"entry\": {html} }}", "root");
                    if ( doc is not null )
                    {
                        WordSearchResult result = new(query);

                        // Iterate through each entry
                        XElement? root = doc.Element("root");
                        if ( root is null )
                            return;

                        List<XElement> entries = new(root.Elements("entry"));
                        // Only deal with entries that match correctly.
                        foreach ( XElement entry in entries )
                        {
                            // .Where( e => e.Element( "meta" )?.Element( "id" )?.Value.ToLower() == query )
                            if ( entry.Element( "meta" ) is XElement meta )
                            {
                                if ( meta.Element( "id" ) is XElement id )
                                {
                                    if ( id.Value.ToLower() != query.ToLower() )
                                    {
                                        PluginLog.LogDebug( $"Element ID did not match query: {id}" );
                                        continue;
                                    }
                                }
                                else
                                {
                                    PluginLog.LogDebug( $"Failed to get ID element: {meta}" );
                                    continue;
                                }
                            }
                            else
                            {
                                PluginLog.LogDebug( $"Failed to get meta: {entry}" );
                                continue;
                            }

                            // Get all of the senses
                            List<XElement> subentries = entry.Element("def")?.Elements("sseq")?.ToList() ?? new();
                            foreach ( XElement x in subentries )
                            {
                                // The data is nested in another sseq tag
                                XElement? data = x.Element("sseq")?.Elements("sseq").ElementAt(1);

                                if ( data is not null )
                                {
                                    ThesaurusEntry tEntry = new()
                                    {
                                        Word = query,
                                        Type = entry.Element("fl")?.Value ?? "",
                                        Definition = ""
                                    };

                                    // Definition
                                    int searches = 2;
                                    foreach ( XElement dt in data.Elements( "dt" ) )
                                    {
                                        // Get the text definition
                                        if ( dt.Elements( "dt" ).ElementAt( 0 ).Value == "text" )
                                        {
                                            tEntry.Definition = dt.Elements( "dt" ).ElementAt( 1 ).Value;
                                            searches--;
                                        }

                                        // Get the example
                                        else if ( dt.Elements( "dt" ).ElementAt( 0 ).Value == "vis" )
                                        {
                                            // This piece is super nested for some reason.
                                            tEntry.Visualization = dt.Elements( "dt" )?.ElementAt( 1 )?.Element( "dt" )?.Element( "t" )?.Value ?? "";
                                            searches--;
                                        }

                                        if ( searches < 1 )
                                            break;
                                    }

                                    // Internal method to get the nested word data.
                                    List<string> GetNestedWordData( string data_header )
                                    {
                                        List<string> result = new();
                                        foreach ( XElement word_list in data.Elements( data_header ) )
                                            // The list has syn_list objects nested.
                                            foreach ( XElement wl in word_list.Elements() )
                                                // Inside the syn_list object is the word
                                                foreach ( XElement wd in wl.Elements() )
                                                    result.Add( wd.Value );

                                        return result;
                                    }

                                    tEntry.AddSynonyms( GetNestedWordData( "syn_list" ) );
                                    tEntry.AddSynonyms( GetNestedWordData( "sim_list" ) );

                                    tEntry.AddRelatedWords( GetNestedWordData( "rel_list" ) );

                                    tEntry.AddNearAntonyms( GetNestedWordData( "near_list" ) );

                                    tEntry.AddAntonyms( GetNestedWordData( "ant_list" ) );
                                    tEntry.AddAntonyms( GetNestedWordData( "opp_list" ) );

                                    result.AddEntry( tEntry );
                                }
                            }
                        }
                        e.Result = result;
                    }
                }
                catch ( Exception ex )
                {
                    PluginLog.LogError( ex.Message );
                }
            }
        }
    }

    private void SearchWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        // Get the result
        // Add the result to the history
        if ( e.Result is WordSearchResult result )
        {
            if ( result.Entries.Count > 0 )
            {
                AddHistoryEntry( result );
                this.State = ApiState.Idle;
                return;
            }
        }
        this.State = ApiState.Failed;
    }

    /// <summary>
    /// Searches through the user's history for the query.
    /// </summary>
    /// <param name="query">Search string to locate</param>
    /// <returns>True if the query was found in the history.</returns>
    private bool SearchHistory(string query)
    {
        try
        {
            // If there is no history return false.
            if (History.Count == 0)
                return false;

            PluginLog.LogDebug($"Checking History for {query}");

            _progress = 0f;
            // If searching the same thing twice, return
            if (History[0].Query == query)
                return true;

            // Check if current query is in the history
            WordSearchResult? result = History.FirstOrDefault(r => r.Query == query);

            // If a match is found
            if (result != null)
            {
                // If the user doesn't want to move results to the top return
                if (Wordsmith.Configuration.ResearchToTop)
                {
                    // Get the current index of the object.
                    int idx = _history.IndexOf(result);

                    // If the object is already at index 0 we can skip
                    // this step.
                    if ( idx > 0 )
                    {
                        // Remove the object
                        _history.Remove( result );
                        _history.Insert( 0, result );
                    }
                }
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
    /// Dispose of the HTML client and return.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _client?.Dispose();
        }
        catch ( Exception e )
        {
            PluginLog.LogError( e.Message );
        }
    }
}
