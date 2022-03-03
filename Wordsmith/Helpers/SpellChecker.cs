using Wordsmith.Data;

namespace Wordsmith.Helpers;

public class SpellChecker
{
    /// <summary>
    /// Check the given string for any possible spelling mistakes.
    /// </summary>
    /// <param name="str">The string to be tested.</param>
    /// <returns>True if no spelling errors or all errors resolved.</returns>
    public static WordCorrection[] CheckString(string str)
    {
        // Take the string and split it into words, find any mistakes.
        string[] words = str.Split(" ");
        List<WordCorrection> results = new();

        for (int i = 0; i < words.Length; ++i)
        {
            // If it's a number, just skip it
            if (float.TryParse(words[i].Replace(",", ""), out float discard))
                continue;

            string word = CleanWord(words[i]);
            string lowercased = word.ToLower();
            if (word.Length < 1)
                continue;

            if (word.EndsWith('-') && Wordsmith.Configuration.IgnoreWordsEndingInHyphen)
                continue;

            //string? result = Lang.WordList.FirstOrDefault(w => w.ToLower() == lowercased);

            // if result is null then the word is not in the dictionary.
            if (!Lang.isWord(lowercased))
            {
                // Check it as a possible number by trying to parse a number after remove "st", "nd", "rd", and "th" from it.
                if (float.TryParse(word.Replace(",", "").Replace("st", "").Replace("nd", "").Replace("rd", "").Replace("th", ""), out float val))
                    continue; // It was a number, so continue the loop.
                
                // Check if the word is a hyphenation such as "crazy-like".
                if (word.Split('-').Length > 1)
                {
                    // Test each word in the hyphenation.
                    foreach(string subword in word.Split('-'))
                    {
                        // If the word is a blank or it isn't in the dictionary then jump to adding it to the list
                        // of misspelled words.
                        if (word.Length == 0 || !Lang.isWord(subword))//Lang.WordList.FirstOrDefault(w => w.ToLower() == subword) == null)
                            goto jumppoint;
                    }

                    // If we reach this point, all of the subwords checked out as properly spelt words.
                    // Continue the main loop to prevent adding hyphenated combinations to the results.
                    continue;
                }

                jumppoint:
                // If we reached this code, we were not able to locate a proper match for the word.
                // Add the index to the list.
                results.Add(new() { Original = word, Index = i });
            }
        }

        // Done checking all of the words, return the results.
        return results.ToArray();
    }

    /// <summary>
    /// Cleans the word of any punctuation marks that should not be at the beginning or end.
    /// i.e. "Hello." becomes Hello
    /// </summary>
    /// <param name="str"></param>
    /// <returns>Returns the word from of starting and ending punctuation and spaces.</returns>
    private static string CleanWord(string str)
    {
        // Remove white space at the beginning and end. There shouldn't be any but just in case.
        str = str.Trim();

        if (str.EndsWith("'s"))
            str = str[0..^2];

        // Loop
        do
        {
            // If the string is now empty, return an empty string.
            if (str.Length < 1)
                break;

            // Check the start and end of the word against every character.
            bool doBreak = true;
            foreach (char c in Wordsmith.Configuration.PunctuationCleaningList)
            {
                // Check the start of the string for the character
                if(str.StartsWith(c))
                {
                    // If the string starts with the symbol, remove the symbol and
                    // prevent exiting the loop.
                    str = str.Substring(1);
                    doBreak = false;
                }

                // If ignoring hyphen-ended words and the character is a hyphen, skip the 
                // EndsWith check.
                if (Wordsmith.Configuration.IgnoreWordsEndingInHyphen && c == '-')
                    continue;

                // Check the ending of the string
                if (str.EndsWith(c))
                {
                    // Remove the last character and prevent loop breaking
                    str = str.Substring(0, str.Length - 1);
                    doBreak = false;
                }
            }

            // If the break hasn't been prevented, break.
            if(doBreak)
                break;
        } while (true);

        return str;
    }
}
