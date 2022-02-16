using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using ImGuiNET;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Wordsmith.Extensions;

namespace Wordsmith.Gui
{
    public class ScratchPadUI : Window
    {
        protected class PadState
        {
            public int ChatType;
            public string ScratchText;
            public bool UseOOC;
            public string TellTarget;
            public PadState()
            {
                ChatType = 0;
                ScratchText = "";
                UseOOC = false;
                TellTarget = "";
            }

            public static bool operator ==(PadState state, object other) => state.Equals(other);

            public static bool operator !=(PadState state, object other) => !state.Equals(other);

            public override bool Equals(object? obj)
            {
                if (obj == null)
                    return false;

                if (obj is not PadState)
                    return false;


                PadState o = (PadState)obj;
                if (o.ChatType != this.ChatType) return false;
                if (o.ScratchText != this.ScratchText) return false;
                if (o.UseOOC != this.UseOOC) return false;
                if (o.TellTarget != this.TellTarget) return false;
                return true;
            }

            public override int GetHashCode() => HashCode.Combine(ChatType, ScratchText, UseOOC, TellTarget);
        }

        protected static readonly string[] _chatOptions = new string[] { "None", "Emote (/em)", "Reply (/r)", "Say (/s)", "Party (/p)", "FC (/fc)", "Shout (/sh)", "Yell (/y)", "Tell (/t)", "Linkshells" };
        protected static readonly string[] _chatHeaders = new string[] { "", "/em", "/r", "/s", "/p", "/fc", "/sh", "/y", "/t", "" };
        protected const int CHAT_NONE = 0;
        protected const int CHAT_TELL = 8;
        protected const int CHAT_LS = 9;

        protected static int _nextID = 0;
        public static int LastID => _nextID;
        public static int NextID => _nextID++;
        public int ID { get; set; }

        protected PadState _lastState = new();
        protected bool _refreshRequired = false;
        protected bool _overrideRefresh = false;

        protected string _error = "";
        protected string _notice = "";
        protected List<Data.WordCorrection> _corrections = new();

        protected int _chatType = 0;
        protected string _scratch = "";

        /// <summary>
        /// Returns a trimmed, single-line version of scratch.
        /// </summary>
        protected string ScratchString => _scratch.Trim().Replace('\n', ' ');
        protected string _telltarget = "";
        protected int _linkshell = 0;
        protected bool _crossWorld = false;
        protected float _lastWidth = -1;
        protected int _charWidth = 0;

        protected bool _useOOC = false;


        /// <summary>
        /// Gets the slash command (if one exists) and the tell target if one is needed.
        /// </summary>
        protected string GetFullChatHeader()
        {
            if (_chatType == CHAT_NONE)
                return "";

            // Get the slash command.
            string result = _chatHeaders[_chatType];

            // If /tell get the target or placeholder.
            if (_chatType == CHAT_TELL)
                result += $" {_telltarget} ";

            // Generate the linkshell options

            // Grab the linkshell command.
            if (_chatType == CHAT_LS)
                result = $"/{(_crossWorld ? "cw" : "")}linkshell{_linkshell+1}";

            return result;
        }

        protected int _scratchBufferSize = 4096;

        protected string[]? _chunks;
        protected int _nextChunk = 0;

        protected string _replaceText = "";

        public ScratchPadUI() : base($"{Wordsmith.AppName} - Scratch Pad #{_nextID}")
        {
            ID = NextID;
            IsOpen = true;
            WordsmithUI.WindowSystem.AddWindow(this);
            SizeConstraints = new()
            {
                MinimumSize = ImGuiHelpers.ScaledVector2(400, 300),
                MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
            };

            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
            Flags |= ImGuiWindowFlags.MenuBar;
        }

        public float GetFooterHeight()
        {
            float result = 60;
            if (!Wordsmith.Configuration.DeleteClosedScratchPads)
                result += 28;

            // If using the old, single-line input
            if (Wordsmith.Configuration.UseOldSingleLineInput)
                result += 35;
            else
                result += 90;

            if (_corrections.Count > 0)
                result += 32;

            return result * ImGuiHelpers.GlobalScale;
        }

        public override void Draw()
        {
            DrawMenu();
            DrawHeader();
            DrawChunkDisplay();

            // Draw the old, single line input
            if (Wordsmith.Configuration.UseOldSingleLineInput)
                DrawSingleLineTextInput();

            // Draw multi-line input.
            else
                DrawTextEntryExperimental();

            DrawWordReplacement();
            DrawFooter();
        }

        /// <summary>
        /// Draws the menu bar at the top of the window.
        /// </summary>
        protected void DrawMenu()
        {
            if (ImGui.BeginMenuBar())
            {
                // Start the scratch pad menu
                if (ImGui.BeginMenu("Scratch Pads##ScratchPadMenu"))
                {
                    // New scratchpad button.
                    if (ImGui.MenuItem($"New Scratch Pad##NewScratchPadMenuItem"))
                        WordsmithUI.ShowScratchPad(-1); // -1 id always creates a new scratch pad.

                    // For each of the existing scratch pads, add a button that opens that specific one.
                    foreach (ScratchPadUI w in WordsmithUI.Windows.Where(x => x.GetType() == typeof(ScratchPadUI)).ToArray())
                        if (w.GetType() != typeof(ScratchPadUI) && ImGui.MenuItem($"{w.WindowName}"))
                            WordsmithUI.ShowScratchPad(w.ID);

                    // End the scratch pad menu
                    ImGui.EndMenu();
                }

                // Text menu
                if (ImGui.BeginMenu($"Text##ScratchPad{ID}TextMenu"))
                {
                    // Clear text.
                    if (ImGui.MenuItem($"Clear##ScratchPad{ID}TextClearMenuItem"))
                        _scratch = "";

                    // TSpell Check
                    if (ImGui.MenuItem($"Spell Check##ScratchPad{ID}SpellCheckMenuItem"))
                        DoSpellCheck();

                    // If there are chunks
                    if ((_chunks?.Length ?? 0) > 0)
                    {
                        // Create a chunk menu.
                        if (ImGui.BeginMenu($"Chunks##ScratchPad{ID}ChunksMenu"))
                        {
                            // Create a copy menu item for each individual chunk.
                            for (int i=0; i<_chunks.Length; ++i)
                                if (ImGui.MenuItem($"Copy Chunk {i+1}##ScratchPad{ID}ChunkMenuItem{i}"))
                                    ImGui.SetClipboardText(_chunks[i]);

                            // End chunk menu
                            ImGui.EndMenu();
                        }
                    }
                    // End Text menu
                    ImGui.EndMenu();
                }

                // Thesaurus menu item
                if (ImGui.MenuItem($"Thesaurus##ScratchPad{ID}ThesaurusMenu"))
                    WordsmithUI.ShowThesaurus();

                // Settings menu item
                if (ImGui.MenuItem($"Settings##ScratchPad{ID}SettingsMenu"))
                    WordsmithUI.ShowSettings();

                // Help menu item
                if (ImGui.MenuItem($"Help##ScratchPad{ID}HelpMenu"))
                    WordsmithUI.ShowScratchPadHelp();

                //end Menu Bar
                ImGui.EndMenuBar();
            }
        }

        /// <summary>
        /// Draws the chat type selection and the tell target entry box if set to /tell
        /// </summary>
        protected void DrawHeader()
        {
            // Display errors
            if (_error != "")
            {
                ImGui.TextColored(new(255, 0, 0, 255), _error);
                ImGui.Separator();
            }

            // Display notifications
            if (_notice != "")
            {
                ImGui.Text(_notice);
                ImGui.Separator();
            }

            // If we're in Tell or Linkshell mode we need an extra column.
            int columns = 2 + (_chatType >= CHAT_TELL ? 1 : 0);
            if (ImGui.BeginTable($"##ScratchPad{ID}HeaderTable", columns))
            {
                // Setup 2-3 columns depending on the selected chat header.
                if (_chatType >= CHAT_TELL)
                {
                    ImGui.TableSetupColumn($"Scratchpad{ID}ChatmodeColumn", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn($"ScratchPad{ID}CustomTargetColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                }
                else
                    ImGui.TableSetupColumn($"Scratchpad{ID}ChatmodeColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn($"Scratchpad{ID}OOCColumn", ImGuiTableColumnFlags.WidthFixed, 75 * ImGuiHelpers.GlobalScale);

                // Header selection
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.Combo($"##ScratchPad{ID}ChatTypeCombo", ref _chatType, _chatOptions, _chatOptions.Length);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Select the chat header.");

                // Chat target bar
                if (_chatType == CHAT_TELL)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##TellTargetText", "User Name@World", ref _telltarget, 128);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Enter the user and world or a placeholder here.");
                }

                // Linkshell selection
                else if (_chatType == CHAT_LS)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.Checkbox("Cross-World", ref _crossWorld);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.Combo($"##ScratchPad{ID}LinkshellCombo", ref _linkshell, new string[] { "1", "2", "3", "4", "5", "6", "7", "8" }, 8);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Enter a custom targer here such as /cwls1.");
                }

                ImGui.TableNextColumn();
                ImGui.Checkbox("((OOC))", ref _useOOC);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enables or disables OOC double parenthesis.");
                ImGui.EndTable();
            }
        }

        /// <summary>
        /// Draws the text chunk display.
        /// </summary>
        /// <param name="FooterHeight">The size of the footer elements.</param>
        protected void DrawChunkDisplay()
        {
            // If we're not showing text chunks and we're not using single-line input, just don't
            // show the TextWrapped at all.
            if (!Wordsmith.Configuration.ShowTextInChunks && !Wordsmith.Configuration.UseOldSingleLineInput)
                return;

            if (ImGui.BeginChild($"{Wordsmith.AppName}##ScratchPad{ID}ChildFrame", new(-1, (Size?.X ?? 25) - GetFooterHeight())))
            {
                ImGui.SetNextItemWidth(-1);
                if (Wordsmith.Configuration.ShowTextInChunks)
                    ImGui.TextWrapped($"{string.Join("\n\n", _chunks ?? new string[] { "" })}");
                else
                    ImGui.TextWrapped($"{GetFullChatHeader()}{(_useOOC ? "(( " : "")}{ScratchString}{(_useOOC ? " ))" : "")}");
                ImGui.EndChild();
            }
            ImGui.Separator();
            ImGui.Spacing();
        }

        /// <summary>
        /// Draws a single line entry.
        /// </summary>
        protected void DrawSingleLineTextInput()
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint("##TextEntryBox", "Type Here...", ref _scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 1)
                    DoSpellCheck();

                else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 2)
                    DoCopyToClipboard();
            }
        }

        /// <summary>
        /// Draws a multiline text entry.
        /// </summary>
        protected unsafe void DrawTextEntryExperimental()
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextMultiline($"##MultilineTextEntry",
                ref _scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength,
                ImGuiHelpers.ScaledVector2(-1, 80), ImGuiInputTextFlags.CallbackEdit | ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CallbackAlways, OnTextEdit))
            {
                if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 1)
                    DoSpellCheck();

                else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 2)
                    DoCopyToClipboard();
            }    
        }

        /// <summary>
        /// Draws the word replacement section if there are known spelling errors.
        /// </summary>
        protected void DrawWordReplacement()
        {
            if (_corrections.Count > 0)
            {
                Data.WordCorrection correct = _corrections[0];

                float len = ImGui.CalcTextSize($"Spelling error: \"{correct.Original}\"").X;
                if (ImGui.BeginTable($"{ID}WordCorrectionTable", 4))
                {

                    ImGui.TableSetupColumn($"{ID}MisspelledWordColumn", ImGuiTableColumnFlags.WidthFixed, (len + 5) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn($"{ID}ReplacementTextInputColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn($"{ID}ReplaceTextButtonColumn", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn($"{ID}AddToDictionaryButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.TextColored(new(255, 0, 0, 255), $"Spelling error: \"{correct.Original}\"");

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - len - 200 * ImGuiHelpers.GlobalScale);
                    if (ImGui.InputTextWithHint("##ScratchPad{ID}ReplaceTextTextbox", "Replace with...", ref _replaceText, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                        OnReplace();

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.Button("Replace##ScratchPad{ID}ReplaceTextButton"))
                        OnReplace();

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.Button("Add To Dictionary##ScratchPad{ID}AddToDictionaryButton"))
                    {
                        Data.Lang.AddDictionaryEntry(correct.Original);

                        _corrections.RemoveAt(0);
                        if (_corrections.Count == 0)
                            _refreshRequired = true;
                    }

                    ImGui.EndTable();
                }
            }
        }

        /// <summary>
        /// Draws the buttons at the foot of the window.
        /// </summary>
        protected void DrawFooter()
        {
            if (ImGui.BeginTable($"{ID}FooterButtonTable", 3))
            {
                ImGui.TableSetupColumn($"{ID}FooterCopyButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn($"{ID}FooterClearButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn($"{ID}FooterSpellCheckButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);

                ImGui.TableNextColumn();
                if (ImGui.Button($"Copy{((_chunks?.Length ?? 0) > 1 ? $" ({_nextChunk + 1}/{_chunks?.Length})" : "")}", ImGuiHelpers.ScaledVector2(-1, 20)))
                    DoCopyToClipboard();

                ImGui.TableNextColumn();
                if (ImGui.Button($"Clear", ImGuiHelpers.ScaledVector2(-1, 20)))
                    _scratch = "";

                // Spell Check button.
                ImGui.TableNextColumn();
                if (ImGui.Button($"Spell Check", ImGuiHelpers.ScaledVector2(-1, 20)))
                    DoSpellCheck();

                ImGui.EndTable();
            }

            if (!Wordsmith.Configuration.DeleteClosedScratchPads)
            {
                if (ImGui.Button($"Delete Pad", ImGuiHelpers.ScaledVector2(-1, 20)))
                {
                    this.IsOpen = false;
                    WordsmithUI.RemoveWindow(this);
                }
            }
        }

        /// <summary>
        /// Gets the next chunk of text and copies it to the player's clipboard.
        /// </summary>
        protected void DoCopyToClipboard()
        {
            // Copy the next chunk over.
            ImGui.SetClipboardText(_chunks?[_nextChunk++] ?? "");

            // If we're not at the last chunk, return.
            if (_nextChunk < _chunks?.Length)
                return;

            // After this point, we assume we've copied the last chunk.
            _nextChunk = 0;

            // If configured to clear text after last copy
            if (Wordsmith.Configuration.AutomaticallyClearAfterLastCopy)
                _scratch = "";
        }

        /// <summary>
        /// Clears out any error messages or notices and runs the spell checker.
        /// </summary>
        protected void DoSpellCheck()
        {
            _error = "";
            _notice = "";

            // Don't spell check an empty input.
            if (_scratch.Length == 0)
                return;

            _corrections.AddRange(Helpers.SpellChecker.CheckString(_scratch.Replace('\n', ' ')));
            if (_corrections.Count > 0)
                _error = $"Found {_corrections.Count} spelling errors.";
            else
                _notice = "No spelling errors found.";
        }

        /// <summary>
        /// Replaces spelling errors with the given text or ignores an error if _replaceText is blank
        /// </summary>
        protected void OnReplace()
        {
            try
            {
                // If the text box is not empty
                if (_replaceText.Length > 0)
                {
                    // Get the first object
                    Data.WordCorrection correct = _corrections[0];

                    // Break apart the words.
                    string[] words = _scratch.Replace('\n', ' ').Split(' ');

                    // Replace the content of the word in question.
                    words[correct.Index] = _replaceText + words[correct.Index].Remove(0, correct.Original.Length);

                    _overrideRefresh = true;
                    // Replace the user's original text with the new words.
                    _scratch = string.Join(' ', words);

                    // Clear out replacement text.
                    _replaceText = "";
                }

                // Remove the spelling error.
                _corrections.RemoveAt(0);

                if (_corrections.Count == 0)
                    _overrideRefresh = false;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Handles automatically deleting the pad if configured to do so.
        /// </summary>
        public override void OnClose()
        {
            base.OnClose();
            if (Wordsmith.Configuration.DeleteClosedScratchPads)
                WordsmithUI.RemoveWindow(this);
        }

        /// <summary>
        /// Alters text input buffer in real time to create word wrap functionality in multiline textbox.
        /// </summary>
        /// <param name="data">Pointer to callback data</param>
        /// <returns></returns>
        public unsafe int OnTextEdit(ImGuiInputTextCallbackData* data)
        {
            UTF8Encoding utf8 = new();

            // Convert the buffer to a string
            string txt = utf8.GetString(data->Buf, data->BufTextLen);

            //// If the event flags are/contain CallbackEdit, the user either copy/pasted or entered a key.
            if ((data->EventFlag & ImGuiInputTextFlags.CallbackEdit) == ImGuiInputTextFlags.CallbackEdit)
                // If the string ends in a new line, remove it.
                txt = txt.TrimEnd('\n', '\r').TrimStart();

            // Replace all remaining new lines with spaces
            txt = txt.Replace('\n', ' ');

            // Replace double spaces if configured to do so.
            if (Wordsmith.Configuration.ReplaceDoubleSpaces)
                txt = txt.FixSpacing();

            // Get the maximum allowed character width.
            float width = ImGui.GetWindowWidth() - (32 * ImGuiHelpers.GlobalScale);

            // Iterate through each character.
            int lastSpace = 0;
            int offset = 0;
            for (int i = 1; i < txt.Length; ++i)
            {
                // If the current character is a space, mark it as a wrap point.
                if (txt[i] == ' ')
                    lastSpace = i;

                // If the size of the text is wider than the available size
                if (ImGui.CalcTextSize(txt.Substring(offset, i - offset)).X > width)
                {
                    // Replace the last previous space with a new line
                    StringBuilder sb = new(txt);
                    sb[lastSpace] = '\n';
                    txt = sb.ToString();

                    offset = lastSpace;
                }
            }

            // Convert the string back to bytes.
            byte[] bytes = utf8.GetBytes(txt);

            // Zero out the buffer.
            for (int i = 0; i < data->BufSize; ++i)
                data->Buf[i] = 0;

            // Replace with new values.
            for (int i = 0; i < bytes.Length; ++i)
                data->Buf[i] = bytes[i];

            data->BufTextLen = txt.Length;
            data->BufDirty = 1;
            return 0;
        }

        /// <summary>
        /// Gets a state object that reflects the current state of the pad
        /// </summary>
        /// <returns>Returns a PadState object with the current values of the pad</returns>
        protected PadState GetState()
        {
            return new()
            {
                ChatType = _chatType,
                ScratchText = _scratch,
                TellTarget = _telltarget,
                UseOOC = _useOOC
            };
        }
        
        /// <summary>
        /// Runs at each framework update.
        /// </summary>
        public override void Update()
        {
            base.Update();

            PadState newState = GetState();

            if (Wordsmith.Configuration.ReplaceDoubleSpaces)
                _scratch = _scratch.FixSpacing();

            if (_overrideRefresh)
            {
                _lastState = newState;
                _overrideRefresh = false;
            }

            else if (_lastState != newState || _refreshRequired)
            {
                _refreshRequired = false;
                _error = "";
                _notice = "";

                _corrections = new();

                _lastState = newState;
                _chunks = Helpers.ChatHelper.FFXIVify(GetFullChatHeader(), ScratchString, _useOOC);
                _nextChunk = 0;
            }
        }
    }
}
