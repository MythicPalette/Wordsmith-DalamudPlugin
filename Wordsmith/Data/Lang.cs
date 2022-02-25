
namespace Wordsmith.Data
{
    public static class Lang
    {
        /// <summary>
        /// Returns an array with all of the words in the current dictionary.
        /// </summary>

        /// <summary>
        /// List of all of the words in the current dictionary.
        /// </summary>
        private static Dictionary<string, bool> _wordlist = new();

        /// <summary>
        /// Active becomes true after Init() has successfully loaded a language file.
        /// </summary>
        public static bool Enabled { get; private set; } = false;

        public static bool isWord(string key) => _wordlist.TryGetValue(key, out bool ignore);

        /// <summary>
        /// Load the language file and enable spell checks.
        /// </summary>
        public static bool Init()
        {
            _wordlist = new();

            // Alert the user if the dictionary file fails to load
            if (!LoadLanguageFile())
            {
                Wordsmith.PluginInterface.UiBuilder.AddNotification($"Failed to load the dictionary file {Wordsmith.Configuration.DictionaryFile}. Spellcheck disabled.", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Warning);
                return false;
            }
            // Add all of the custom dictionary entries to the dictionary
            foreach (string word in Wordsmith.Configuration.CustomDictionaryEntries)
                _wordlist[word.Trim().ToLower()] = true;

            // Set the dictionary to enabled.
            Enabled = true;
            return true;
        }

        /// <summary>
        /// Reinitialize the dictionary.
        /// </summary>
        /// <returns></returns>
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
                        _wordlist[l.Trim().ToLower()] = true;
                }
                return true;
            }
            catch (Exception e)
            {
                PluginLog.LogError($"Unable to load language file {Wordsmith.Configuration.DictionaryFile}. {e}");
                return false;
            }
        }

        public static bool AddDictionaryEntry(string text)
        {
            // If the word is already in the dictionary, disregard.
            //if (_wordlist.FirstOrDefault(w => w.ToLower() == text.ToLower().Trim()) != null)
            //    return false;

            // Add the word to the currently loaded dictionary.
            //_wordlist.Add(text.ToLower().Trim(), true);
            _wordlist[text.ToLower().Trim()] = true;
            // Add the word to the configuration option.
            Wordsmith.Configuration.CustomDictionaryEntries.Add(text.ToLower().Trim());

            // Save the configuration.
            Wordsmith.Configuration.Save();
            return true;
        }
    }
}
