using System.Net.Http;
using Newtonsoft.Json;

namespace Wordsmith.Helpers;

internal sealed class Git
{
    private const string _MANIFEST_JSON_URL = "https://raw.githubusercontent.com/MythicPalette/WordsmithDictionaries/main/manifest.json";
    private const string _LIBRARY_FILE_URL = "https://raw.githubusercontent.com/MythicPalette/WordsmithDictionaries/main/library";

    internal static WebManifest GetManifest()
    {
        // Download the manifest to a string.
        WebManifest result = new();
        using ( HttpClient client = new() )
        {
            int tries = 3;
            // Force refresh
            client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.UtcNow;
            while ( tries-- > 0 )
            {
                string raw = "";
                try
                {
                    raw = client.GetStringAsync( _MANIFEST_JSON_URL ).Result;

                    // Deserialize the manifest.
                    WebManifest? manifest = JsonConvert.DeserializeObject<WebManifest>(raw);

                    // If a valid manifest was received then make it the result.
                    if ( manifest != null )
                        result = manifest;

                    result.IsLoaded = true;

                    // Break from the while loop to avoid trying more times.
                    break;
                }
                catch ( Exception e )
                {
                    // Disable the IfModifiedSince header to avoid a 304 response error.
                    client.DefaultRequestHeaders.IfModifiedSince = null;
                    Wordsmith.PluginLog.Warning( $"Failed to get manifest. Tries remaining {tries}. Error: {e.Message}\nRaw: {raw}" );
                }
            }
        }
        return result;
    }

    internal static string[] LoadDictionary(string name)
    {
        // Load the dictionary file as a string
        string result = "";
        using ( HttpClient client = new() )
        {
            // Force refresh
            client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.Now;
            int tries = 3;
            while ( tries-- > 0 )
            {
                try
                {
                    // Get data
                    result = client.GetStringAsync( $"{_LIBRARY_FILE_URL}/{name}" ).Result;
                    Wordsmith.PluginLog.Debug( $"Loaded dictionary {name} from web." );
                    break;
                }
                catch ( Exception e )
                {
                    // Disable refresh request.
                    client.DefaultRequestHeaders.IfModifiedSince = null;
                    Wordsmith.PluginLog.Warning( $"Error loading dictionary from web: {e.Message}" );
                }
            }            
        }
        return result.Split( '\n' );
    }
}
