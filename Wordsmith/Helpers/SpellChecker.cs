using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Wordsmith.Helpers;

internal sealed class SpellChecker
{   
    /// Checks a string against the currently loaded dictionary.
    /// </summary>
    /// <param name="str">String to check.</param>
    /// <param name="token">Cancellation token</param>
    /// <returns><see cref="SpellCheckResult"/> with result data.</returns>
    internal static List<Word> CheckString( string str )
    {
        List<Word> results = new();
        if ( Lang.Enabled && str is not null )
        {
            List<Word> words = str.Words();

            // Iterate through all of the words.
            for ( int i = 0; i < words.Count; ++i )
            {
                Word word = words[i];
                if ( word is null )
                    continue;

                // If the word is hyphen terminated, then ignore it.
                if ( word.HyphenTerminated && Wordsmith.Configuration.IgnoreWordsEndingInHyphen )
                    continue;

                // Get the word without punctuation at the beginning and end.
                string text = word.GetWordString(str);

                if ( text is null || text.Length < 1 )
                    continue;

                // If it's numeric, skip it
                if ( Regex.Match( text, Wordsmith.Configuration.NumericQuery ).Success )
                    continue;

                // If it is a date, skip it.
                if ( Regex.Match( text, Wordsmith.Configuration.DateQuery ).Success )
                    continue;

                // If it is a timestamp, skip it
                if ( Regex.Match( text, Wordsmith.Configuration.TimeQuery ).Success )
                    continue;

                // Regex is used here to get the word without any contractions.
                Match m = Regex.Match( text, Wordsmith.Configuration.WordQuery );

                // Failed to match to a word.
                if ( !m.Success )
                {
                    word.InDictionary = false;
                    results.Add( word );
                }

                // If the match data is not a known word
                else
                {
                    // Try to segment the word into subwords and match those. This is for cases where
                    // words are slashed/hyphened (i.e.: heavy/large)
                    foreach ( Match sub in Regex.Matches( m.Value, @"[^\s\-\\/]+" ) )
                    {
                        if ( !Lang.isWord( sub.Value, true ) )
                        {
                            // If we reached this code, we were not able to locate a proper match for the word.
                            // Add the index to the list.
                            word.InDictionary = false;
                            results.Add( word );
                            break;
                        }
                    }
                }
            }
        }
        
        return results;
    }
}
