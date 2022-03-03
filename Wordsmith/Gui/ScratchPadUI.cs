using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace Wordsmith.Gui;

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

    protected const string SPACED_WRAP_MARKER = "\r\r";
    protected const string NOSPACE_WRAP_MARKER = "\r";

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
    protected string ScratchString => _scratch.Trim().Replace(SPACED_WRAP_MARKER + "\n", " ").Replace(NOSPACE_WRAP_MARKER+"\n", "");
    protected string _scratch = "";
    protected string _clearedScratch = "";
    protected int _scratchBufferSize = 4096;
    protected bool _useOOC = false;
    protected string[]? _chunks;
    protected int _nextChunk = 0;
    #endregion

    protected bool _scrollToBottom = false;

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

    /// <summary>
    /// Default constructor
    /// </summary>
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
    
    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with chat header as Tell and target as
    /// the given <see cref="string"/> argument.
    /// </summary>
    /// <param name="tellTarget">The target to append to the header.</param>
    public ScratchPadUI(string tellTarget) : this()
    {
        _chatType = CHAT_TELL;
        _telltarget = tellTarget;
    }

    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with chat header as the given type.
    /// </summary>
    /// <param name="chatType"><see cref="int"/> chat type.</param>
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

    /// <summary>
    /// The Draw entrypoint for the <see cref="WindowSystem"/> to draw the window.
    /// </summary>
    public override void Draw()
    {
        if (Wordsmith.Configuration.RecentlySaved)
            Refresh();

        DrawMenu();
        DrawAlerts();
        DrawHeader();

        if (ImGui.BeginChild($"ScratchPad{ID}MainBodyChild", new(0, -1)))
        {
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

                // Ignore cursor position requirement because we aren't adjusting that
                // just rewrapping.
                int ignore = 0;

                // Rewrap scratch
                _scratch = WrapString(_scratch, ref ignore);

                // Update the last known width.
                _lastWidth = ImGui.GetWindowWidth();
            }

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

    /// <summary>
    /// Draws the alert section of the page over the chat header selection.
    /// </summary>
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
            if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == Enums.EnterKeyAction.SpellCheck)
                DoSpellCheck();

            else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == Enums.EnterKeyAction.CopyNextChunk)
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

        // If the user has their option set to SpellCheck or Copy then
        // handle it with an EnterReturnsTrue.
        if (ImGui.InputTextMultiline($"##ScratchPad{ID}MultilineTextEntry",
            ref _scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength,
            v,
            ImGuiInputTextFlags.CallbackEdit |
            ImGuiInputTextFlags.EnterReturnsTrue,
            OnTextEdit))
        {
            // If the user hits enter, run the user-defined action.
            if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == Enums.EnterKeyAction.SpellCheck)
                DoSpellCheck();

            else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == Enums.EnterKeyAction.CopyNextChunk)
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
                    Refresh();
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

    /// <summary>
    /// Saves the cleared text in case the user wants to undo the clear then
    /// clears it.
    /// </summary>
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

            int ignore = 0;
            // Rewrap the text string.
            _scratch = WrapString(_scratch, ref ignore);
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

        int pos = data->CursorPos;

        // Wrap the string if there is enough there.
        if (txt.Length > 0)
            txt = WrapString(txt, ref pos);

        // Convert the string back to bytes.
        byte[] bytes = utf8.GetBytes(txt);

        // Replace with new values.
        for (int i = 0; i < bytes.Length; ++i)
            data->Buf[i] = bytes[i];

        // Terminate the string.
        data->Buf[bytes.Length] = 0;

        // Assign the new buffer text length. This is the
        // number of bytes that make up the text, not the number
        // of characters in the text.
        data->BufTextLen = bytes.Length;

        // Reassing the cursor position to adjust for the change in text lengths.
        data->CursorPos = pos;

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
    protected string WrapString(string text, ref int cursorPos)
    {
        // If the string is empty then just return it.
        if (text.Length == 0)
            return text;

        // Trim any return carriages off the end. This can happen if the user
        // backspaces a new line character off of the end.
        text = text.TrimEnd('\r');

        // If the user enters any character at the end of a line it will
        // break the wrap marker so scan for any broken wrap makers.
        for (int i = 0; i < text.Length; ++i)
        {
            // Check for a broken wrap marker \r\r#\n where # is any char besides \n.
            if (
                i < text.Length - SPACED_WRAP_MARKER.Length + 1 &&
                text[i..(i + SPACED_WRAP_MARKER.Length)] == SPACED_WRAP_MARKER &&
                text[i + SPACED_WRAP_MARKER.Length] != '\n' &&
                text[i + SPACED_WRAP_MARKER.Length + 1] == '\n')
            {
                // if the fourth character IS new line
                if (text[i + 3] == '\n')
                {
                    // A character has been wedged between the return
                    // carriage characters and the new line character.

                    // First, remove the new line characters.
                    text = text[0..i] + text[(i + 2)..^0];

                    // Now replace the new line with a space.
                    text = text[0..(i + 1)] + " " + text[(i + 2)..^0];

                    // Subtract the length of the spaced marker from the cursor position
                    if (cursorPos > i)
                        cursorPos -= 2;
                }
            }

            // Check for a broken no-space wrap maker \r#\n where # is any char besides \n.
            // It's important to also check that
            else if (
                i < text.Length - NOSPACE_WRAP_MARKER.Length + 1 &&      // Ensure that there are enough indices remaining.
                text[i..(i + NOSPACE_WRAP_MARKER.Length)] == NOSPACE_WRAP_MARKER &&     // If text starts with the no space marker and
                text[i + NOSPACE_WRAP_MARKER.Length] != '\n' &&                         // If the next character is not a new line
                text[i + NOSPACE_WRAP_MARKER.Length + 1] == '\n' &&                     // If the character after that is a new line
                text[i..(i + SPACED_WRAP_MARKER.Length)] != SPACED_WRAP_MARKER)         // If the characters are not the SPACED marker.
            {

                // Remove the \r from the text
                text = text[0..i] + text[(i + 1)..^0];

                // Remove the \n from the text.
                text = text[0..(i + 1)] + text[(i + 2)..^0];

                // Subtract the length of the spaced marker from the cursor position
                if (cursorPos > i)
                    cursorPos -= 2;

                // Need to iterate that the same index because we have already removed the
                // current char at i.
                i -= 1;
            }
        }

        // Replace all wrap markers with spaces and adjust cursor offset. Do this before
        // all non-spaced wrap markers because the Spaced marker contains the nonspaced marker
        while (text.Contains(SPACED_WRAP_MARKER+'\n'))
        {
            int idx = text.IndexOf(SPACED_WRAP_MARKER + '\n');
            text = text[0..idx] + " " + text[(idx + (SPACED_WRAP_MARKER + '\n').Length)..^0];

            // We adjust the cursor position by one less than the wrap marker
            // length to account for the space that replaces it.
            if (cursorPos > idx)
                cursorPos -= SPACED_WRAP_MARKER.Length;
            
        }

        while (text.Contains(NOSPACE_WRAP_MARKER+'\n'))
        {
            int idx = text.IndexOf(NOSPACE_WRAP_MARKER + '\n');
            text = text[0..idx] + text[(idx + (NOSPACE_WRAP_MARKER + '\n').Length)..^0];

            if (cursorPos > idx)
                cursorPos -= (NOSPACE_WRAP_MARKER + '\n').Length;
        }
        
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

                if (lastSpace > offset)
                {
                    sb.Remove(lastSpace, 1);
                    sb.Insert(lastSpace, SPACED_WRAP_MARKER+'\n');
                    offset = lastSpace + SPACED_WRAP_MARKER.Length;
                    i += SPACED_WRAP_MARKER.Length;

                    // Adjust cursor position for the marker but not
                    // the new line as the new line is replacing the space.
                    if (lastSpace < cursorPos)
                        cursorPos += SPACED_WRAP_MARKER.Length;
                }
                else
                {
                    sb.Insert(i, NOSPACE_WRAP_MARKER + '\n');
                    offset = i + NOSPACE_WRAP_MARKER.Length;
                    i += NOSPACE_WRAP_MARKER.Length;

                    // Adjust cursor position for the marker and the
                    // new line since both are inserted.
                    if (cursorPos > i - NOSPACE_WRAP_MARKER.Length)
                        cursorPos += NOSPACE_WRAP_MARKER.Length + 1;
                }
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

        if (Wordsmith.Configuration.ReplaceDoubleSpaces)
            _scratch = _scratch.FixSpacing();

        _scrollToBottom = _lastState.ScratchText != _scratch;

        if (_lastState != GetState())
            Refresh();
    }

    /// <summary>
    /// Updates the window.
    /// </summary>
    internal void Refresh()
    {
        // If the user has entered text then clear the _clearedScratch variable.
        if (_scratch != "")
            _clearedScratch = "";

        // Cancel any outstanding tokens.
        _cancellationTokenSource?.Cancel();

        // If not preserving the error list, clear it.
        if (!_preserveCorrections)
        {
            _corrections = new();
            _spellChecked = false;
        }
        _preserveCorrections = false;

        // Update the last state.
        _lastState = GetState();

        // Rebuild chunks and reset chunk position counter.
        _chunks = Helpers.ChatHelper.FFXIVify(GetFullChatHeader(), ScratchString, _useOOC);
        _nextChunk = 0;
    }
}
