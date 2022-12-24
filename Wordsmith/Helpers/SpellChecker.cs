namespace Wordsmith.Helpers;

internal sealed class SpellChecker
{
    /// <summary>
    /// Checks a string against the currently enabled dictionary.
    /// </summary>
    /// <param name="str"><see cref="string"/> to check for spelling errors.</param>
    /// <returns>A list containing all mispelled words. If there are no spelling errors then an empty list is returned.</returns>
    internal static List<Word> CheckString( string str )
    {
        List<Word> lResults = new();
        if ( Lang.Enabled && str.Length > 0 )
        {
            List<Word> lWords = str.Words();
            string sUnwrapped = str.Unwrap();
            // Iterate through all of the words.
            for ( int i = 0; i < lWords.Count; ++i )
            {
                Word word = lWords[i];
                if ( word is null )
                    continue;

                // If the word is hyphen terminated, then ignore it.
                if ( word.HyphenTerminated && Wordsmith.Configuration.IgnoreWordsEndingInHyphen )
                    continue;

                // Get the word without punctuation at the beginning and end.
                string text = word.GetWordString(sUnwrapped);

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
                    lResults.Add( word );
                }

                // If the match data is not a known word
                else if ( !Lang.isWord(text))
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
                            lResults.Add( word );
                            break;
                        }
                    }
                }
            }
        }
        
        return lResults;
    }
}
