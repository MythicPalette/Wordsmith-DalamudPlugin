using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ImGuiNET;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using System.Numerics;

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
        protected string _telltarget = "";
        protected float _lastWidth = -1;
        protected int _charWidth = 0;

        protected bool _useOOC = false;

        protected static readonly string[] _chatOptions = new string[] { "None", "Emote (/em)", "Reply (/r)", "Party (/p)", "FC (/fc)", "Say (/s)", "Shout (/sh)", "Yell (/y)", "Tell (/t)" };
        protected static readonly string[] _chatHeaders = new string[] { "", "/em", "/r", "/p", "/fc", "/s", "/sh", "/y", "/t" };

        protected int _scratchBufferSize = 4096;

        protected string[]? _chunks;
        protected int _nextChunk = 0;

        protected string _replaceText = "";

        public ScratchPadUI() :base($"{Wordsmith.AppName} - Scratch Pad #{_nextID}")
        {
            ID = NextID;
            IsOpen = true;
            WordsmithUI.WindowSystem.AddWindow(this);
            SizeConstraints = new()
            {
                MinimumSize = new(400, 300),
                MaximumSize = new(float.MaxValue, float.MaxValue)
            };

            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        }

        public override void Draw()
        {
            //DrawMenu();
            DrawHead();
            DrawTextEntry();
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
                if (ImGui.BeginMenu($"Text##ScratchPad{ID}TextMenu"))
                {
                    if (ImGui.MenuItem($"Clear##ScratchPad{ID}TextClearMenuItem"))
                        _scratch = "";

                    // TODO Enable spell check.
                    if (ImGui.MenuItem($"Spell Check##ScratchPad{ID}SpellCheckMenuItem")) { } // Placeholder for the spell check function.

                    // If there are chunks
                    if ((_chunks?.Length ?? 0) > 0)
                    {
                        // Create a chunk menu.
                        if (ImGui.BeginMenu($"Chunks##ScratchPad{ID}ChunksMenu"))
                        {
                            // Create a copy button for each individual chunk.
                            for (int i=0; i<_chunks.Length; ++i)
                            {
                                if (ImGui.MenuItem($"Copy Chunk {i+1}##ScratchPad{ID}ChunkMenuItem{i}"))
                                    ImGui.SetClipboardText(_chunks[i]);
                            }

                            ImGui.EndMenu();
                        }
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu($"Settings##ScratchPad{ID}SettingsMenu"))
                {
                    if (ImGui.MenuItem($"Delete on close", "", Wordsmith.Configuration.DeleteClosedScratchPads))
                    {
                        Wordsmith.Configuration.DeleteClosedScratchPads = !Wordsmith.Configuration.DeleteClosedScratchPads;
                        _refreshRequired = true;
                        Wordsmith.Configuration.Save();
                    }
                    if (ImGui.MenuItem($"Don't spellcheck hyphenated words", "", Wordsmith.Configuration.IgnoreWordsEndingInHyphen))
                    { 
                        Wordsmith.Configuration.IgnoreWordsEndingInHyphen = !Wordsmith.Configuration.IgnoreWordsEndingInHyphen;
                        _refreshRequired = true;
                        Wordsmith.Configuration.Save();
                    }

                    if (ImGui.MenuItem($"Show text in blocks", "", Wordsmith.Configuration.ShowTextInChunks))
                    { 
                        Wordsmith.Configuration.ShowTextInChunks = !Wordsmith.Configuration.ShowTextInChunks;
                        _refreshRequired = true;
                        Wordsmith.Configuration.Save();
                    }

                    if (ImGui.MenuItem($"Attempt to form blocks by sentence", "", Wordsmith.Configuration.BreakOnSentence))
                    { 
                        Wordsmith.Configuration.BreakOnSentence = !Wordsmith.Configuration.BreakOnSentence;
                        _refreshRequired = true;
                        Wordsmith.Configuration.Save();
                    }

                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
        }

        /// <summary>
        /// Draws the chat type selection and the tell target entry box if set to /tell
        /// </summary>
        protected void DrawHead()
        {
            if (_error != "")
            {
                ImGui.TextColored(new(255, 0, 0, 255), _error);
                ImGui.Separator();
            }

            if (_notice != "")
            {
                ImGui.Text(_notice);
                ImGui.Separator();
            }

            int columns = 3 + (_chatType == 8 ? 1 : 0);
            if (ImGui.BeginTable($"##ScratchPad{ID}HeaderTable", columns))
            {
                
                if (_chatType == 8)
                {
                    ImGui.TableSetupColumn($"Scratchpad{ID}ChatmodeColumn", ImGuiTableColumnFlags.WidthFixed, 120);
                    ImGui.TableSetupColumn($"ScratchPad{ID}TellTargetColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                }
                else
                    ImGui.TableSetupColumn($"Scratchpad{ID}ChatmodeColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn($"Scratchpad{ID}OOCColumn", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn($"Scratchpad{ID}HelpButtonColumn", ImGuiTableColumnFlags.WidthFixed, 25);


                //ImGui.SetNextItemWidth((_chatType != 8 ? ImGui.GetWindowWidth() - 120 : 100));
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.Combo($"##ScratchPad{ID}ChatTypeCombo", ref _chatType, _chatOptions, 9);
                if (_chatType == _chatHeaders.Length - 1)
                {
                    //ImGui.SameLine();
                    ImGui.TableNextColumn();
                    //ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 220);
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##TellTargetText", "User Name@World", ref _telltarget, 128);
                }

                //ImGui.SameLine();
                ImGui.TableNextColumn();
                ImGui.Checkbox("((OOC))", ref _useOOC);

                //ImGui.SameLine();
                ImGui.TableNextColumn();
                if (ImGui.Button($"?##ScratchPad{ID}HelpButton", new(20, 20)))
                    WordsmithUI.ShowScratchPadHelp();

                ImGui.EndTable();
            }
        }

        /// <summary>
        /// Draws the chat with a single line entry and a wrapped text frame above it for proofreading.
        /// </summary>
        protected void DrawTextEntry()
        {
            int FooterHeight = 100;
            if (_corrections.Count > 0)
                FooterHeight += 25;

            if (ImGui.BeginChild($"{Wordsmith.AppName}##ScratchPad{ID}ChildFrame", new(-1, (Size?.X ?? 25) - FooterHeight)))
            {
                ImGui.SetNextItemWidth(-1);
                if (Wordsmith.Configuration.ShowTextInChunks)
                    ImGui.TextWrapped($"{string.Join("\n\n", _chunks ?? new string[] { })}");
                else
                    ImGui.TextWrapped($"{(_chatType > 0 ? $"{_chatHeaders[_chatType]} " : "")}{(_chatType == 8 ? $"{_telltarget} " : "")}{(_useOOC ? "(( " : "")}{_scratch}{(_useOOC ? " ))" : "")}");
                ImGui.EndChild();
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint("##TextEntryBox", "Type Here...", ref _scratch, 4096, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 1)
                    DoSpellCheck();
                else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 2)
                    DoCopyToClipboard();
            }
        }
        
        protected void DrawWordReplacement()
        {
            if (_corrections.Count > 0)
            {
                Data.WordCorrection correct = _corrections[0];

                float len = ImGui.CalcTextSize($"Spelling error: \"{correct.Original}\"").X;
                if (ImGui.BeginTable($"{ID}WordCorrectionTable", 4))
                {

                    ImGui.TableSetupColumn($"{ID}MisspelledWordColumn", ImGuiTableColumnFlags.WidthFixed, len + 5);
                    ImGui.TableSetupColumn($"{ID}ReplacementTextInputColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn($"{ID}ReplaceTextButtonColumn", ImGuiTableColumnFlags.WidthFixed, 50);
                    ImGui.TableSetupColumn($"{ID}AddToDictionaryButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.TextColored(new(255, 0, 0, 255), $"Spelling error: \"{correct.Original}\"");

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - len - 200);
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
                if (ImGui.Button($"Copy{(_chatType > 0 ? $" with {_chatHeaders[_chatType]}" : "")}{((_chunks?.Length ?? 0) > 1 ? $" ({_nextChunk + 1}/{_chunks?.Length})" : "")}", new(-1, 20)))
                    DoCopyToClipboard();

                ImGui.TableNextColumn();
                if (ImGui.Button($"Clear", new(-1, 20)))
                    _scratch = "";

                // Spell Check button.
                ImGui.TableNextColumn();
                if (ImGui.Button($"Spell Check", new(-1, 20)))
                    DoSpellCheck();

                ImGui.EndTable();
            }

            if (ImGui.Button($"Delete Pad", new(-1, 20)))
            {
                this.IsOpen = false;
                WordsmithUI.RemoveWindow(this);
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


        protected void DoSpellCheck()
        {
            _error = "";
            _notice = "";

            _corrections.AddRange(Helpers.SpellChecker.CheckString(_scratch));
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
            // If the text box is not empty
            if (_replaceText.Length > 0)
            {
                Data.WordCorrection correct = _corrections[0];
                // Break apart the words.
                string[] words = _scratch.Split(' ');

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
                _chunks = Helpers.ChatHelper.FFXIVify($"{(_chatType > 0 ? $"{_chatHeaders[_chatType]} " : "")}{(_chatType == 8 ? $"{_telltarget} " : "")}", _scratch, _useOOC);
                _nextChunk = 0;
            }
        }
    }
}
