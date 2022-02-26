using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace Wordsmith.Gui
{
    public class ScratchPadUI : Window
    {
        /// <summary>
        /// A protected class used only for comparing multiple pad state elements at once.
        /// </summary>
        protected class PadState
        {
            public int ChatType;
            public string ScratchText;
            public bool UseOOC;
            public string TellTarget;
            public bool CrossWorld;
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
                if (o.CrossWorld != this.CrossWorld) return false;
                return true;
            }

            public override int GetHashCode() => HashCode.Combine(ChatType, ScratchText, UseOOC, TellTarget);
        }

        /// <summary>
        /// Contains all of the constants used in this file.
        /// </summary>
        #region Constants
        protected static readonly string[] _chatOptions = new string[] { "None", "Emote (/em)", "Reply (/r)", "Say (/s)", "Party (/p)", "FC (/fc)", "Shout (/sh)", "Yell (/y)", "Tell (/t)", "Linkshells", "Echo" };
        protected static readonly string[] _chatHeaders = new string[] { "", "/em", "/r", "/s", "/p", "/fc", "/sh", "/y", "/t", "", "/e" };
        public const int CHAT_NONE = 0;
        public const int CHAT_EMOTE = 1;
        public const int CHAT_REPLY = 2;
        public const int CHAT_SAY = 3;
        public const int CHAT_PARTY = 4;
        public const int CHAT_FC = 5;
        public const int CHAT_SHOUT = 6;
        public const int CHAT_YELL = 7;
        public const int CHAT_TELL = 8;
        public const int CHAT_LS = 9;
        public const int CHAT_ECHO = 10;

        protected const int ENTER_KEY = 0xD;
        #endregion

        /// <summary>
        /// Contains all of the variables related to ID
        /// </summary>
        #region ID
        protected static int _nextID = 0;
        public static int LastID => _nextID;
        public static int NextID => _nextID++;
        public int ID { get; set; }
        #endregion

        /// <summary>
        /// Contains all of the variables related to the PadState
        /// </summary>
        #region Pad State
        protected PadState _lastState = new();
        protected bool _refreshRequired = false;
        protected bool _preserveCorrections = false;
        #endregion

        protected List<Data.WordCorrection> _corrections = new();

        #region Alerts
        protected List<string> _errors = new();
        protected List<string> _notices = new();
        protected bool _spellChecked = false;
        #endregion

        /// <summary>
        /// Contains all of the variables related to the chat header
        /// </summary>
        #region Chat Header
        protected int _chatType = 0;
        protected string _telltarget = "";
        protected int _linkshell = 0;
        protected bool _crossWorld = false;
        #endregion

        /// <summary>
        /// Contains all of the variables related to chat text.
        /// </summary>
        #region Chat Text
        /// <summary>
        /// Returns a trimmed, single-line version of scratch.
        /// </summary>
        protected string ScratchString => _scratch.Trim().Replace('\n', ' ');
        protected string _scratch = "";
        protected string _clearedScratch = "";
        protected int _scratchBufferSize = 4096;
        protected bool _useOOC = false;
        protected string[]? _chunks;
        protected int _nextChunk = 0;
        #endregion

        protected float _lastWidth = 0;
        protected bool _ignoreTextEdit = false;

        /// <summary>
        /// The text used by the replacement inputtext.
        /// </summary>
        protected string _replaceText = "";

        /// <summary>
        /// Cancellation token source for spellchecking.
        /// </summary>
        protected CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Gets the slash command (if one exists) and the tell target if one is needed.
        /// </summary>
        internal string GetFullChatHeader()
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
        
        public ScratchPadUI(string tellTarget) : this()
        {
            _chatType = CHAT_TELL;
            _telltarget = tellTarget;
        }

        public ScratchPadUI(int chatType) : this() => _chatType = chatType;

        /// <summary>
        /// Gets the height of the footer.
        /// </summary>
        /// <param name="IncludeTextbox">If false, the height of the textbox is not added to the result.</param>
        /// <returns></returns>
        public float GetFooterHeight(bool IncludeTextbox = true)
        {
            float result = 70;
            if (!Wordsmith.Configuration.DeleteClosedScratchPads)
                result += 28;

            if (IncludeTextbox)
            {
                // If using the old, single-line input
                if (Wordsmith.Configuration.UseOldSingleLineInput)
                    result += 35;
                else
                    result += 90;
            }

            if (_corrections.Count > 0)
                result += 32;

            return result * ImGuiHelpers.GlobalScale;
        }

        public override void Draw()
        {
            DrawMenu();
            DrawAlerts();
            DrawHeader();

            if (ImGui.BeginChild($"ScratchPad{ID}MainBodyChild", new(0, -1)))
            {
                if (WordsmithUI.FontBuilder?.Enabled ?? false)
                    ImGui.PushFont(WordsmithUI.FontBuilder.RegularFont!.Value);

                DrawChunkDisplay();

                // Draw the old, single line input
                if (Wordsmith.Configuration.UseOldSingleLineInput)
                    DrawSingleLineTextInput();

                // Draw multi-line input.
                else
                    DrawMultilineTextInput();

                // We do this here in case the window is being resized and we want
                // to rewrap the text in the textbox.
                if (ImGui.GetWindowWidth() > _lastWidth + 0.1 || ImGui.GetWindowWidth() < _lastWidth - 0.1)
                {
                    // Don't flag to ignore text edit if the window was just opened.
                    if (_lastWidth > 0.1)
                        _ignoreTextEdit = true;

                    // Turn _preserveCorrections on in case the user has incorrectly
                    // spelled words while resizing.
                    _preserveCorrections = true;

                    // Rewrap scratch
                    _scratch = WrapString(_scratch);

                    // Update the last known width.
                    _lastWidth = ImGui.GetWindowWidth();
                }

                if (WordsmithUI.FontBuilder?.Enabled ?? false)
                    ImGui.PopFont();

                // Draw the word replacement form.
                DrawWordReplacement();

                // Draw the buttons at the bottom of the screen.
                DrawFooter();

                // Close the child widget.
                ImGui.EndChild();
            }
        }

        /// <summary>
        /// Draws the menu bar at the top of the window.
        /// </summary>
        protected void DrawMenu()
        {
            if (ImGui.BeginMenuBar())
            {
                // Start the scratch pad menu
                if (ImGui.BeginMenu($"Scratch Pads##ScratchPadMenu{ID}"))
                {
                    // New scratchpad button.
                    if (ImGui.MenuItem($"New Scratch Pad##NewScratchPad{ID}MenuItem"))
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
                    // Show undo clear text option.
                    if (_clearedScratch.Length > 0)
                        if (ImGui.MenuItem($"Undo Clear##ScratchPad{ID}TextUndoClearMenuItem"))
                            UndoClearText();

                    // Show the clear text option.
                    else
                        if (ImGui.MenuItem($"Clear##ScratchPad{ID}TextClearMenuItem"))
                            DoClearText();

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
                            for (int i=0; i<_chunks!.Length; ++i)
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
            // If we're in Tell or Linkshell mode we need an extra column.
            int columns = 2 + (_chatType >= CHAT_TELL && _chatType != CHAT_ECHO ? 1 : 0);
            if (ImGui.BeginTable($"##ScratchPad{ID}HeaderTable", columns))
            {
                // Setup 2-3 columns depending on the selected chat header.
                if (_chatType >= CHAT_TELL && _chatType != CHAT_ECHO)
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
                    ImGui.InputTextWithHint($"##TellTargetText{ID}", "User Name@World", ref _telltarget, 128);
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
                    ImGui.Combo($"##ScratchPad{ID}LinkshellCombo", ref _linkshell, (_crossWorld ? Wordsmith.Configuration.CrossWorldLinkshellNames : Wordsmith.Configuration.LinkshellNames), 8);
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

        protected void DrawAlerts()
        {
            List<(string Message, int Level)> alerts = new();

            // Display errors
            if (_errors.Count > 0)
            {
                foreach (string err in _errors)
                    alerts.Add(new(err, -1));
            }

            // Display notices.
            if (_notices.Count > 0)
            {
                foreach (string n in _notices)
                    alerts.Add(new(n, 0));
            }

            // Display spelling error message
            if ((_corrections?.Count ?? 0) > 0)
                alerts.Add(new($"Found {_corrections!.Count} spelling errors.", -1));
            else if (_spellChecked)
                alerts.Add(new($"No spelling errors found!", 0));

            // If there are no alerts, return.
            if (alerts.Count == 0)
                return;

            // Draw all alerts.
            foreach ((string Message, int Level) alert in alerts)
            {
                if (alert.Level == -1)
                    ImGui.TextColored(new(255, 0, 0, 255), alert.Message);
                else
                    ImGui.Text(alert.Message);
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

            // Draw the chunk display
            if (ImGui.BeginChild($"{Wordsmith.AppName}##ScratchPad{ID}ChildFrame", new(-1, (Size?.X ?? 25) - GetFooterHeight())))
            {
                // We still perform this check on the property for ShowTextInChunks in case the user is using single line input.
                // If ShowTextInChunks is enabled, we show the text in its chunked state.
                if (Wordsmith.Configuration.ShowTextInChunks)
                {
                    for (int i = 0; i < (_chunks?.Length ?? 0); ++i)
                    {
                        // If not the first chunk, add a spacing.
                        if (i > 0)
                            ImGui.Spacing();

                        // Put a separator at the top of the chunk.
                        ImGui.Separator();

                        // Set width and display the chunk.
                        ImGui.SetNextItemWidth(-1);
                        ImGui.TextWrapped(_chunks![i]);
                    }
                }
                // If it's disabled and the user has enabled UseOldSingleLineInput then we still need to draw a display for them.
                else
                {
                    ImGui.SetNextItemWidth(-1);
                    ImGui.TextWrapped($"{GetFullChatHeader()}{(_useOOC ? "(( " : "")}{ScratchString}{(_useOOC ? " ))" : "")}");
                }
                
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

            // Draw the single line input
            if (ImGui.InputTextWithHint($"##TextEntryBox{ID}", "Type Here...", ref _scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                // Respond according to user-defined action in settings.
                if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 1)
                    DoSpellCheck();

                else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == 2)
                    DoCopyToClipboard();
            }
        }

        /// <summary>
        /// Draws a multiline text entry.
        /// </summary>
        protected unsafe void DrawMultilineTextInput()
        {
            ImGui.SetNextItemWidth(-1);

            // Default size of the text input.
            var v = ImGuiHelpers.ScaledVector2(-1, 80);

            // If the user has disabled ShowTextInChunks, increase the size to
            // take the entire available area.
            if (!Wordsmith.Configuration.ShowTextInChunks)
                v = new(-1, (Size?.X ?? 25) - GetFooterHeight(false));

            // Draw the input with multiple callbacks. These callbacks will be
            // used for managing the word wrapping.
            ImGui.InputTextMultiline($"##ScratchPad{ID}MultilineTextEntry",
                ref _scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength, v,
                ImGuiInputTextFlags.CallbackEdit |
                ImGuiInputTextFlags.NoHorizontalScroll, OnTextEdit);

            // Because InputTextMultiline doesn't trigger EnterReturnsTrue we instead check
            // if the input has focus and the user pressed the enter key
            if(ImGui.IsItemFocused() && ImGui.IsKeyPressed(ENTER_KEY))
            {
                // If the user hits enter, run the user-defined action.
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
                // Get the fist incorrect word.
                Data.WordCorrection correct = _corrections[0];


                // Notify of the spelling error.
                ImGui.TextColored(new(255, 0, 0, 255), "Spelling Error:");

                // Draw the text input.
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 230 * ImGuiHelpers.GlobalScale);
                _replaceText = correct.Original;

                if (ImGui.InputText($"##ScratchPad{ID}ReplaceTextTextbox", ref _replaceText, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                    OnReplace();

                // If they mouse over the input, tell them to use the enter key to replace.
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Fix the spelling of the word and hit enter or\nclick the \"Add to Dictionary\" button.");

                // Add to dictionary button
                ImGui.SameLine();
                ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"Add To Dictionary##ScratchPad{ID}"))
                {
                    Data.Lang.AddDictionaryEntry(correct.Original);

                    _corrections.RemoveAt(0);
                    if (_corrections.Count == 0)
                        _refreshRequired = true;
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
                // Setup the three columns for the buttons. I use a table here for easy space sharing.
                // The table will handle all sizing and positioning of the buttons automatically with no
                // extra input from me.
                ImGui.TableSetupColumn($"{ID}FooterCopyColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn($"{ID}FooterClearButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                ImGui.TableSetupColumn($"{ID}FooterSpellCheckButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);

                // Draw the copy button.
                ImGui.TableNextColumn();
                DrawCopyButton();

                // Draw the clear button.
                ImGui.TableNextColumn();
                DrawClearButton();

                // If spell check is disabled, make the button dark so it appears as though it is disabled.
                if (!Data.Lang.Enabled)
                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);

                // Draw the spell check button.
                ImGui.TableNextColumn();
                if (ImGui.Button($"Spell Check##Scratch{ID}", ImGuiHelpers.ScaledVector2(-1, 25)))
                    if (Data.Lang.Enabled) // If the dictionary is functional then do the spell check.
                        DoSpellCheck();

                // If spell check is disabled, pop the stylevar to return to normal.
                if (!Data.Lang.Enabled)
                    ImGui.PopStyleVar();

                ImGui.EndTable();
            }

            // If not configured to automatically delete scratch pads, draw the delete button.
            if (!Wordsmith.Configuration.DeleteClosedScratchPads)
            {
                if (ImGui.Button($"Delete Pad##Scratch{ID}", ImGuiHelpers.ScaledVector2(-1, 25)))
                {
                    this.IsOpen = false;
                    WordsmithUI.RemoveWindow(this);
                }
            }
        }

        /// <summary>
        /// Draws the copy button depending on how many chunks are available.
        /// </summary>
        protected void DrawCopyButton()
        {
            // If there is more than 1 chunk.
            if ((_chunks?.Length ?? 0) > 1)
            {
                // Push the icon font for the character we need then draw the previous chunk button.
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{(char)0xF100}##{ID}ChunkBackButton", ImGuiHelpers.ScaledVector2(25, 25)))
                {
                    --_nextChunk;
                    if (_nextChunk < 0)
                        _nextChunk = _chunks?.Length - 1 ?? 0;
                }
                // Reset the font.
                ImGui.PushFont(UiBuilder.DefaultFont);

                // Draw the copy button with no spacing.
                ImGui.SameLine(0, 0);
                if (ImGui.Button($"Copy{((_chunks?.Length ?? 0) > 1 ? $" ({_nextChunk + 1}/{_chunks?.Length})" : "")}##ScratchPad{ID}", new(ImGui.GetColumnWidth() - (23 * ImGuiHelpers.GlobalScale), 25 * ImGuiHelpers.GlobalScale)))
                    DoCopyToClipboard();

                // Push the font and draw the next chunk button with no spacing.
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SameLine(0, 0);
                if (ImGui.Button($"{(char)0xF101}##{ID}ChunkBackButton", ImGuiHelpers.ScaledVector2(25, 25)))
                {
                    ++_nextChunk;
                    if (_nextChunk >= (_chunks?.Length ?? 0))
                        _nextChunk = 0;
                }
                // Reset the font.
                ImGui.PushFont(UiBuilder.DefaultFont);
            }
            else // If there is only one chunk simply draw a normal button.
            {
                if (ImGui.Button($"Copy{((_chunks?.Length ?? 0) > 1 ? $" ({_nextChunk + 1}/{_chunks?.Length})" : "")}##ScratchPad{ID}", new(-1, 25 * ImGuiHelpers.GlobalScale)))
                    DoCopyToClipboard();
            }
        }


        /// <summary>
        /// Draws the copy button depending on how many chunks are available.
        /// </summary>
        protected void DrawClearButton()
        {
            // If there is more than 1 chunk.
            if (_clearedScratch.Length > 0)
            {
                if (ImGui.Button($"Clear##ScratchPad{ID}", new(ImGui.GetColumnWidth() - (23 * ImGuiHelpers.GlobalScale), 25 * ImGuiHelpers.GlobalScale)))
                    DoClearText();

                // Push the font and draw the next chunk button with no spacing.
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SameLine(0, 0);
                if (ImGui.Button($"{(char)0xF0E2}##{ID}UndoClearButton", ImGuiHelpers.ScaledVector2(25, 25)))
                    UndoClearText();

                // Reset the font.
                ImGui.PushFont(UiBuilder.DefaultFont);
            }
            else // If there is only one chunk simply draw a normal button.
            {
                if (ImGui.Button($"Clear##ScratchPad{ID}", new(-1, 25 * ImGuiHelpers.GlobalScale)))
                    DoClearText();
            }
        }

        /// <summary>
        /// Gets the next chunk of text and copies it to the player's clipboard.
        /// </summary>
        protected void DoCopyToClipboard()
        {
            // If there are no chunks to copy exit the function.
            if ((_chunks?.Length ?? 0) == 0)
                return;

            // Copy the next chunk over.
            ImGui.SetClipboardText(_chunks?[_nextChunk++]);

            // If we're not at the last chunk, return.
            if (_nextChunk < _chunks?.Length)
                return;

            // After this point, we assume we've copied the last chunk.
            _nextChunk = 0;

            // If configured to clear text after last copy
            if (Wordsmith.Configuration.AutomaticallyClearAfterLastCopy)
                DoClearText();
        }

        /// <summary>
        /// Moves the text from the textbox to a hidden variable in case the user
        /// wants to undo the change.
        /// </summary>
        protected void DoClearText()
        {
            // Ignore empty strings.
            if (_scratch.Length == 0)
                return;

            // Copy the string to the history variable.
            _clearedScratch = _scratch;

            // Clear scratch.
            _scratch = "";
        }

        protected void UndoClearText()
        {
            _scratch = _clearedScratch;
            _clearedScratch = "";
        }

        /// <summary>
        /// Clears out any error messages or notices and runs the spell checker.
        /// </summary>
        protected void DoSpellCheck()
        {
            // If there are any outstanding tokens, cancel them.
            _cancellationTokenSource?.Cancel();

            // Clear any errors and notifications.
            _notices.Add("Checking your spelling...");

            // Don't spell check an empty input.
            if (_scratch.Length == 0)
                return;

            // Create a new token source.
            _cancellationTokenSource = new();

            // Create and start the spell check task.
            Task t = new Task(() => DoSpellCheckAsync(), _cancellationTokenSource.Token);
            t.Start();
        }

        /// <summary>
        /// The spell check task to run.
        /// </summary>
        protected unsafe void DoSpellCheckAsync()
        {
            // Clear any old corrections to prevent them from stacking.
            _corrections = new();
            _corrections.AddRange(Helpers.SpellChecker.CheckString(_scratch.Replace('\n', ' ').Trim()));
            _spellChecked = true;
            _notices.Remove("Checking your spelling...");
        }

        /// <summary>
        /// Replaces spelling errors with the given text or ignores an error if _replaceText is blank
        /// </summary>
        protected void OnReplace()
        {
            // If the text box is not empty when the user hits enter then
            // update the text.
            if (_replaceText.Length > 0)
            {
                // Get the first object
                Data.WordCorrection correct = _corrections[0];

                // Break apart the words in the sentence.
                string[] words = _scratch
                    .Replace('\n', ' ')
                    .Split(' ');

                // Replace the content of the word in question.
                words[correct.Index] = words[correct.Index].Replace(correct.Original, _replaceText);

                // Preserving corrections prevents the Update method from
                // clearing the list of corrections even though the text
                // in the box has changed.
                _preserveCorrections = true;

                // Replace the user's original text with the new words.
                _scratch = string.Join(' ', words);

                // Clear out replacement text.
                _replaceText = "";

                // Rewrap the text string.
                _scratch = WrapString(_scratch);
            }

            // Remove the spelling error.
            _corrections.RemoveAt(0);

            // If corrections are not emptied then disable preserve.
            if (_corrections.Count == 0)
                _preserveCorrections = false;
        }

        /// <summary>
        /// Handles automatically deleting the pad if configured to do so.
        /// </summary>
        public override void OnClose()
        {
            base.OnClose();
            if (Wordsmith.Configuration.DeleteClosedScratchPads)
            {
                _cancellationTokenSource?.Cancel();
                WordsmithUI.RemoveWindow(this);
            }
        }

        /// <summary>
        /// Alters text input buffer in real time to create word wrap functionality in multiline textbox.
        /// </summary>
        /// <param name="data">Pointer to callback data</param>
        /// <returns></returns>
        public unsafe int OnTextEdit(ImGuiInputTextCallbackData* data)
        {
            // If _ignoreTextEdit is true then the reason for the edit
            // was a resize and the text has already been wrapped so
            // we simply return from here.
            if (_ignoreTextEdit)
            {
                _ignoreTextEdit = false;
                return 0;
            }

            UTF8Encoding utf8 = new();

            // For some reason, ImGui's InputText never verifies that BufTextLen never goes negative
            // which can lead to some serious problems and crashes with trying to get the string.
            // Here we do the check ourself with the turnery operator. If it does happen to be
            // a negative number, return a blank string so the rest of the code can continue as normal
            // at which point the buffer will be cleared and BufTextLen will be set to 0, preventing any
            // memory damage or crashes.
            string txt = data->BufTextLen >= 0 ? utf8.GetString(data->Buf, data->BufTextLen).TrimStart() : "";

            // If the event flags are/contain CallbackEdit, the user either copy/pasted or entered a key.
            if ((data->EventFlag & ImGuiInputTextFlags.CallbackEdit) == ImGuiInputTextFlags.CallbackEdit
                && ImGui.IsKeyPressed(ENTER_KEY)) // If the key they hit was enter
            {
                // Verify that the text length is greater than 1.
                if (txt.Length > 1)
                {
                    // If the user hit enter simply rebuild the string without the new line at the cursor.
                    txt = txt[0..(data->CursorPos - 1)] + txt[data->CursorPos..^0];

                    // Move the cursor back as if the user never hit enter.
                    data->CursorPos -= 1;
                }
                else
                {
                    // In this case, the text length was 1 or 0 and the user hit enter so we can simply
                    // blank the text and reset cursor position to 0.
                    txt = "";
                    data->CursorPos = 0;
                }
            }

            // Wrap the string.
            txt = WrapString(txt);

            // Convert the string back to bytes.
            byte[] bytes = utf8.GetBytes(txt);

            // Zero out the buffer.
            for (int i = 0; i < data->BufSize; ++i)
                data->Buf[i] = 0;

            // Replace with new values.
            for (int i = 0; i < bytes.Length; ++i)
                data->Buf[i] = bytes[i];

            // Assign the new buffer text length. This is the
            // number of bytes that make up the text, not the number
            // of characters in the text.
            data->BufTextLen = bytes.Length;

            // Flag the buffer as dirty so ImGui will rebuild the buffer
            // and redraw the text in the InputText.
            data->BufDirty = 1;

            // Return 0 to signal no errors.
            return 0;
        }

        /// <summary>
        /// Takes a string and wraps it based on the current width of the window.
        /// </summary>
        /// <param name="text">The string to be wrapped.</param>
        /// <returns></returns>
        protected string WrapString(string text)
        {
            // If the string is empty then just return it.
            if (text.Length == 0)
                return text;

            // Replace all remaining new lines with spaces
            text = text.Replace('\n', ' ');//(" \n", " ").Replace("\n", "");

            // Replace double spaces if configured to do so.
            if (Wordsmith.Configuration.ReplaceDoubleSpaces)
                text = text.FixSpacing();

            // Get the maximum allowed character width.
            float width = _lastWidth - (35 * ImGuiHelpers.GlobalScale);

            // Iterate through each character.
            int lastSpace = 0;
            int offset = 0;
            for (int i = 1; i < text.Length; ++i)
            {
                // If the current character is a space, mark it as a wrap point.
                if (text[i] == ' ')
                    lastSpace = i;

                // If the size of the text is wider than the available size
                float txtWidth = ImGui.CalcTextSize(text.Substring(offset, i - offset)).X;
                if (txtWidth + 10*ImGuiHelpers.GlobalScale > width)
                {
                    // Replace the last previous space with a new line
                    StringBuilder sb = new(text);
                    sb[lastSpace] = '\n';
                    offset = lastSpace;
                    text = sb.ToString();
                }
            }
            return text;
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
                UseOOC = _useOOC,
                CrossWorld = _crossWorld
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

            if (_lastState != newState || _refreshRequired)
            {
                // If the user has entered text then clear the _clearedScratch variable.
                if (_scratch != "")
                    _clearedScratch = "";

                // Cancel any outstanding tokens.
                _cancellationTokenSource?.Cancel();

                // Reset refreshRequired;
                _refreshRequired = false;

                // If not preserving the error list, clear it.
                if (!_preserveCorrections)
                {
                    _corrections = new();
                    _spellChecked = false;
                }
                _preserveCorrections = false;

                // Update the last state.
                _lastState = newState;

                // Rebuild chunks and reset chunk position counter.
                _chunks = Helpers.ChatHelper.FFXIVify(GetFullChatHeader(), ScratchString, _useOOC);
                _nextChunk = 0;
            }
        }
    }
}
