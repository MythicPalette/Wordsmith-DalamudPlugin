using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class ResetDictionaryUI : Window
    {
        public ResetDictionaryUI() : base($"{Wordsmith.AppName} - Reset Dictionary")
        {
            WordsmithUI.WindowSystem.AddWindow(this);
            IsOpen = true;
            Size = new(300, 150);
            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
            Flags |= ImGuiWindowFlags.NoResize;
        }
        public override void Draw()
        {
            ImGui.TextColored(new(255, 0, 0, 255), "WARNING");
            ImGui.TextWrapped("This will delete all entries that you added to the dictionary. This cannot be undone.");
            ImGui.Text("Proceed?");

            if (ImGui.Button("Yes##RestoreDefaultSettingsConfirmedButton", new(120, 20)))
            {
                // Thesaurus settings.
                Wordsmith.Configuration.CustomDictionaryEntries = new();
                Wordsmith.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel##RestoreDefaultSettingsAbortedButton", new(120, 20)))
                IsOpen = false;
        }
    }
}
