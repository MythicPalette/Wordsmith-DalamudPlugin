
namespace Wordsmith.Data
{
    public static class Lang
    {
        /// <summary>
        /// Returns an array with all of the words in the current dictionary.
        /// </summary>
        public static string[] WordList => _wordlist.ToArray();

        /// <summary>
        /// List of all of the words in the current dictionary.
        /// </summary>
        private static List<string> _wordlist = new();

        /// <summary>
        /// Active becomes true after Init() has successfully loaded a language file.
        /// </summary>
        public static bool Enabled { get; private set; } = false;

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
            _wordlist.AddRange(Wordsmith.Configuration.CustomDictionaryEntries);

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
                // Get a stream reader for the file
                using (StreamReader r = new StreamReader(new FileStream(filepath, FileMode.Open)))
                {
                    // Read the file line by line into the the array.
                    while(!r.EndOfStream)
                    {
                        string? line = r.ReadLine()?.Trim();

                        // Ignore null lines.
                        if (line == null)
                            continue;

                        // Ignore empty lines.
                        if (line.Length < 1)
                            continue;

                        // Ignore comments and notations.
                        if (line.StartsWith("#"))
                            continue;

                        // Split the line by spaces and add it to the list of lines.
                        // This is important for human readability so that cities and other names
                        // can be written on the same line but registered into the dictionary separately.
                        // i.e. "Rak'tika Greatwood" can be one line.
                        //
                        // The dictionary is not case sensitive so trim and ToLower() each word.
                        _wordlist.AddRange(line.Split(" ").Select(l => l.Trim().ToLower()));
                    }
                    r.Close();
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
            if (_wordlist.FirstOrDefault(w => w.ToLower() == text.ToLower().Trim()) != null)
                return false;

            // Add the word to the currently loaded dictionary.
            _wordlist.Add(text.ToLower().Trim());

            // Add the word to the configuration option.
            Wordsmith.Configuration.CustomDictionaryEntries.Add(text.ToLower().Trim());

            // Save the configuration.
            Wordsmith.Configuration.Save();
            return true;
        }
    }
}
