using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Dalamud.Logging;
using System.Reflection;

namespace Wordsmith.Data
{
    public class Lang
    {
        public static string[] WordList => _wordlist.ToArray();
        private static List<string> _wordlist = new();

        /// <summary>
        /// Active becomes true after Init() has successfully loaded a language file.
        /// </summary>
        public static bool Active { get; private set; } = false;

        /// <summary>
        /// Load the language file and enable spell checks.
        /// </summary>
        public static void Init()
        {
            // Only support english for now.
            string langCode = "en";
            if (!LoadLanguageFile(langCode))
                return;

            _wordlist.AddRange(Wordsmith.Configuration.CustomDictionaryEntries);
        }

        /// <summary>
        /// Loads the specified language file.
        /// </summary>
        /// <param name="langCode">The language code to be loaded (i.e. en). Defaults to "en" for English</param>
        private static bool LoadLanguageFile(string langCode)
        {
            string filepath = $"{Assembly.GetExecutingAssembly().Location.Replace("Wordsmith.dll", "")}lang_{langCode}";
            if (!File.Exists(filepath))
            {
                PluginLog.LogWarning($"Configured language file \"lang_{langCode}\" not found. Disabling all spell checking.");
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
                PluginLog.LogError($"Unable to load lang_{langCode}. {e}");
                return false;
            }
        }

        public static void AddDictionaryEntry(string text)
        {
            _wordlist.Add(text.ToLower().Trim());
            Wordsmith.Configuration.CustomDictionaryEntries.Add(text.ToLower().Trim());
            Wordsmith.Configuration.Save();
        }
    }
}
