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
        private string _sentenceTerminators = Wordsmith.Configuration.SplitPointDefinitions;
        private string _encapTerminators = Wordsmith.Configuration.EncapsulationCharacters;
        private bool _scratchSingleLineInput = Wordsmith.Configuration.UseOldSingleLineInput;

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
                    ImGui.InputInt("History Size", ref _searchHistoryCountChange, 1, 5);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("This is the number of searches to keep in memory at one time.\nNote: The more you keep, them more memory used.");
                    if (_searchHistoryCountChange < 1)
                        _searchHistoryCountChange = 1;
                    if (_searchHistoryCountChange > 50)
                        _searchHistoryCountChange = 50;

                    //Re-search to top
                    ImGui.Checkbox("Move repeated search to top of history.", ref _researchToTopChange);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("If enabled, when searching for a word you've searched\nalready, it will move it to the top of the list.");

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
                    // Auto-Clear Scratch Pad
                    ImGui.Checkbox("Auto-clear Scratch Pad", ref _autoClear);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Automatically clears text from scratch pad after copying last chunk.");
                    ImGui.Separator();

                    // Auto Delete Scratch Pads
                    ImGui.Checkbox("Auto-delete Scratch Pads on close##SettingsUICheckbox", ref _deleteClosed);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("When enabled it will delete the scratch pad on close.\nWhen disabled you will have a delete button at the bottom.");
                    ImGui.Separator();

                    // Show text in chunks.
                    ImGui.Checkbox("Show text in chunks##SettingsUICheckbox", ref _showChunks);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("When enabled it will display a large box with text above your entry form.\nThis box will show you how the text will be broken into chunks.\nIn single-line input mode this text will always show but without chunking.");
                    ImGui.Separator();

                    // Split on sentence
                    ImGui.Checkbox("Split text on sentence##SettingsUICheckbox", ref _onSentence);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("When enabled, Scratch Pad attempts to do chunk breaks at the end of sentences instead\nof between any words.");
                    ImGui.Separator();

                    // Sentence terminators
                    ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 175 * ImGuiHelpers.GlobalScale);
                    ImGui.InputText("Sentence terminators##ScratchPadSplitCharsText", ref _sentenceTerminators, 32);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Each of these characters can mark the end of a sentence when followed by a space of encapsulator.\ni.e. \"A.B\" is not a sentence terminator but \"A. B\" is.");
                    ImGui.Separator();

                    // Encapsulation terminators
                    ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 175 * ImGuiHelpers.GlobalScale);
                    ImGui.InputText($"Encpasulation Terminators##ScratchPadEncapCharsText", ref _encapTerminators, 32);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"Each of these characters ends an encapsulator.\nThis is used with sentence terminators in case of encapsulation for chunk breaks\ni.e. \"A) B\" will not count but \"A.) B\" will.");
                    ImGui.Separator();

                    // Max Text Length
                    ImGui.DragInt("Max text length", ref _scratchMaxTextLen, 128, 512, 8192);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("This is the buffer size for text input. The higher this value is the more\nmemory consumed up to a maximum of 8MB per Scratch Pad.");
                    ImGui.Separator();

                    // Enter Key Behavior
                    ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
                    ImGui.Combo("Enter Key Behavior", ref _scratchEnter, new string[] { "Do nothing", "Spell Check", "Copy" }, 3);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Defines what action to take when the user hits enter in the text entry.");
                    ImGui.Separator();

                    // Revert Text Entry Mode
                    ImGui.Checkbox($"Revert To Scratch Pad Single Line Text Entry", ref _scratchSingleLineInput);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Reverts Scratch Pad to using the old, single-line text entry method.");
                    //ImGui.Separator();
                    //ImGui.TextColored(new(255, 0, 0, 255), "WARNING: Experimental.");
                    //ImGui.TextWrapped($"These are experimental features and may have more bugs than usual, including game-crashing bugs. While I have done my best to ensure this doesn't happen, these are still experimental options until proven stable.");
                    //ImGui.Checkbox($"Scratch Pad Multi Line Text Entry", ref _scratchSingleLineInput);
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
                    // Ignore Hyphen terminated words.
                    ImGui.Checkbox("Ignore hyphen-terminated words##SettingsUICheckbox", ref _ignoreHypen);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("This is useful in roleplay for emulating cut speech.\ni.e. \"How dare yo-,\" she was cut off but the rude man.");
                    ImGui.Separator();

                    // Auto-Fix Spaces
                    ImGui.Checkbox("Autmatically fix multiple spaces in text.", ref _fixDoubleSpace);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("When enabled, Scratch Pads will programmatically remove extra\nspaces from your text for you.");
                    ImGui.Separator();

                    // Dictionary File
                    ImGui.InputText("Dictionary Filename", ref _dictionaryFilename, 128);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"This is the file to be used for the dictionary. To use a custom spell check\ndictionary it must be inside the plug-in's Dictionary folder.");
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Custom Dictionary Table
                    if (ImGui.BeginTable($"CustomDictionaryEntriesTable", 2, ImGuiTableFlags.BordersH))
                    {
                        ImGui.TableSetupColumn("CustomDictionaryWordColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                        ImGui.TableSetupColumn("CustomDictionaryDeleteColumn", ImGuiTableColumnFlags.WidthFixed, 65 * ImGuiHelpers.GlobalScale);

                        ImGui.TableNextColumn();
                        ImGui.Text("Custom Dictionary Entries");

                        // Delete all
                        ImGui.TableNextColumn();
                        if (ImGui.Button("Delete All##DeleteAllDictionaryEntriesButton", ImGuiHelpers.ScaledVector2(-1, 25)))
                            WordsmithUI.ShowResetDictionary();
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip($"Deletes all dictionary entries. This action cannot be undone.");

                        // Individual entries
                        for (int i = 0; i < Wordsmith.Configuration.CustomDictionaryEntries.Count; ++i)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text(Wordsmith.Configuration.CustomDictionaryEntries[i]);

                            ImGui.TableNextColumn();
                            if (ImGui.Button($"Delete##CustomDictionaryDelete{i}Buttom", ImGuiHelpers.ScaledVector2(-1, 25)))
                            {
                                Wordsmith.Configuration.CustomDictionaryEntries.RemoveAt(i);
                                Wordsmith.Configuration.Save();
                            }
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip($"Permanently deletes {Wordsmith.Configuration.CustomDictionaryEntries[i]} from your custom dictionary.");
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
                ImGui.TableSetupColumn("SettingsUITableSpacerColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn("SettingsUISaveAndCloseButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("SettingsUIDefaultsButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("SettingsUICancelButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);

                // Leave the first column blank for spacing.
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                // Save and close buttons
                if (ImGui.Button("Apply", ImGuiHelpers.ScaledVector2(-1, 25)))
                    Save();

                ImGui.TableNextColumn();
                // Reset settings to default.
                if (ImGui.Button("Defaults", ImGuiHelpers.ScaledVector2(-1, 25)))
                {
                    WordsmithUI.ShowRestoreSettings();
                    IsOpen = false;
                }

                ImGui.TableNextColumn();
                // Cancel button
                if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(-1, 25)))
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
            _sentenceTerminators = Wordsmith.Configuration.SplitPointDefinitions;
            _encapTerminators = Wordsmith.Configuration.EncapsulationCharacters;
            _scratchSingleLineInput = Wordsmith.Configuration.UseOldSingleLineInput;

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

            if (_sentenceTerminators != Wordsmith.Configuration.SplitPointDefinitions)
                Wordsmith.Configuration.SplitPointDefinitions = _sentenceTerminators;

            if (_encapTerminators != Wordsmith.Configuration.EncapsulationCharacters)
                Wordsmith.Configuration.EncapsulationCharacters = _encapTerminators;

            if (_scratchSingleLineInput != Wordsmith.Configuration.UseOldSingleLineInput)
                Wordsmith.Configuration.UseOldSingleLineInput = _scratchSingleLineInput;

            // Spell Check settings.
            if (_dictionaryFilename != Wordsmith.Configuration.DictionaryFile)
                Wordsmith.Configuration.DictionaryFile = _dictionaryFilename;

            // Save the configuration
            Wordsmith.Configuration.Save();
        }
    }
}
