using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class SettingsUI : WordsmithWindow
    {

        protected int _searchHistoryCountChange = -1;
        protected bool _researchToTopChange = true;

        // Start with _once at true so the program will load
        // the configuration values by default.
        protected bool _once = true;

        public SettingsUI() : base($"{Wordsmith.AppName} - Settings")
        {
            IsOpen = true;
            _searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
            _researchToTopChange = Wordsmith.Configuration.ResearchToTop;
            WordsmithUI.WindowSystem.AddWindow(this);
            Size = new(375, 150);
            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
            Flags |= ImGuiWindowFlags.NoResize;
        }

        public override void Update()
        {
            base.Update();

            if(!IsOpen)
                this.Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
            WordsmithUI.WindowSystem.RemoveWindow(this);
        }


        public override void Draw()
        {
            // If not open, exit.
            if (!IsOpen) return;

            //Search history count
            ImGui.DragInt("Search History Size", ref _searchHistoryCountChange, 0.1f, 1, 50);

            //Re-search to top
            ImGui.Checkbox("Move repeated search to top of history.", ref _researchToTopChange);

            // Save and close buttons
            if(ImGui.Button("Save And Close"))
            {
                // If history size has changed then update it
                if (_searchHistoryCountChange != Wordsmith.Configuration.SearchHistoryCount)
                    Wordsmith.Configuration.SearchHistoryCount = _searchHistoryCountChange;

                // If Research to top has changed then update it
                if (_researchToTopChange != Wordsmith.Configuration.ResearchToTop)
                    Wordsmith.Configuration.ResearchToTop = _researchToTopChange;

                // Save the configuration
                Wordsmith.Configuration.Save();
                IsOpen = false;
            }

            ImGui.SameLine();

            // Cancel button
            if(ImGui.Button("Cancel"))
                IsOpen = false;            
        }
    }
}
