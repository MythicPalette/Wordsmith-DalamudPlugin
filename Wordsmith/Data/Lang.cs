using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Wordsmith.Data
{
    public static class Lang
    {
        public static string[] WordList => _wordlist.ToArray();
        private static List<string> _wordlist = new();

        private static DalamudPluginInterface? pluginInterface;

        /// <summary>
        /// Active becomes true after Init() has successfully loaded a language file.
        /// </summary>
        public static bool Active { get; private set; } = false;

        /// <summary>
        /// Load the language file and enable spell checks.
        /// </summary>
        public static void Init(DalamudPluginInterface plugin)
        {
            pluginInterface = plugin;
            // Only support english for now.
            if (!LoadLanguageFile())
                return;

            _wordlist.AddRange(Wordsmith.Configuration.CustomDictionaryEntries);
        }

        /// <summary>
        /// Loads the specified language file.
        /// </summary>
        private static bool LoadLanguageFile()
        {
            string filepath = Path.Combine(pluginInterface?.AssemblyLocation.Directory?.FullName!, $"Dictionaries\\{Wordsmith.Configuration.DictionaryFile}");

            if (!File.Exists(filepath))
            {
                PluginLog.LogWarning($"Configured language file \"{filepath}\" not found. Disabling all spell checking.");
                return false;
            }
            try
            {
                using (StreamReader r = new StreamReader(new FileStream(filepath, FileMode.Open)))
                {
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
                        _wordlist.AddRange(line.Split(" "));
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

            _wordlist.Add(text.ToLower().Trim());
            Wordsmith.Configuration.CustomDictionaryEntries.Add(text.ToLower().Trim());
            Wordsmith.Configuration.Save();
            return true;
        }
    }
}
