using System.Threading;
using Wordsmith.Data;

namespace Wordsmith.Helpers;

public class SpellChecker
{
    /// <summary>
    /// Check the given string for any possible spelling mistakes.
    /// </summary>
    /// <param name="str">The string to be tested.</param>
    /// <returns>True if no spelling errors or all errors resolved.</returns>
    internal static Word[] CheckString( string str )
    /// <summary>
    /// Check the given string for any possible spelling mistakes.
    /// </summary>
    /// <param name="str">The string to be tested.</param>
    /// <returns>True if no spelling errors or all errors resolved.</returns>
    {
        return CheckString( str, new CancellationTokenSource().Token );
    }

    internal static Word[] CheckString( string str, CancellationToken token )
    {
        List<Word> results = new();
        
        Word[] words = str.Words();

        // Iterate through all of the words.
        for ( int i = 0; i < words.Length; ++i )
        {
            if ( token.IsCancellationRequested )
                return results.ToArray();

            Word word = words[i];

            // If the word is hyphen terminated, then ignore it.
            if ( word.HyphenTerminated && Wordsmith.Configuration.IgnoreWordsEndingInHyphen )
                continue;

            string text = word.GetWordString(str);

            // If it's a number, just skip it
            if ( float.TryParse( text.Replace( ",", "" ), out _ ) )
                continue;

            if ( text.Length < 1 )
                continue;

            // if result is null then the word is not in the dictionary.
            if ( !Lang.isWord( text, true ) )
            {
                // For each possible contraction
                foreach ( string s in new string[] { "s", "d", "ll", "t", "ve", "n", "em", "re" } )
                {
                    // If the text ends with the contraction
                    if ( text.EndsWith( $"'{s}" ) )
                    {
                        // If the word is correct without the contraction then continue through words.
                        if ( Lang.isWord( text.Substring( 0, text.Length - 1 - s.Length ), true ) )
                            goto nextword;

                        // Found a matching contraction but it didn't work. Break from the loop.
                        // If there is more than one contraction it's going to be an error anyway.
                        break;
                    }
                }

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
                            goto invalidword;
                    }

                    // If we reach this point, all of the subwords checked out as properly spelt words.
                    // Continue the main loop to prevent adding hyphenated combinations to the results.
                    continue;
                }

            invalidword:
                // If we reached this code, we were not able to locate a proper match for the word.
                // Add the index to the list.
                word.InDictionary = false;
                results.Add( word );
            }

            // Gives a jump point to go to the next word.
        nextword:
            continue;
        }

        // Done checking all of the words, return the results.
        return results.ToArray();
    }
}
