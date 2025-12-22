namespace Wordsmith.Helpers;

internal sealed partial class SpellChecker
{
    internal const string WORD_QUERY = @"^(?<word>\S+)(?:'(?:ll|m|em|d|s))$|^(?<word>\S+)$";

    internal const string TIME_QUERY = @"(?:\d{1,2}[:.]){1,3}\d{2}?\s*(?:[AaPp]\.?[Mm]\.?)*";

    /// <summary>
    /// Checks a string against the currently enabled dictionary.
    /// </summary>
    /// <param name="str"><see cref="string"/> to check for spelling errors.</param>
    /// <returns>A list containing all mispelled words. If there are no spelling errors then an empty list is returned.</returns>
    internal static List<Word> CheckString( string str )
    {
        List<Word> lResults = [];
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
                if ( NumericRegex().IsMatch( text ) )
                    continue;

                // If it's a roman numeral, skip it
                if ( RomanNumeralRegex().IsMatch( text ) )
                    continue;

                // If it is a date, skip it.
                if ( DateRegex().IsMatch( text ) )
                    continue;

                // If it is a timestamp, skip it
                if ( TimeRegex().IsMatch( text ) )
                    continue;

                // Regex is used here to get the word without any contractions.
                Match m = WordRegex().Match( text );

                // Failed to match to a word.
                if ( !m.Success )
                {
                    word.InDictionary = false;
                    lResults.Add( word );
                }

                // If the match data is not a known word
                else if ( !Lang.IsWord( m.Groups["word"].Value ))
                {
                    // Try to segment the word into subwords and match those. This is for cases where
                    // words are slashed/hyphened (i.e.: heavy/large)
                    foreach ( Match sub in SubwordsRegex().Matches( m.Groups["word"].Value ) )
                    {
                        if ( !Lang.IsWord( sub.Value, true ) )
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

    [GeneratedRegex( @"^[0-9\-\.\,]+(?:st|nd|rd|th)?$" )]
    private static partial Regex NumericRegex();

    [GeneratedRegex( @"^[IVXLCDM]+(?:st|nd|rd|th)?$" )]
    private static partial Regex RomanNumeralRegex();
    [GeneratedRegex( @"^\d{0,4}[\\\/\-\.]\d{0,4}[\\\/\-\.]\d{0,4}$" )]
    private static partial Regex DateRegex();

    [GeneratedRegex( TIME_QUERY )]
    private static partial Regex TimeRegex();
    [GeneratedRegex( WORD_QUERY )]
    private static partial Regex WordRegex();
    [GeneratedRegex( @"[^\s\-\\/]+" )]
    private static partial Regex SubwordsRegex();
}
