using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Wordsmith
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public int SearchHistoryCount { get; set; } = 10;
        public bool ResearchToTop { get; set; } = true;

        public bool DeleteClosedScratchPads { get; set; } = false;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
