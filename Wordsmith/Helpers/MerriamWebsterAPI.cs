using System.Net.Http;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Wordsmith.Helpers;

internal enum ApiState { Idle, Searching, Failed}
internal sealed class MerriamWebsterAPI : IDisposable
{
    public bool Loading { get; private set; } = false;
    public float Progress { get; private set; } = 0.0f;

    internal ApiState State { get; private set; }

    public List<WordSearchResult> History { get; } = [];

    /// <summary>
    /// The client used by Wordsmith Thesaurus to get the web pages for scraping.
    /// </summary>
    private HttpClient _client;

    /// <summary>
    /// Instantiates a new SearchHelper object
    /// </summary>
    public MerriamWebsterAPI() => this._client = new HttpClient();

    /// <summary>
    /// Adds a searched item to the history.
    /// </summary>
    /// <param name="entry">The entry to add to the history.</param>
    private void AddHistoryEntry(WordSearchResult entry)
    {
        // Don't re-add the same item to history.
        if ( FindSearchResult( entry.Query ) >= 0 )
            return;

        // Add the latest to the history
        this.History.Insert(0, entry);
        Wordsmith.PluginLog.Debug($"Added {entry.Query} to history.");

        // Setting search history length to zero will just make it unlimited.
        if ( Wordsmith.Configuration.SearchHistoryCount == 0 )
            return;

        // If over allowed amount remove oldest.
        while ( this.History.Count > Wordsmith.Configuration.SearchHistoryCount)
            this.History.RemoveAt( this.History.Count - 1);
    }

    /// <summary>
    /// Deletes an item from the search history.
    /// </summary>
    /// <param name="entry">Entry to be removed from history</param>
    public void DeleteResult(WordSearchResult? entry)
    {
        if (entry == null)
            return;

        else
            _ = this.History.Remove(entry);
    }

    public int FindSearchResult(string query)
    {
        for ( int i = 0; i < this.History.Count; i++ )
        {
            if ( this.History[i].Query.Equals( query, StringComparison.CurrentCultureIgnoreCase ) )
                return i;
        }
        return -1;
    }

    public void SearchThesaurus( string query )
    {
        BackgroundWorker worker = new();
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
            if( !SearchHistory( query ) )
            {
                string request = $"https://dictionaryapi.com/api/v3/references/thesaurus/json/{query.ToLower().Trim()}?key={Wordsmith.Configuration.MwApiKey}";
                string html = this._client.GetStringAsync( request ).Result;

                try
                {
                    XDocument? doc = JsonConvert.DeserializeXNode($"{{ \"entry\": {html} }}", "root");
                    if( doc is not null )
                    {
                        WordSearchResult result = new(query);

                        // Iterate through each entry
                        XElement? root = doc.Element("root");
                        if( root is null )
                            return;

                        List<XElement> entries = [.. root.Elements("entry")];
                        // Only deal with entries that match correctly.
                        foreach( XElement entry in entries )
                        {
                            // .Where( e => e.Element( "meta" )?.Element( "id" )?.Value.ToLower() == query )
                            if( entry.Element( "meta" ) is XElement meta )
                            {
                                if( meta.Element( "id" ) is XElement id )
                                {
                                    if( id.Value.Equals( query, StringComparison.CurrentCultureIgnoreCase ) )
                                    {
                                        Wordsmith.PluginLog.Debug( $"Element ID did not match query: {id}" );
                                        continue;
                                    }
                                }
                                else
                                {
                                    Wordsmith.PluginLog.Debug( $"Failed to get ID element: {meta}" );
                                    continue;
                                }
                            }
                            else
                            {
                                Wordsmith.PluginLog.Debug( $"Failed to get meta: {entry}" );
                                continue;
                            }

                            // Get all of the senses
                            List<XElement> subentries = entry.Element("def")?.Elements("sseq")?.ToList() ?? [];
                            foreach( XElement x in subentries )
                            {
                                // The data is nested in another sseq tag
                                XElement? data = x.Element("sseq")?.Elements("sseq").ElementAt(1);

                                if( data is not null )
                                {
                                    ThesaurusEntry tEntry = new()
                                    {
                                        Word = query,
                                        Type = entry.Element("fl")?.Value ?? "",
                                        Definition = ""
                                    };

                                    // Definition
                                    int searches = 2;
                                    foreach( XElement dt in data.Elements( "dt" ) )
                                    {
                                        // Get the text definition
                                        if( dt.Elements( "dt" ).ElementAt( 0 ).Value == "text" )
                                        {
                                            tEntry.Definition = dt.Elements( "dt" ).ElementAt( 1 ).Value;
                                            searches--;
                                        }

                                        // Get the example
                                        else if( dt.Elements( "dt" ).ElementAt( 0 ).Value == "vis" )
                                        {
                                            // This piece is super nested for some reason.
                                            tEntry.Visualization = dt.Elements( "dt" )?.ElementAt( 1 )?.Element( "dt" )?.Element( "t" )?.Value ?? "";
                                            searches--;
                                        }

                                        if( searches < 1 )
                                            break;
                                    }

                                    // Internal method to get the nested word data.
                                    List<string> GetNestedWordData( string data_header )
                                    {
                                        List<string> result = [];
                                        foreach( XElement word_list in data.Elements( data_header ) )
                                        {
                                            // The list has syn_list objects nested.
                                            foreach( XElement wl in word_list.Elements() )
                                            {
                                                // Inside the syn_list object is the word
                                                foreach( XElement wd in wl.Elements() )
                                                    result.Add( wd.Value );
                                            }
                                        }
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
                catch( Exception ex )
                {
                    Wordsmith.PluginLog.Error( ex.Message );
                }
            }
            else
            {
                e.Result = this.History.FirstOrDefault( w => w.Query.Equals( query, StringComparison.CurrentCultureIgnoreCase ) );
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
            if ( this.History.Count == 0)
                return false;

            Wordsmith.PluginLog.Debug($"Checking History for {query}");

            this.Progress = 0f;
            // If searching the same thing twice, return
            if ( this.History[0].Query.Equals( query, StringComparison.CurrentCultureIgnoreCase ) )
                return true;

            // Check if current query is in the history
            int idx = FindSearchResult(query);

            // If a match is found
            if (idx >= 0 )
            {
                // If the user doesn't want to move results to the top return
                if (Wordsmith.Configuration.ResearchToTop)
                {
                    // Get the search result
                    WordSearchResult result = this.History[idx];

                    // Remove the object
                    this.History.RemoveAt( idx );
                    this.History.Insert( 0, result );
                    
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            Wordsmith.PluginLog.Error(ex.Message);
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
            this._client?.Dispose();
        }
        catch ( Exception e )
        {
            Wordsmith.PluginLog.Error( e.Message );
        }
    }
}
