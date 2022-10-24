
using Wordsmith.Helpers;

namespace Wordsmith.Data;

public static class Lang
{
    private static HashSet<string> _dictionary = new();

    /// <summary>
    /// Active becomes true after Init() has successfully loaded a language file.
    /// </summary>
    public static bool Enabled { get; private set; } = false;

    /// <summary>
    /// Verifies that the string exists in the hash table
    /// </summary>
    /// <param name="key">String to search for.</param>
    /// <param name="lowercase">If <see langword="true"/> then the string is made lowercase.</param>
    /// <returns><see langword="true""/> if the word is in the dictionary</returns>
    public static bool isWord(string key, bool lowercase = true) => _dictionary.Contains( lowercase ? key.ToLower() : key );

    private static void ValidateAndAddWord(string candidate)
    {
        // Split and trim the candidate into all possible words. This should break entries with multiple words into single entries.
        string[] splits = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach ( string s in splits )
            _dictionary.Add( s.ToLower() );
    }

    /// <summary>
    /// Load the language file and enable spell checks.
    /// </summary>
    public static void Init()
    {
        _dictionary = new();

        // Load the dictionary
        bool web_loaded = LoadWebLanguage();

        // If web loading failed, load the file
        bool file_loaded = web_loaded ? false : LoadLanguageFile();

        // If both failed to load then present the failure notification
        if ( !(web_loaded || file_loaded) )
            Wordsmith.PluginInterface.UiBuilder.AddNotification( $"Failed to load the dictionary file {Wordsmith.Configuration.DictionaryFile}. Spellcheck disabled.", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Warning );
        else
        {
            // Add all of the custom dictionary entries to the dictionary
            foreach ( string word in Wordsmith.Configuration.CustomDictionaryEntries )
                ValidateAndAddWord( word );//_dictionary.Add( word.Trim().ToLower() );

            Enabled = true;
        }
    }

    /// <summary>
    /// Reinitialize the dictionary.
    /// </summary>
    /// <returns><see langword="true"/> if succesfully reinitialized.</returns>
    public static void Reinit()
    {
        Init();
    }

    private static bool LoadWebLanguage()
    {
        // Get the manifest
        WebManifest manifest = Git.GetManifest();

        // If the dictionary isn't in the manifest the user may have a custom dictionary
        // file that they prefer to use. Check for its existence here.
        if ( !manifest.IsLoaded || !manifest.Dictionaries.Contains( Wordsmith.Configuration.DictionaryFile ) )
            return false;

        try
        {
            // Load the dictionary array
            string[] lines = Git.LoadDictionary( Wordsmith.Configuration.DictionaryFile );
            foreach ( string l in lines )
                if ( !l.StartsWith( "#" ) && l.Trim().Length > 0 )
                    ValidateAndAddWord( l ); //_dictionary.Add( l.Trim().ToLower() );
            return true;
        }
        catch ( Exception e )
        {
            PluginLog.LogError( $"Unable to load language file {Wordsmith.Configuration.DictionaryFile}.\n{e}" );
            return false;
        }
    }

    /// <summary>
    /// Loads the specified language file.
    /// </summary>
    private static bool LoadLanguageFile()
    {
        // Get the filepath of the dictionary file
        string filepath = Path.Combine(Wordsmith.PluginInterface.AssemblyLocation.Directory?.FullName!, $"Dictionaries\\{Wordsmith.Configuration.DictionaryFile}");

        // If the file doesn't exist then abort
        if ( !File.Exists( filepath ) )
            return false;

        try
        {
            // Read the content to an array.
            string[] lines = File.ReadAllLines(filepath);

            // Iterate over each word and add it to the dictionary
            foreach ( string l in lines )
                if ( !l.StartsWith( "#" ) && l.Trim().Length > 0 )
                    ValidateAndAddWord( l ); //_dictionary.Add( l.Trim().ToLower() );

            return true;
        }
        catch ( Exception e )
        {
            PluginLog.LogError( $"Unable to load language file {Wordsmith.Configuration.DictionaryFile}. {e}" );
        }
        return false;
    }

    /// <summary>
    /// Attempts to add a word to the custom dictionary.
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

    /// <summary>
    /// Attempt to remove a word from the custom dictionary
    /// </summary>
    /// <param name="word">String to remove</param>
    public static void RemoveDictionaryEntry(string word)
    {
        _dictionary.Remove( word.Trim().ToLower() );
        Wordsmith.Configuration.CustomDictionaryEntries.Remove( word.Trim().ToLower() );
        Wordsmith.Configuration.Save();
    }

    internal static void GetSuggestions(ref List<string> list, string word)
    {
        if ( word.Length == 0)
            return;

        // Check if the first character is capitalized.
        bool isCapped = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(word[0]);

        // Get the lowercase version of the word for the remaining tests.
        word = word.ToLower();
        
        List<string> results = GenerateTranspose(word, isCapped, true);

        // Splits
        if ( results.Count < Wordsmith.Configuration.MaximumSuggestions )
        {
            List<string> splits = GenerateSplits(word);
            foreach ( string s in splits )
            {
                if ( results.Count == Wordsmith.Configuration.MaximumSuggestions )
                    break;

                if ( !results.Contains( s ) )
                    results.Add( s );
            }
        }

        // Letter deletes
        if ( results.Count < Wordsmith.Configuration.MaximumSuggestions )
        {
            List<string> deletes = GenerateDeletes(word, isCapped, true);
            foreach ( string s in deletes )
            {
                if ( results.Count >= Wordsmith.Configuration.MaximumSuggestions )
                    break;

                if ( isWord( s ) )
                    results.Add( s );
            }
        }

        // One away and two away.
        if ( results.Count < Wordsmith.Configuration.MaximumSuggestions )
        {
            List<string> aways = GenerateAway(word, 2, isCapped, false);

            foreach ( string s in aways )
            {
                if ( results.Count >= Wordsmith.Configuration.MaximumSuggestions )
                    break;

                if ( isWord( s ) && !results.Contains(s) )
                    results.Add( s );
            }
        }
        list = new( results );
    }

    private static List<string> GenerateTranspose(string word, bool isCapped, bool filter)
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

    private static List<string> GenerateDeletes(string word, bool isCapped, bool filter)
    {
        if ( word.Length == 0 )
            return new();

        List<string> results = new();
        for (int i = 0; i < word.Length; ++i )
        {
            if ( !filter || isWord( word.Remove( i, 1 ) ) )
                    results.Add( isCapped ? word.Remove( i, 1 ).CaplitalizeFirst() : word.Remove( i, 1 ) );
        }

        return results;
    }

    private static List<string> GenerateSplits(string word)
    {
        // for index
        // split into two words
        // if both splits are words
        // if check word one
        // then if check word two
        // add word one + word two
        // return results
        List<string> results = new();
        for (int i = 1; i < word.Length -1; ++i)
        {
            string[] splits = new string[] { word[0..i], word[i..^0] };
            if ( isWord( splits[0] ) && isWord( splits[1] ) )
                results.Add( $"{splits[0]} {splits[1]}" );
        }
        return results;
    }

    private static List<string> GenerateAway(string word, int depth, bool isCapped, bool filter)
    {
        string letters = "abcdefghijklmnopqrstuvwxyz";
        List<string> results = new();

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

        if (depth > 1)
        {
            List<string> parents = new List<string>(results);
            foreach (string s in parents)
                results.AddRange(GenerateAway(s, depth-1, isCapped, depth>2));
        }
        return results;
    }
}
