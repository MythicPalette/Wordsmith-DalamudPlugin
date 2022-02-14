using Dalamud.Configuration;
using Dalamud.Plugin;
using System.Collections.Generic;
using System;

namespace Wordsmith
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public int SearchHistoryCount { get; set; } = 10;
        public bool ResearchToTop { get; set; } = true;

        // Scratch Pad settings
        public bool DeleteClosedScratchPads { get; set; } = false;

        /// <summary>
        /// If true, the spellchecker will not attempt to match words ending in a hyphen.
        /// This is because people often write a hyphen to indicate their sentence being
        /// cut off (i.e. "How dare yo-").
        /// </summary>
        public bool IgnoreWordsEndingInHyphen { get; set; } = true;

        /// <summary>
        /// The spell checker will attempt to delete these punctuation marks from the beginning and end of every word
        /// </summary>
        public string PunctuationCleaningList { get; set; } = ",.'*\"-(){}[]!?<>`~♥@#$%^&*_=+\\/";

        /// <summary>
        /// Toggles displaying text in copy chunks.
        /// </summary>
        public bool ShowTextInChunks { get; set; } = true;

        /// <summary>
        /// Attempts to break text chunks at the nearest sentence rather than the nearest space.
        /// </summary>
        public bool BreakOnSentence { get; set; } = true;

        /// <summary>
        /// If true, scratch pads will automatically clear their text after copying the last block.
        /// </summary>
        public bool AutomaticallyClearAfterLastCopy { get; set; } = false;
        
        /// <summary>
        /// Decides behavior when hitting enter in Scratch Pad text entry.
        /// </summary>
        public int ScratchPadTextEnterBehavior { get; set; } = 0;

        // Spell Check settings.
        /// <summary>
        /// Holds the dictionary of words added by the user.
        /// </summary>
        public List<string> CustomDictionaryEntries { get; set; } = new();

        /// <summary>
        /// The file to be loaded into Lang dictionary.
        /// </summary>
        public string DictionaryFile { get; set; } = "lang_en";

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;
        public void Save() => this.pluginInterface!.SavePluginConfig(this);
        
    }
}
