using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class SettingsUI : Window
    {

        // Thesaurus settings.
        private int _searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
        private bool _researchToTopChange = Wordsmith.Configuration.ResearchToTop;

        // Scratch Pad settings.
        private bool _deleteClosed = Wordsmith.Configuration.DeleteClosedScratchPads;
        private bool _ignoreHypen = Wordsmith.Configuration.IgnoreWordsEndingInHyphen;
        private bool _showChunks = Wordsmith.Configuration.ShowTextInChunks;
        private bool _onSentence = Wordsmith.Configuration.BreakOnSentence;
        private bool _autoClear = Wordsmith.Configuration.AutomaticallyClearAfterLastCopy;
        private int _scratchEnter = Wordsmith.Configuration.ScratchPadTextEnterBehavior;

        // Dictionary Settings
        private string _dictionaryFilename = Wordsmith.Configuration.DictionaryFile;

        // Start with _once at true so the program will load
        // the configuration values by default.
        protected bool _once = true;

        public SettingsUI() : base($"{Wordsmith.AppName} - Settings")
        {
            IsOpen = true;
            _searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
            _researchToTopChange = Wordsmith.Configuration.ResearchToTop;
            WordsmithUI.WindowSystem.AddWindow(this);
            //Size = new(375, 350);
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new(400, 375),
                MaximumSize = new(400, float.MaxValue)
            };

            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        }

        public override void Update()
        {
            base.Update();

            if (!IsOpen)
                WordsmithUI.WindowSystem.RemoveWindow(this);
        }


        public override void Draw()
        {
            // If not open, exit.
            if (!IsOpen) return;


            int bottomButtonsGap = 95;
            if (ImGui.BeginTabBar("SettingsUITabBar"))
            {
                if (ImGui.BeginTabItem("Thesaurus##SettingsUITabItem"))
                {
                    if (ImGui.BeginChild("ThesaurusSettingsChildFrame", new(-1, ImGui.GetWindowSize().Y - bottomButtonsGap)))
                    {
                        //Search history count
                        //ImGui.DragInt("Search History Size", ref _searchHistoryCountChange, 0.1f, 1, 50);
                        ImGui.InputInt("Search History Size", ref _searchHistoryCountChange, 1, 5);
                        if (_searchHistoryCountChange < 1)
                            _searchHistoryCountChange = 1;
                        if (_searchHistoryCountChange > 50)
                            _searchHistoryCountChange = 50;

                        //Re-search to top
                        ImGui.Checkbox("Move repeated search to top of history.", ref _researchToTopChange);

                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Scratch Pad##SettingsUITabItem"))
                {
                    if (ImGui.BeginChild("SettingsUIScratchPadChildFrame", new(-1, ImGui.GetWindowSize().Y - bottomButtonsGap)))
                    {
                        ImGui.Checkbox("Auto-delete Scratch Pads on close##SettingsUICheckbox", ref _deleteClosed);
                        ImGui.Checkbox("Don't spell check words that end with a hyphen##SettingsUICheckbox", ref _ignoreHypen);
                        ImGui.Checkbox("Show text in chunks##SettingsUICheckbox", ref _showChunks);
                        ImGui.Checkbox("Split text at period/questionmark/exclamation mark##SettingsUICheckbox", ref _onSentence);
                        ImGui.Checkbox("Automatically clear Scratch Pad text after copying last chunk.", ref _autoClear);
                        ImGui.Combo("Enter Key Behavior", ref _scratchEnter, new string[] { "Do nothing", "Spell Check", "Copy" }, 3);
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Spell Check##SettingsUITabItem"))
                {
                    if (ImGui.BeginChild("DictionarySettingsChild", new(-1, ImGui.GetWindowSize().Y - bottomButtonsGap)))
                    {
                        ImGui.InputText("Dictionary Filename", ref _dictionaryFilename, 128);
                        ImGui.Separator();
                        ImGui.Spacing();

                        if(ImGui.BeginTable($"CustomDictionaryEntriesTable", 2, ImGuiTableFlags.BordersH))
                        {
                            ImGui.TableSetupColumn("CustomDictionaryWordColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                            ImGui.TableSetupColumn("CustomDictionaryDeleteColumn", ImGuiTableColumnFlags.WidthFixed, 60);

                            ImGui.TableNextColumn();
                            ImGui.Text("Custom dictionary entries (Deleting is permanent.)");

                            ImGui.TableNextColumn();
                            if (ImGui.Button("Delete All##DeleteAllDictionaryEntriesButton", new(-1, 20)))
                                WordsmithUI.ShowResetDictionary();

                            for (int i=0; i<Wordsmith.Configuration.CustomDictionaryEntries.Count; ++i)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text(Wordsmith.Configuration.CustomDictionaryEntries[i]);

                                ImGui.TableNextColumn();
                                if(ImGui.Button($"Delete##CustomDictionaryDelete{i}Buttom", new(-1, 20)))
                                {
                                    Wordsmith.Configuration.CustomDictionaryEntries.RemoveAt(i);
                                    Wordsmith.Configuration.Save();
                                }
                            }
                            ImGui.EndTable();
                        }
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }
            }

            ImGui.Separator();
            if (ImGui.BeginTable("SettingsUISaveCloseCancelButtonTable", 4))
            {
                ImGui.TableSetupColumn("SettingsUITableSpacerColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("SettingsUISaveAndCloseButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("SettingsUIDefaultsButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("SettingsUICancelButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100);

                // Leave the first column blank for spacing.
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                // Save and close buttons
                if (ImGui.Button("Save And Close", new(-1, 20)))
                    Save();

                ImGui.TableNextColumn();
                // Reset settings to default.
                if (ImGui.Button("Restore Defaults", new(-1, 20)))
                    WordsmithUI.ShowRestoreSettings();

                ImGui.TableNextColumn();
                // Cancel button
                if (ImGui.Button("Cancel", new(-1, 20)))
                    IsOpen = false;

                ImGui.EndTable();
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();
            ResetValues();
        }

        private void ResetValues()
        {
            // Thesaurus settings.
            _searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
            _researchToTopChange = Wordsmith.Configuration.ResearchToTop;

            // Scratch Pad settings.
            _deleteClosed = Wordsmith.Configuration.DeleteClosedScratchPads;
            _ignoreHypen = Wordsmith.Configuration.IgnoreWordsEndingInHyphen;
            _showChunks = Wordsmith.Configuration.ShowTextInChunks;
            _onSentence = Wordsmith.Configuration.BreakOnSentence;

            // Dictionary Settings
            _dictionaryFilename = Wordsmith.Configuration.DictionaryFile;
        }

        private void Save()
        {
            // Thesaurus Settings.
            if (_searchHistoryCountChange != Wordsmith.Configuration.SearchHistoryCount)
                Wordsmith.Configuration.SearchHistoryCount = _searchHistoryCountChange;

            if (_researchToTopChange != Wordsmith.Configuration.ResearchToTop)
                Wordsmith.Configuration.ResearchToTop = _researchToTopChange;

            // Scratch Pad settings.
            if (_deleteClosed != Wordsmith.Configuration.DeleteClosedScratchPads)
                Wordsmith.Configuration.DeleteClosedScratchPads = _deleteClosed;

            if (_ignoreHypen != Wordsmith.Configuration.IgnoreWordsEndingInHyphen)
                Wordsmith.Configuration.IgnoreWordsEndingInHyphen = _ignoreHypen;

            if (_showChunks != Wordsmith.Configuration.ShowTextInChunks)
                Wordsmith.Configuration.ShowTextInChunks = _showChunks;

            if (_onSentence != Wordsmith.Configuration.BreakOnSentence)
                Wordsmith.Configuration.BreakOnSentence = _onSentence;

            if (_autoClear != Wordsmith.Configuration.AutomaticallyClearAfterLastCopy)
                Wordsmith.Configuration.AutomaticallyClearAfterLastCopy = _autoClear;

            if (_scratchEnter != Wordsmith.Configuration.ScratchPadTextEnterBehavior)
                Wordsmith.Configuration.ScratchPadTextEnterBehavior = _scratchEnter;

            // Spell Check settings.
            if (_dictionaryFilename != Wordsmith.Configuration.DictionaryFile)
                Wordsmith.Configuration.DictionaryFile = _dictionaryFilename;

            // Save the configuration
            Wordsmith.Configuration.Save();
            IsOpen = false;
        }
    }
}
