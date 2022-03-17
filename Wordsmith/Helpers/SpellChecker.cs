using Wordsmith.Data;

namespace Wordsmith.Helpers;

public class SpellChecker
{
    /// <summary>
    /// Check the given string for any possible spelling mistakes.
    /// </summary>
    /// <param name="str">The string to be tested.</param>
    /// <returns>True if no spelling errors or all errors resolved.</returns>
    internal static Word[] CheckString(string str)
    {
        List<Word> results = new();
        
        Word[] words = str.Words();

        // Iterate through all of the words.
        for ( int i = 0; i < words.Length; ++i )
        {
            Word word = words[i];
            string text = word.GetString(str);

            text = text.Clean();
            string lowercased = text.ToLower();

            // If it's a number, just skip it
            if ( float.TryParse( text.Replace( ",", "" ), out _ ) )
                continue;

            if ( text.Length < 1 )
                continue;

            if ( text.EndsWith( '-' ) && Wordsmith.Configuration.IgnoreWordsEndingInHyphen )
                continue;

            //string? result = Lang.WordList.FirstOrDefault(w => w.ToLower() == lowercased);

            // if result is null then the word is not in the dictionary.
            if ( !Lang.isWord( lowercased ) )
            {
                // Check it as a possible number by trying to parse a number after remove "st", "nd", "rd", and "th" from it.
                if ( float.TryParse( text.Replace( ",", "" ).Replace( "st", "" ).Replace( "nd", "" ).Replace( "rd", "" ).Replace( "th", "" ), out _ ) )
                    continue; // It was a number, so continue the loop.

                // Check if the word is a hyphenation such as "crazy-like".
                if ( text.Split( '-' ).Length > 1 )
                {
                    // Test each word in the hyphenation.
                    foreach ( string subword in text.Split( '-' ) )
                    {
                        // If the word is a blank or it isn't in the dictionary then jump to adding it to the list
                        // of misspelled words.
                        if ( text.Length == 0 || !Lang.isWord( subword ) )//Lang.WordList.FirstOrDefault(w => w.ToLower() == subword) == null)
                            goto jumppoint;
                    }

                    // If we reach this point, all of the subwords checked out as properly spelt words.
                    // Continue the main loop to prevent adding hyphenated combinations to the results.
                    continue;
                }

            jumppoint:
                // If we reached this code, we were not able to locate a proper match for the word.
                // Add the index to the list.
                word.InDictionary = false;
                results.Add( word );
            }
        }
        //offset += words.Length;
        //}
        // Done checking all of the words, return the results.
        return results.ToArray();
    }
}
