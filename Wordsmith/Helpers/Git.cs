using System.Net.Http;
using Newtonsoft.Json;

namespace Wordsmith.Helpers
{
    internal class Git
    {
        internal class DictionaryDoesNotExistException : Exception
        { }

        // Read the manifest
        // Get language list
        // Get language file
        internal static WebManifest GetManifest()
        {
            // Download the manifest to a string.
            WebManifest result = new();
            using ( HttpClient client = new HttpClient() )
            {
                int tries = 3;
                while ( tries-- > 0 )
                {
                    try
                    {
                        // Force refresh
                        client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.UtcNow;
                        string raw = client.GetStringAsync( Global.MANIFEST_JSON_URL ).Result;

                        // Deserialize the manifest.
                        WebManifest? json = JsonConvert.DeserializeObject<WebManifest>(raw);

                        // If a valid manifest was received then make it the result.
                        if ( json != null )
                            result = json;

                        result.IsLoaded = true;
                        break;
                    }
                    catch ( Exception e )
                    {
                        client.DefaultRequestHeaders.IfModifiedSince = null;
                        PluginLog.LogError( $"Failed to get manifest. Tries remaining {tries}. Error: {e.Message}" );
                    }
                }
            }

            return result;
        }

        internal static string[] LoadDictionary(string name)
        {
            // Load the dictionary file as a string
            string result = "";
            using ( HttpClient client = new HttpClient() )
            {
                try
                {
                    // Force refresh
                    client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.Now;

                    // Get data
                    result = client.GetStringAsync( $"{Global.LIBRARY_FILE_URL}/{name}" ).Result;
                }
                catch (Exception e)
                {
                    PluginLog.LogError( e.Message );
                }
            }
            return result.Split( '\n' );
        }
    }
}
