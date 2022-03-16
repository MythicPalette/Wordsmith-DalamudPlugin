
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
}
