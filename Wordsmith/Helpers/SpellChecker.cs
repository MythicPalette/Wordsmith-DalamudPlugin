namespace Wordsmith.Helpers;

internal sealed class SpellChecker
{
    private const string NUMERIC_QUERY = @"^[0-9\-\.\,]+(?:st|nd|rd|th)?$";

    private const string ROMAN_NUMERAL_QUERY = @"^[IVXLCDM]+(?:st|nd|rd|th)?$";

    private const string DATE_QUERY = @"^\d{0,4}[\\\/\-\.]\d{0,4}[\\\/\-\.]\d{0,4}$";

    private const string WORD_QUERY = @"^(?<word>\S+)(?:'(?:ll|m|em|d|s))$|^(?<word>\S+)$";

    private const string TIME_QUERY = @"(?:\d{1,2}[:.]){1,3}\d{2}?\s*(?:[AaPp]\.?[Mm]\.?)*";

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
                string text = word.GetWordString(str);

                if ( text is null || text.Length < 1 )
                    continue;

                // If it's numeric, skip it
                if ( Regex.Match( text, NUMERIC_QUERY ).Success )
                    continue;

                // If it's a roman numeral, skip it
                if ( Regex.Match( text, ROMAN_NUMERAL_QUERY ).Success )
                    continue;

                // If it is a date, skip it.
                if ( Regex.Match( text, DATE_QUERY ).Success )
                    continue;

                // If it is a timestamp, skip it
                if ( Regex.Match( text, TIME_QUERY ).Success )
                    continue;

                // Regex is used here to get the word without any contractions.
                Match m = Regex.Match( text, WORD_QUERY );

                // Failed to match to a word.
                if ( !m.Success )
                {
                    word.InDictionary = false;
                    lResults.Add( word );
                }

                // If the match data is not a known word
                else if ( !Lang.isWord( m.Groups["word"].Value ))
                {
                    // Try to segment the word into subwords and match those. This is for cases where
                    // words are slashed/hyphened (i.e.: heavy/large)
                    foreach ( Match sub in Regex.Matches( m.Groups["word"].Value, @"[^\s\-\\/]+" ) )
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
