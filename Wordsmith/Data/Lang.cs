
namespace Wordsmith.Data;

public static class Lang
{
    private static HashSet<string> _dictionary = new();

    /// <summary>
    /// Active becomes true after Init() has successfully loaded a language file.
    /// </summary>
    public static bool Enabled { get; private set; } = false;

    /// <summary>
    /// Verifies that the string exists in the hash table.
    /// </summary>
    /// <param name="key">String to search for.</param>
    /// <returns><see langword="true"/> if the word is in the dictionary.</returns>
    public static bool isWord(string key) => _dictionary.Contains(key);

    /// <summary>
    /// Load the language file and enable spell checks.
    /// </summary>
    public static bool Init()
    {
        _dictionary = new();

        // Alert the user if the dictionary file fails to load
        if (!LoadLanguageFile())
        {
            Wordsmith.PluginInterface.UiBuilder.AddNotification($"Failed to load the dictionary file {Wordsmith.Configuration.DictionaryFile}. Spellcheck disabled.", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Warning);
            return false;
        }
        // Add all of the custom dictionary entries to the dictionary
        foreach (string word in Wordsmith.Configuration.CustomDictionaryEntries)
            _dictionary.Add(word.Trim().ToLower());

        // Set the dictionary to enabled.
        Enabled = true;
        return true;
    }

    /// <summary>
    /// Reinitialize the dictionary.
    /// </summary>
    /// <returns><see langword="true"/> if succesfully reinitialized.</returns>
    public static bool Reinit()
    {
        if (Init())
            Wordsmith.PluginInterface.UiBuilder.AddNotification($"Dictionary  {Wordsmith.Configuration.DictionaryFile} loaded.", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Success);
        else
            return false;
        return true;
    }

    /// <summary>
    /// Loads the specified language file.
    /// </summary>
    private static bool LoadLanguageFile()
    {
        // Get the filepath of the dictionary file
        string filepath = Path.Combine(Wordsmith.PluginInterface.AssemblyLocation.Directory?.FullName!, $"Dictionaries\\{Wordsmith.Configuration.DictionaryFile}");

        // Verify the file exists or log if the file is not found.
        if (!File.Exists(filepath))
        {
            PluginLog.LogWarning($"Configured language file \"{filepath}\" not found. Disabling all spell checking.");
            return false;
        }

        try
        {
            string[] lines = File.ReadAllLines(filepath);
            foreach (string l in lines)
            {
                if (!l.StartsWith("#") && l.Trim().Length > 0)
                    _dictionary.Add(l.Trim().ToLower());
            }
            return true;
        }
        catch (Exception e)
        {
            PluginLog.LogError($"Unable to load language file {Wordsmith.Configuration.DictionaryFile}. {e}");
            return false;
        }
    }

    /// <summary>
    /// Attempts to add a file to the dictionary.
    /// </summary>
    /// <param name="word">String to search.</param>
    /// <returns><see langword="true"/> if the word was not in the dictionary already.</returns>
    public static bool AddDictionaryEntry(string word)
    {
        // Add the word to the currently loaded dictionary.
        if (_dictionary.Add(word.Trim().ToLower()))
        {
            // If the word was added succesfully
            // Add the word to the configuration option.
            Wordsmith.Configuration.CustomDictionaryEntries.Add(word.ToLower().Trim());

            // Save the configuration.
            Wordsmith.Configuration.Save();
            return true;
        }

        return false;
    }

    public static void RemoveDictionaryEntry(string word)
    {
        _dictionary.Remove( word );
        Wordsmith.Configuration.CustomDictionaryEntries.Remove( word );
        Wordsmith.Configuration.Save();
    }

    internal static IReadOnlyList<string> GetSuggestions(string word)
    {
        if ( word.Length == 0)
            return new List<string>();

        // Check if the first character is capitalized.
        bool isCapped = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(word[0]);

        // Get the lowercase version of the word for the remaining tests.
        word = word.ToLower();
        
        // Letter swaps
        List<string> results = GenerateSwaps(word, isCapped, true);

        List<string> oneaway = GenerateAway(word, isCapped, false);
        List<string> twoaway = new();

        if ( results.Count < Wordsmith.Configuration.MaximumSuggestions  || Wordsmith.Configuration.MaximumSuggestions == 0)
            for ( int i = 0; i < oneaway.Count && oneaway.Count < Wordsmith.Configuration.MaximumSuggestions; ++i)// string away in oneaway )
                twoaway.AddRange( GenerateAway( oneaway[i], isCapped, true ) );

        results.AddRange( oneaway.Where(one => 
            _dictionary.Contains( one.ToLower() ) &&    // The word exists in the dictionary
            !results.Contains(one) &&                   // The word is not already in the results.
            (results.Count < Wordsmith.Configuration.MaximumSuggestions || Wordsmith.Configuration.MaximumSuggestions == 0) // There is still room in the results.
            ));

        results.AddRange( twoaway.Where(two =>
            _dictionary.Contains( two.ToLower() ) &&    // The word exists in the dictionary.
            !results.Contains( two ) &&                 // The word is not already in the results.
            (results.Count < Wordsmith.Configuration.MaximumSuggestions || Wordsmith.Configuration.MaximumSuggestions == 0) // There is still room in the results.
            ));

        return results.OrderBy(x => x).ThenBy( x => x.Length).ToList();
    }

    private static List<string> GenerateSwaps(string word, bool isCapped, bool filter)
    {
        List<string> results = new();

        // Letter swaps
        for ( int x = 0; x < word.Length - 1; ++x )
        {
            // Get the chars.
            char[] chars = word.ToCharArray();

            // Get the char at x
            char y = chars[x];

            // Move the char from x+1 to x
            chars[x] = chars[x + 1];

            // Overwite char at x+1 with x.
            chars[x + 1] = y;

            if ( !filter || isWord( new string( chars ) ) )
                results.Add( isCapped ? new string( chars ).CaplitalizeFirst() : new string( chars ) );
        }
        return results;
    }

    private static List<string> GenerateAway(string word, bool isCapped, bool filter)
    {
        string letters = "abcdefghijklmnopqrstuvwxyz";
        List<string> results = new();
        try
        {
            for ( int z = 0; z < 2; ++z )
            {
                for ( int x = -1; x <= word.Length; ++x )
                {
                    // Insert letter at the start of the word
                    if ( x == -1 )
                    {
                        // If z == 0 we skip this iteration.
                        if ( z == 0 )
                            continue;

                        for ( int y = 0; y < letters.Length; ++y )
                        {
                            string test = $"{letters[y]}{word}";
                            if ( !filter || isWord( test ) && !results.Contains( test ) )
                                results.Add( test );
                        }
                    }
                    // Append letter to the end.
                    else if ( x == word.Length )
                    {
                        // If z == 0 we skip this iteration.
                        if ( z == 0 )
                            continue;
                        for ( int y = 0; y < letters.Length; ++y )
                        {
                            string test = $"{word}{letters[y]}";
                            if ( !filter || isWord( test ) && !results.Contains( test ) )
                                results.Add( test );
                        }
                    }
                    // x will make the code switch between overwritting vowels and consenants.
                    else
                    {
                        for ( int y = 0; y < letters.Length; ++y )
                        {
                            char[] chars = word.ToCharArray();

                            // Start with vowel replacements, these are more common than
                            // consonant mistakes.
                            if ( "aAeEiIoOuUyY".Contains( chars[x] ) == (z == 0) )
                            {
                                chars[x] = letters[y];
                                string test = new string( chars );

                                if ( (!filter || isWord( test )) && !results.Contains( test ) )
                                    results.Add( isCapped ? test.CaplitalizeFirst() : test );
                            }

                            // For optimization break out of the y loop to avoid checking this
                            // 26 different times each time the chars[x] is the wrong character type.
                            // i.e. consant when z==0 or vowel when z==1.
                            else
                                break;
                        }
                    }

                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.LogError( ex.ToString() );
        }
        return results;
    }
}
