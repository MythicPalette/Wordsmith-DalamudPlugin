using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class SettingsUI : Window
    {
        private const int FOOTER_HEIGHT = 100;

        // Thesaurus settings.
        private int _searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
        private bool _researchToTopChange = Wordsmith.Configuration.ResearchToTop;

        // Scratch Pad settings.
        private bool _deleteClosed = Wordsmith.Configuration.DeleteClosedScratchPads;
        private bool _ignoreHypen = Wordsmith.Configuration.IgnoreWordsEndingInHyphen;
        private bool _showChunks = Wordsmith.Configuration.ShowTextInChunks;
        private bool _onSentence = Wordsmith.Configuration.BreakOnSentence;
        private bool _autoClear = Wordsmith.Configuration.AutomaticallyClearAfterLastCopy;
        private bool _fixDoubleSpace = Wordsmith.Configuration.ReplaceDoubleSpaces;
        private int _scratchMaxTextLen = Wordsmith.Configuration.ScratchPadMaximumTextLength;
        private int _scratchEnter = Wordsmith.Configuration.ScratchPadTextEnterBehavior;
        private string _scratchSplitPoints = Wordsmith.Configuration.SplitPointDefinitions;
        private string _scratchEncapChars = Wordsmith.Configuration.EncapsulationCharacters;
        private bool _scratchExperimentalUI = Wordsmith.Configuration.UseExperimentalInputText;

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
                MinimumSize = ImGuiHelpers.ScaledVector2(400, 375),
                MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
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

            if (ImGui.BeginTabBar("SettingsUITabBar"))
            {
                DrawThesaurusTab();
                DrawScratchPadTab();
                DrawSpellCheckTab();
                ImGui.EndTabBar();
            }

            ImGui.Separator();
            DrawFooter();
        }

        protected void DrawThesaurusTab()
        {
            if (ImGui.BeginTabItem("Thesaurus##SettingsUITabItem"))
            {
                if (ImGui.BeginChild("ThesaurusSettingsChildFrame", new(-1, ImGui.GetWindowSize().Y - FOOTER_HEIGHT * ImGuiHelpers.GlobalScale)))
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
        }

        protected void DrawScratchPadTab()
        {
            if (ImGui.BeginTabItem("Scratch Pad##SettingsUITabItem"))
            {
                if (ImGui.BeginChild("SettingsUIScratchPadChildFrame", new(-1, ImGui.GetWindowSize().Y - FOOTER_HEIGHT * ImGuiHelpers.GlobalScale)))
                {
                    ImGui.Checkbox("Auto-delete Scratch Pads on close##SettingsUICheckbox", ref _deleteClosed);
                    ImGui.Separator();
                    ImGui.Checkbox("Don't spell check words that end with a hyphen##SettingsUICheckbox", ref _ignoreHypen);
                    ImGui.Separator();
                    ImGui.Checkbox("Show text in chunks##SettingsUICheckbox", ref _showChunks);
                    ImGui.Separator();
                    ImGui.Checkbox("Split text at period/questionmark/exclamation mark##SettingsUICheckbox", ref _onSentence);
                    ImGui.Separator();
                    ImGui.Checkbox("Automatically clear Scratch Pad text after copying last chunk.", ref _autoClear);
                    ImGui.Separator();
                    ImGui.Checkbox("Autmatically fix multiple spaces in text.", ref _fixDoubleSpace);
                    ImGui.Separator();
                    ImGui.DragInt("Max text length", ref _scratchMaxTextLen, 128, 512, 8096);
                    ImGui.TextWrapped("Note: The higher the text length, the more memory it will use (up to 8MB)");
                    ImGui.Separator();
                    ImGui.Combo("Enter Key Behavior", ref _scratchEnter, new string[] { "Do nothing", "Spell Check", "Copy" }, 3);
                    ImGui.Separator();
                    ImGui.TextWrapped($"Each of the characters in the text below can mark the end of a sentence. i.e. \"A.B\" is not a sentence terminator but \"A. B\" is.");
                    ImGui.InputText("##ScratchPadSplitCharsText", ref _scratchSplitPoints, 32);
                    ImGui.Separator();
                    ImGui.TextWrapped($"Each of the characters in the text below marks the end of an encapsulator. This is used with sentence terminators in case of encapsulation i.e. \"A) B\" will not count but \"A.) B\" will.");
                    ImGui.InputText($"##ScratchPadEncapCharsText", ref _scratchEncapChars, 32);
                    ImGui.Separator();
                    ImGui.TextColored(new(255, 0, 0, 255), "WARNING: Experimental.");
                    ImGui.TextWrapped($"These are experimental features and may have more bugs than usual, including game-crashing bugs. While I have done my best to ensure this doesn't happen, these are still experimental options until proven stable.");
                    ImGui.Checkbox($"Scratch Pad Multi Line Text Entry", ref _scratchExperimentalUI);
                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }
        }

        protected void DrawSpellCheckTab()
        {
            if (ImGui.BeginTabItem("Spell Check##SettingsUITabItem"))
            {
                if (ImGui.BeginChild("DictionarySettingsChild", new(-1, ImGui.GetWindowSize().Y - FOOTER_HEIGHT * ImGuiHelpers.GlobalScale)))
                {
                    ImGui.InputText("Dictionary Filename", ref _dictionaryFilename, 128);
                    ImGui.Separator();
                    ImGui.Spacing();

                    if (ImGui.BeginTable($"CustomDictionaryEntriesTable", 2, ImGuiTableFlags.BordersH))
                    {
                        ImGui.TableSetupColumn("CustomDictionaryWordColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                        ImGui.TableSetupColumn("CustomDictionaryDeleteColumn", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);

                        ImGui.TableNextColumn();
                        ImGui.Text("Custom dictionary entries (Deleting is permanent.)");

                        ImGui.TableNextColumn();
                        if (ImGui.Button("Delete All##DeleteAllDictionaryEntriesButton", ImGuiHelpers.ScaledVector2(-1, 20)))
                            WordsmithUI.ShowResetDictionary();

                        for (int i = 0; i < Wordsmith.Configuration.CustomDictionaryEntries.Count; ++i)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text(Wordsmith.Configuration.CustomDictionaryEntries[i]);

                            ImGui.TableNextColumn();
                            if (ImGui.Button($"Delete##CustomDictionaryDelete{i}Buttom", ImGuiHelpers.ScaledVector2(-1, 20)))
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

        protected void DrawFooter()
        {
            if (ImGui.BeginTable("SettingsUISaveCloseCancelButtonTable", 4))
            {
                ImGui.TableSetupColumn("SettingsUITableSpacerColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("SettingsUISaveAndCloseButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("SettingsUIDefaultsButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("SettingsUICancelButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);

                // Leave the first column blank for spacing.
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                // Save and close buttons
                if (ImGui.Button("Save And Close", ImGuiHelpers.ScaledVector2(-1, 20)))
                    Save();

                ImGui.TableNextColumn();
                // Reset settings to default.
                if (ImGui.Button("Restore Defaults", ImGuiHelpers.ScaledVector2(-1, 20)))
                {
                    WordsmithUI.ShowRestoreSettings();
                    IsOpen = false;
                }

                ImGui.TableNextColumn();
                // Cancel button
                if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(-1, 20)))
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
            _autoClear = Wordsmith.Configuration.AutomaticallyClearAfterLastCopy;
            _fixDoubleSpace = Wordsmith.Configuration.ReplaceDoubleSpaces;
            _scratchMaxTextLen = Wordsmith.Configuration.ScratchPadMaximumTextLength;
            _scratchEnter = Wordsmith.Configuration.ScratchPadTextEnterBehavior;
            _scratchSplitPoints = Wordsmith.Configuration.SplitPointDefinitions;
            _scratchEncapChars = Wordsmith.Configuration.EncapsulationCharacters;
            _scratchExperimentalUI = Wordsmith.Configuration.UseExperimentalInputText;

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

            if (_fixDoubleSpace != Wordsmith.Configuration.ReplaceDoubleSpaces)
                Wordsmith.Configuration.ReplaceDoubleSpaces = _fixDoubleSpace;

            if (_scratchMaxTextLen != Wordsmith.Configuration.ScratchPadMaximumTextLength)
                Wordsmith.Configuration.ScratchPadMaximumTextLength = _scratchMaxTextLen;

            if (_scratchEnter != Wordsmith.Configuration.ScratchPadTextEnterBehavior)
                Wordsmith.Configuration.ScratchPadTextEnterBehavior = _scratchEnter;

            if (_scratchSplitPoints != Wordsmith.Configuration.SplitPointDefinitions)
                Wordsmith.Configuration.SplitPointDefinitions = _scratchSplitPoints;

            if (_scratchEncapChars != Wordsmith.Configuration.EncapsulationCharacters)
                Wordsmith.Configuration.EncapsulationCharacters = _scratchEncapChars;

            if (_scratchExperimentalUI != Wordsmith.Configuration.UseExperimentalInputText)
                Wordsmith.Configuration.UseExperimentalInputText = _scratchExperimentalUI;

            // Spell Check settings.
            if (_dictionaryFilename != Wordsmith.Configuration.DictionaryFile)
                Wordsmith.Configuration.DictionaryFile = _dictionaryFilename;

            // Save the configuration
            Wordsmith.Configuration.Save();
            IsOpen = false;
        }
    }
}
