using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class RestoreDefaultsUI : Window
    {
        public RestoreDefaultsUI() : base($"{Wordsmith.AppName} - Restore Default Settings")
        {
            WordsmithUI.WindowSystem.AddWindow(this);
            IsOpen = true;
            Size = ImGuiHelpers.ScaledVector2(300, 170);
            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
            Flags |= ImGuiWindowFlags.NoResize;
        }
        public override void Draw()
        {
            ImGui.TextColored(new(255, 0, 0, 255), "WARNING");
            ImGui.TextWrapped("Restoring defaults resets all settings to their original values (not including words added to your dictionary).");
            ImGui.Text("Proceed?");

            if (ImGui.Button("Yes##RestoreDefaultSettingsConfirmedButton", ImGuiHelpers.ScaledVector2(120, 20)))
            {
                // Thesaurus settings.
                Wordsmith.Configuration.SearchHistoryCount = 10;
                Wordsmith.Configuration.ResearchToTop = true;

                // Scratch Pad settings
                Wordsmith.Configuration.DeleteClosedScratchPads = false;
                Wordsmith.Configuration.IgnoreWordsEndingInHyphen = true;
                Wordsmith.Configuration.PunctuationCleaningList = ",.'*\"-(){}[]!?<>`~♥@#$%^&*_=+\\/";
                Wordsmith.Configuration.ShowTextInChunks = true;
                Wordsmith.Configuration.BreakOnSentence = true;
                Wordsmith.Configuration.AutomaticallyClearAfterLastCopy = false;
                Wordsmith.Configuration.ScratchPadTextEnterBehavior = 0;

                // Spell Check settings
                Wordsmith.Configuration.DictionaryFile = "lang_en";

                Wordsmith.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel##RestoreDefaultSettingsAbortedButton", ImGuiHelpers.ScaledVector2(120, 20)))
                IsOpen = false;
        }
    }
}
