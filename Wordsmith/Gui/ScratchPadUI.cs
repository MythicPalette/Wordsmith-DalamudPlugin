using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Wordsmith.Data;
using Wordsmith.Enums;
using Wordsmith.Interfaces;

namespace Wordsmith.Gui;

internal class ScratchPadUI : Window, IReflected
{
    /// <summary>
    /// A protected class used only for comparing multiple pad state elements at once.
    /// </summary>
    protected class PadState
    {
        internal ChatType ChatType;
        internal string ScratchText;
        internal bool UseOOC;
        internal string TellTarget;
        internal bool CrossWorld;
        internal int Linkshell;
        public PadState()
        {
            this.ChatType = 0;
            this.ScratchText = "";
            this.UseOOC = false;
            this.TellTarget = "";
        }

        public PadState(ScratchPadUI ui)
        {
            this.ChatType = ui._chatType;
            this.ScratchText = ui._scratch;
            this.TellTarget = ui._telltarget;
            this.Linkshell = ui._linkshell;
            this.UseOOC = ui._useOOC;
            this.CrossWorld = ui._crossWorld;
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
            if (o.Linkshell != this.Linkshell) return false;
            return true;
        }

        public override int GetHashCode() => HashCode.Combine( this.ChatType, this.ScratchText, this.UseOOC, this.TellTarget );

        public override string ToString() => $"{{ ChatType: {this.ChatType}, ScratchText: \"{this.ScratchText}\", UseOOC: {this.UseOOC}, TellTarget: \"{this.TellTarget}\", CrossWorld: {this.CrossWorld}, Linkshell: {this.Linkshell} }}";
    }

    /// <summary>
    /// Contains all of the variables related to ID
    /// </summary>
    #region ID
    protected static int _nextID = 0;
    public static int LastID => _nextID-1;
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

    protected List<Word> _corrections = new();

    #region Alerts
    protected List<(string, int)> _errors = new();
    protected List<(string, int)> _notices = new();
    protected bool _spellChecked = false;
    #endregion

    /// <summary>
    /// Contains all of the variables related to the chat header
    /// </summary>
    #region Chat Header
    internal ChatType _chatType = 0;
    protected bool _header_parse = Wordsmith.Configuration.ParseHeaderInput;
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
    protected string ScratchString => this._scratch.Unwrap();
    protected string _scratch = "";
    protected int _scratchBufferSize = 4096;
    protected bool _useOOC = false;
    private List<TextChunk> _chunks = new();
    protected int _nextChunk = 0;
    protected bool _canUndo = false;
    #endregion

    /// <summary>
    /// Editor state is the functional state of the editor.
    /// </summary>
    protected int _editorstate = Constants.EDITING_TEXT;
    protected bool _textchanged = false;
    protected List<PadState> _text_history = new();

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

    // TODO Refactor
    protected internal System.Timers.Timer _spellchecktimer = new(Wordsmith.Configuration.AutoSpellCheckDelay * 1000) { AutoReset = false, Enabled = false };

    /// <summary>
    /// Gets the slash command (if one exists) and the tell target if one is needed.
    /// </summary>
    internal string GetFullChatHeader() => GetFullChatHeader(this._chatType, this._telltarget, this._crossWorld, this._linkshell);
    internal string GetFullChatHeader(ChatType c, string t, bool cw, int l )
    {
        if ( c == ChatType.None )
            return c.GetShortHeader();

        // Get the slash command.
        string result = c.GetShortHeader();

        // If /tell get the target or placeholder.
        if ( c == ChatType.Tell )
            result += $" {t} ";

        // Grab the linkshell command.
        if ( c == ChatType.Linkshell )
            result = $"/{(cw ? "cw" : "")}linkshell{l + 1}";

        return result;
    }

    /// <summary>
    /// Gets the height of the footer.
    /// </summary>
    /// <param name="IncludeTextbox">If false, the height of the textbox is not added to the result.</param>
    /// <returns></returns>
    public float GetFooterHeight( bool IncludeTextbox = true )
    {
        float result = 70;
        if ( !Wordsmith.Configuration.DeleteClosedScratchPads )
            result += 28;

        if ( IncludeTextbox )
            result += 90;

        if ( this._corrections.Count > 0 )
            result += 32;

        return result * ImGuiHelpers.GlobalScale;
    }

    #region Constructors
    /// <summary>
    /// Default constructor
    /// </summary>
    public ScratchPadUI() : base($"{Wordsmith.AppName} - Scratch Pad #{_nextID}")
    {
        this.ID = NextID;
        this.SizeConstraints = new()
        {
            MinimumSize = ImGuiHelpers.ScaledVector2(400, 300),
            MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
        };

        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        this.Flags |= ImGuiWindowFlags.MenuBar;
        this._spellchecktimer.Elapsed += ( object? s, System.Timers.ElapsedEventArgs e ) => { this.DoSpellCheck(); this._spellchecktimer.Enabled = false; };
    }
    
    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with chat header as Tell and target as
    /// the given <see cref="string"/> argument.
    /// </summary>
    /// <param name="tellTarget">The target to append to the header.</param>
    public ScratchPadUI(string tellTarget) : this()
    {
        this._chatType = ChatType.Tell;
        this._telltarget = tellTarget;
    }

    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with chat header as the given type.
    /// </summary>
    /// <param name="chatType"><see cref="int"/> chat type.</param>
    public ScratchPadUI(int chatType) : this() => this._chatType = (ChatType)chatType;
    #endregion
    #region Overrides
    /// <summary>
    /// The Draw entrypoint for the <see cref="WindowSystem"/> to draw the window.
    /// </summary>
    public override void Draw()
    {
        try
        {
            if ( Wordsmith.Configuration.RecentlySaved )
                Refresh();

            DrawMenu();

            if ( this._editorstate == Constants.EDITING_TEXT )
            {
                DrawAlerts();
                DrawHeader();

                if ( ImGui.BeginChild( $"ScratchPad{this.ID}MainBodyChild", new( 0, -1 ) ) )
                {
                    DrawChunkDisplay();

                    // Draw multi-line input.
                    DrawMultilineTextInput();

                    // We do this here in case the window is being resized and we want
                    // to rewrap the text in the textbox.
                    if ( ImGui.GetWindowWidth() > this._lastWidth + 0.1 || ImGui.GetWindowWidth() < this._lastWidth - 0.1 )
                    {
                        // Don't flag to ignore text edit if the window was just opened.
                        if ( this._lastWidth > 0.1 )
                            this._ignoreTextEdit = true;

                        // Ignore cursor position requirement because we aren't adjusting that
                        // just rewrapping.
                        int ignore = 0;

                        // Rewrap scratch
                        this._scratch = WrapString( this._scratch, ref ignore );

                        // Update the last known width.
                        this._lastWidth = ImGui.GetWindowWidth();
                    }

                    // Draw the word replacement form.
                    DrawWordReplacement();

                    // Draw the buttons at the bottom of the screen.
                    DrawFooter();

                    // Close the child widget.
                    ImGui.EndChild();
                }
            }
            else if ( this._editorstate == Constants.VIEWING_HISTORY )
                DrawHistory();
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// Runs at each framework update.
    /// </summary>
    public override void Update()
    {
        base.Update();

        if ( Wordsmith.Configuration.ReplaceDoubleSpaces )
            _scratch = _scratch.FixSpacing();

        Refresh();
    }
    #endregion
    #region Top
    /// <summary>
    /// Draws the menu bar at the top of the window.
    /// </summary>
    protected void DrawMenu()
    {
        if (ImGui.BeginMenuBar())
        {
            // Start the scratch pad menu
            if (ImGui.BeginMenu($"Scratch Pads##ScratchPadMenu{this.ID}"))
            {
                // New scratchpad button.
                if (ImGui.MenuItem($"New Scratch Pad##NewScratchPad{this.ID}MenuItem"))
                    WordsmithUI.ShowScratchPad(-1); // -1 id always creates a new scratch pad.

                // For each of the existing scratch pads, add a button that opens that specific one.
                foreach (ScratchPadUI w in WordsmithUI.Windows.Where(x => x.GetType() == typeof(ScratchPadUI)).ToArray())
                    if (w.GetType() != typeof(ScratchPadUI) && ImGui.MenuItem($"{w.WindowName}"))
                        WordsmithUI.ShowScratchPad(w.ID);

                // End the scratch pad menu
                ImGui.EndMenu();
            }


            // Text menu
            if (ImGui.BeginMenu($"Text##ScratchPad{this.ID}TextMenu"))
            {
                // Show undo clear text option.
                if ( this._canUndo && this._text_history.Count > 0 )
                    if (ImGui.MenuItem($"Undo Clear##ScratchPad{this.ID}TextUndoClearMenuItem"))
                        UndoClearText();

                // Show the clear text option.
                else
                    if (ImGui.MenuItem($"Clear##ScratchPad{this.ID}TextClearMenuItem"))
                        DoClearText();

                // Spell Check
                if (ImGui.MenuItem($"Spell Check##ScratchPad{this.ID}SpellCheckMenuItem", this._scratch.Length > 0))
                    DoSpellCheck();

                // If there are chunks
                if ( this._chunks.Count > 0)
                {
                    // Create a chunk menu.
                    if (ImGui.BeginMenu($"Chunks##ScratchPad{this.ID}ChunksMenu"))
                    {
                        // Create a copy menu item for each individual chunk.
                        for (int i=0; i< this._chunks.Count; ++i)
                            if (ImGui.MenuItem($"Copy Chunk {i+1}##ScratchPad{this.ID}ChunkMenuItem{i}"))
                                ImGui.SetClipboardText( this._chunks[i].CompleteText);

                        // End chunk menu
                        ImGui.EndMenu();
                    }
                }

                // View/Close history
                if ( this._editorstate == Constants.EDITING_TEXT )
                {
                    if ( ImGui.MenuItem( $"View History##ScratchPad{this.ID}MenuItem" ) )
                        this._editorstate = Constants.VIEWING_HISTORY;
                }
                else if ( this._editorstate == Constants.VIEWING_HISTORY )
                {
                    if ( ImGui.MenuItem( $"Close History##ScratchPad{this.ID}MenuItem" ) )
                        this._editorstate = Constants.EDITING_TEXT;
                }

                // End Text menu
                ImGui.EndMenu();
            }

            // Thesaurus menu item
            if (ImGui.MenuItem($"Thesaurus##ScratchPad{this.ID}ThesaurusMenu"))
                WordsmithUI.ShowThesaurus();

            // Settings menu item
            if (ImGui.MenuItem($"Settings##ScratchPad{this.ID}SettingsMenu"))
                WordsmithUI.ShowSettings();

            // Help menu item
            if (ImGui.MenuItem($"Help##ScratchPad{this.ID}HelpMenu"))
                WordsmithUI.ShowScratchPadHelp();

#if DEBUG
            if ( ImGui.MenuItem( $"Debug UI##ScratchPad{ID}DebugMenu" ) )
                WordsmithUI.ShowDebugUI();
#endif

            //end Menu Bar
            ImGui.EndMenuBar();
        }
    }

    /// <summary>
    /// Draws the chat type selection and the tell target entry box if set to /tell
    /// </summary>
    protected void DrawHeader()
    {
        // Set the column count.
        int default_columns = 3;
        int columns = default_columns;

        // If using Tell or Linkshells, we need 3 columns
        if ( this._chatType == ChatType.Tell || this._chatType == ChatType.Linkshell)
            ++columns;

        if (ImGui.BeginTable($"##ScratchPad{this.ID}HeaderTable", columns))
        {
            // Setup the header lock and chat mode columns.
            ImGui.TableSetupColumn( $"Scratchpad{this.ID}HeaderLockColumn", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale );

            // If there is an extra column, insert it here.
            if ( columns > default_columns )
            {
                ImGui.TableSetupColumn( $"Scratchpad{this.ID}ChatmodeColumn", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale );
                ImGui.TableSetupColumn( $"ScratchPad{this.ID}CustomTargetColumn", ImGuiTableColumnFlags.WidthStretch, 2 );
            }
            else
                ImGui.TableSetupColumn( $"Scratchpad{this.ID}ChatmodeColumn", ImGuiTableColumnFlags.WidthStretch, 2);

            // Setup the OOC column.
            ImGui.TableSetupColumn($"Scratchpad{this.ID}OOCColumn", ImGuiTableColumnFlags.WidthFixed, 75 * ImGuiHelpers.GlobalScale);

            // Header parse lock
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth( -1 );
            if ( Dalamud.Interface.Components.ImGuiComponents.IconButton( (this._header_parse ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Lock) ) )
                this._header_parse = !this._header_parse;
            if ( ImGui.IsItemHovered() )
                ImGui.SetTooltip( $"Temporarily {(this._header_parse ? "locks" : "unlocks")} header parsing on this pad." );

            // Header selection
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth( -1 );

            // Get the header options.
            string[] options = Enum.GetNames(typeof(ChatType));

            // Convert the chat type into a usable int.
            int ctype = (int)this._chatType;

            // Display a combo box and reference ctype. Do not show the last option because it is handled
            // in a different way.
            ImGui.Combo($"##ScratchPad{this.ID}ChatTypeCombo", ref ctype, options, options.Length-1);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the chat header.");

            // Convert ctype back to ChatType and set _chatType
            this._chatType = (ChatType)ctype;

            // Chat target bar is only shown if the mode is tell
            if ( this._chatType == ChatType.Tell)
            {
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint($"##TellTargetText{this.ID}", "User Name@World", ref _telltarget, 128);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enter the user and world or a placeholder here.");
            }

            // Linkshell selection
            else if ( this._chatType == ChatType.Linkshell)
            {
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.Checkbox("Cross-World", ref this._crossWorld );

                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                ImGui.Combo($"##ScratchPad{this.ID}LinkshellCombo", ref this._linkshell, (this._crossWorld ? Wordsmith.Configuration.CrossWorldLinkshellNames : Wordsmith.Configuration.LinkshellNames), 8);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enter a custom targer here such as /cwls1.");
            }

            ImGui.TableNextColumn();
            ImGui.Checkbox("((OOC))", ref this._useOOC );
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
        // Draw all alerts.
        try
        {
            List<(string Message, int Level)> alerts = new();

            // Display errors
            if ( this._errors.Count > 0)
                alerts.AddRange( this._errors );

            // Display notices.
            if (_notices.Count > 0)
                alerts.AddRange( this._notices );

            // Display spelling error message
            if ((this._corrections?.Count ?? 0) > 0)
                alerts.Add(new($"Found {this._corrections!.Count} spelling errors.", Constants.CORRECTIONS_FOUND));

            // If there are no alerts, return.
            if (alerts.Count == 0)
                return;

            // Draw the alerts.
            foreach ( (string Message, int Level) alert in alerts )
            {
                if ( alert.Level < 0 )
                    ImGui.TextColored( new( 255, 0, 0, 255 ), alert.Message );
                else
                    ImGui.Text( alert.Message );
            }
        }

        catch (InvalidOperationException e)
        {
            PluginLog.LogDebug( $"InvalidOperationException in DrawAlerts(). This can be ignored." );
        }
    }
    #endregion
    #region Body
    /// <summary>
    /// Draws the text chunk display.
    /// </summary>
    /// <param name="FooterHeight">The size of the footer elements.</param>
    protected void DrawChunkDisplay()
    {
        // Draw the chunk display
        if (ImGui.BeginChild($"{Wordsmith.AppName}##ScratchPad{this.ID}ChildFrame", new(-1, (this.Size?.Y ?? 25) - GetFooterHeight())))
        {
            // We still perform this check on the property for ShowTextInChunks in case the user is using single line input.
            // If ShowTextInChunks is enabled, we show the text in its chunked state.
            if (Wordsmith.Configuration.ShowTextInChunks)
            {
                for (int i = 0; i < this._chunks.Count; ++i)
                {
                    //// If not the first chunk, add a spacing.
                    if ( i > 0 )
                        ImGui.Spacing();

                    // Put a separator at the top of the chunk.
                    ImGui.Separator();

                    if (Wordsmith.Configuration.EnableTextHighlighting)
                        DrawChunkItem( this._chunks[i], this._chatType );
                    else
                    {
                        // Set width and display the chunk.
                        ImGui.SetNextItemWidth( -1 );
                        ImGui.TextWrapped( this._chunks[i].CompleteText );
                    }
                }
            }
            // If it's disabled and the user has enabled UseOldSingleLineInput then we still need to draw a display for them.
            else
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.TextWrapped($"{GetFullChatHeader()}{(this._useOOC ? "(( " : "")}{this.ScratchString}{(this._useOOC ? " ))" : "")}");
            }

            if ( this._textchanged )
            {
                ImGui.SetScrollHereY();
                this._textchanged = false;
            }
            ImGui.EndChild();
        }
        ImGui.Separator();
        ImGui.Spacing();
    }

    protected void DrawChunkItem(TextChunk chunk, ChatType ct)
    {
        // Split it into words.

        float width = 0f;

        bool sameLine = false;

        // Draw header
        if ( chunk.Header.Length > 0 )
        {
            ImGui.TextColored( Wordsmith.Configuration.HeaderColors[(int)ct], chunk.Header.Replace("%", "%%") );
            width += ImGui.CalcTextSize( chunk.Header ).X;
            sameLine = true;
        }

        // Draw OOC
        if (_useOOC)
        {
            ImGui.SameLine( 0, 2 * ImGuiHelpers.GlobalScale );
            ImGui.Text( Wordsmith.Configuration.OocOpeningTag.Replace( "%", "%%") );
            width += ImGui.CalcTextSize( Wordsmith.Configuration.OocOpeningTag ).X;
            sameLine = true;
        }

        // Draw body
        for (int i = 0; i < chunk.Words.Length; ++i)
        {
            // Get the first word
            Word word = chunk.Words[i];

            // Get the word string
            string text = word.GetString(chunk.Text);

            float objWidth = ImGui.CalcTextSize(text).X;
            float spacing = 3*ImGuiHelpers.GlobalScale;

            width += objWidth+spacing;
            if ( (i > 0 || sameLine) && width < ImGui.GetWindowContentRegionMax().X )
                ImGui.SameLine( 0, spacing );
            else
                width = objWidth;

            if ( _corrections?.Count > 0 && _corrections[0].StartIndex == word.StartIndex+chunk.StartIndex)
                ImGui.TextColored( Wordsmith.Configuration.SpellingErrorHighlightColor, text.Replace( "%", "%%") );
            else
                ImGui.Text(text.Replace( "%", "%%"));

#if DEBUG
            if ( ImGui.IsItemHovered() )
                ImGui.SetTooltip( $"StartIndex: {word.StartIndex}, EndIndex: {word.EndIndex}, WordIndex: {word.WordIndex}, WordLength: {word.WordLength}" );
#endif
        }

        // Draw OOC
        if ( _useOOC )
        {
            // Calculate new width.
            width += ImGui.CalcTextSize( Wordsmith.Configuration.OocClosingTag ).X;

            // If width is within bounds then draw same line.
            if ( width < ImGui.GetWindowContentRegionMax().X )
                ImGui.SameLine( 0, 2 * ImGuiHelpers.GlobalScale );

            // If out of bounds reset width to new width.
            else
                width = ImGui.CalcTextSize( Wordsmith.Configuration.OocClosingTag ).X;

            // Draw text.
            ImGui.Text( Wordsmith.Configuration.OocClosingTag.Replace( "%", "%%") );
        }

        if ( this._chunks.Count > 1)
        {
            if ( chunk.ContinuationMarker.Length > 0)
            {
                // Calculate new width.
                width += ImGui.CalcTextSize( chunk.ContinuationMarker ).X;

                // If width is within bounds then draw same line.
                if ( width < ImGui.GetWindowContentRegionMax().X )
                    ImGui.SameLine( 0, 2 * ImGuiHelpers.GlobalScale );

                // If out of bounds reset width to new width.
                else
                    width = ImGui.CalcTextSize( chunk.ContinuationMarker ).X;

                ImGui.Text( chunk.ContinuationMarker.Replace( "%", "%%") );
            }
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
            v = new(-1, (this.Size?.Y ?? 25) - GetFooterHeight(false));

        // If the user has their option set to SpellCheck or Copy then
        // handle it with an EnterReturnsTrue.
        if (ImGui.InputTextMultiline($"##ScratchPad{this.ID}MultilineTextEntry",
            ref this._scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength,
            v,
            ImGuiInputTextFlags.CallbackEdit |
            ImGuiInputTextFlags.CallbackAlways |
            ImGuiInputTextFlags.EnterReturnsTrue,
            OnTextCallback))
        {
            // If the user hits enter, run the user-defined action.
            if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == EnterKeyAction.SpellCheck)
                DoSpellCheck();

            else if (Wordsmith.Configuration.ScratchPadTextEnterBehavior == EnterKeyAction.CopyNextChunk)
                DoCopyToClipboard();
        }
    }

    /// <summary>
    /// Draws the word replacement section if there are known spelling errors.
    /// </summary>
    protected void DrawWordReplacement()
    {
        if ( this._corrections.Count > 0)
        {
            int index = 0;
            // Get the fist incorrect word.
            Word word = this._corrections[index];

            // Notify of the spelling error.
            ImGui.TextColored(new(255, 0, 0, 255), "Spelling Error:");

            // Draw the text input.
            ImGui.SameLine(0,0);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize("Spelling Error: ").X - (120*ImGuiHelpers.GlobalScale));
            this._replaceText = word.GetWordString( this._scratch );

            if (ImGui.InputText($"##ScratchPad{this.ID}ReplaceTextTextbox", ref this._replaceText, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                OnReplace( index );

            // If the user right clicks the text show the popup.
            bool showPop = ImGui.IsItemClicked(ImGuiMouseButton.Right);
            if ( showPop )
                ImGui.OpenPopup( $"ScratchPad{this.ID}ReplaceContextMenu" );

            // Build the popup.
            if ( ImGui.BeginPopup( $"ScratchPad{this.ID}ReplaceContextMenu" ) )
            {
                // Create a selectable to add the word to dictionary.
                if ( ImGui.Selectable( $"Add To Dictionary##ScratchPad{this.ID}ReplaceContextMenu" ) )
                    OnAddToDictionary( index );

                // If the suggestions haven't been generated then try to.
                if ( word.Suggestions is null )
                {
                    word.Suggestions = new List<string>();
                    new Thread( () => {
                        Lang.GetSuggestions( ref word.Suggestions, word.GetWordString( this._scratch ));
                    })
                        .Start();
                }

                // If there is more than zero suggestions put a separator.
                if (word.Suggestions.Count > 0)
                    ImGui.Separator();

                float childwidth = ImGui.CalcTextSize(this._replaceText).X + (20*ImGuiHelpers.GlobalScale) > ImGui.CalcTextSize(" Add To Dictionary ").X ? ImGui.CalcTextSize(this._replaceText).X + (20*ImGuiHelpers.GlobalScale) : ImGui.CalcTextSize(" Add To Dictionary ").X;
                if ( ImGui.BeginChild( $"ScratchPad{this.ID}ReplaceContextChild",
                    new(
                        childwidth,
                        (word.Suggestions.Count > 5 ? 105 : word.Suggestions.Count * 21)*ImGuiHelpers.GlobalScale
                        )))
                {
                    // List the suggestions.
                    foreach ( string suggestion in word.Suggestions )
                    {
                        if ( ImGui.Selectable( $"{suggestion}##ScratchPad{this.ID}Replacement" ) )
                        {
                            _replaceText = $"{suggestion}";
                            OnReplace( 0 );
                        }
                    }
                    ImGui.EndChild();
                }

                ImGui.EndPopup();
            }
            // If they mouse over the input, tell them to use the enter key to replace.
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Fix the spelling of the word and hit enter or\nclick the \"Add to Dictionary\" button.");

            // Add to dictionary button
            ImGui.SameLine();
            ImGui.SetNextItemWidth( 120 * ImGuiHelpers.GlobalScale );
            if ( ImGui.Button( $"Add To Dictionary##ScratchPad{this.ID}" ) )
                OnAddToDictionary( 0 );
        }
    }
    #endregion
    #region Bottom
    /// <summary>
    /// Draws the buttons at the foot of the window.
    /// </summary>
    protected void DrawFooter()
    {
        
        if (ImGui.BeginTable($"{this.ID}FooterButtonTable", (Wordsmith.Configuration.AutoSpellCheck ? 2 : 3)))
        {
            // Setup the three columns for the buttons. I use a table here for easy space sharing.
            // The table will handle all sizing and positioning of the buttons automatically with no
            // extra input from me.
            ImGui.TableSetupColumn($"{this.ID}FooterCopyColumn", ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn($"{this.ID}FooterClearButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);
            //ImGui.TableSetupColumn($"{this.ID}FooterSpellCheckButtonColumn", ImGuiTableColumnFlags.WidthStretch, 1);

            // Draw the copy button.
            ImGui.TableNextColumn();
            DrawCopyButton();

            // Draw the clear button.
            ImGui.TableNextColumn();
            DrawClearButton();

            // If spell check is disabled, make the button dark so it appears as though it is disabled.
            if (!Lang.Enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);

            if ( !Wordsmith.Configuration.AutoSpellCheck )
            {
                // Draw the spell check button.
                ImGui.TableNextColumn();
                if ( this._scratch.Length == 0 )
                    ImGui.BeginDisabled();

                if ( ImGui.Button( $"Spell Check##Scratch{this.ID}", ImGuiHelpers.ScaledVector2( -1, 25 ) ) )
                    if ( Lang.Enabled ) // If the dictionary is functional then do the spell check.
                        DoSpellCheck();

                if ( this._scratch.Length == 0 )
                    ImGui.EndDisabled();

                // If spell check is disabled, pop the stylevar to return to normal.
                if ( !Lang.Enabled )
                    ImGui.PopStyleVar();
            }

            ImGui.EndTable();
        }

        // If not configured to automatically delete scratch pads, draw the delete button.
        if (!Wordsmith.Configuration.DeleteClosedScratchPads)
        {
            if (ImGui.Button($"Delete Pad##Scratch{this.ID}", ImGuiHelpers.ScaledVector2(-1, 25)))
            {
                if ( Wordsmith.Configuration.ConfirmCloseScratchPads )
                {
                    WordsmithUI.ShowMessageBox( "Confirm Delete", $"Are you sure you want to delete this pad?", ( mb ) =>
                    {
                        if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                            WordsmithUI.RemoveWindow( this );
                    },
                    ImGuiHelpers.ScaledVector2(200, 100));
                }
                else
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
        if ( this._chunks.Count > 1)
        {
            // Push the icon font for the character we need then draw the previous chunk button.
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)0xF100}##{this.ID}ChunkBackButton", ImGuiHelpers.ScaledVector2(25, 25)))
            {
                --this._nextChunk;
                if ( this._nextChunk < 0)
                    this._nextChunk = this._chunks.Count - 1;
            }
            // Reset the font.
            ImGui.PushFont(UiBuilder.DefaultFont);

            // Draw the copy button with no spacing.
            ImGui.SameLine(0, 0);
            if (ImGui.Button($"Copy{(this._chunks.Count > 1 ? $" ({this._nextChunk + 1}/{this._chunks.Count})" : "")}##ScratchPad{this.ID}", new(ImGui.GetColumnWidth() - (23 * ImGuiHelpers.GlobalScale), 25 * ImGuiHelpers.GlobalScale)))
                DoCopyToClipboard();

            // Push the font and draw the next chunk button with no spacing.
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.SameLine(0, 0);
            if (ImGui.Button($"{(char)0xF101}##{this.ID}ChunkBackButton", ImGuiHelpers.ScaledVector2(25, 25)))
            {
                ++this._nextChunk;
                if ( this._nextChunk >= this._chunks.Count)
                    this._nextChunk = 0;
            }
            // Reset the font.
            ImGui.PushFont(UiBuilder.DefaultFont);
        }
        else // If there is only one chunk simply draw a normal button.
        {
            if (ImGui.Button($"Copy{(this._chunks.Count > 1 ? $" ({this._nextChunk + 1}/{this._chunks.Count})" : "")}##ScratchPad{this.ID}", new(-1, 25 * ImGuiHelpers.GlobalScale)))
                DoCopyToClipboard();
        }
    }

    /// <summary>
    /// Draws the copy button depending on how many chunks are available.
    /// </summary>
    protected void DrawClearButton()
    {
        // If undo is enabled and there is history.
        if ( this._canUndo && this._text_history.Count > 0 )
        {
            if (ImGui.Button($"Clear##ScratchPad{this.ID}", new(ImGui.GetColumnWidth() - (23 * ImGuiHelpers.GlobalScale), 25 * ImGuiHelpers.GlobalScale)))
                DoClearText();

            // Push the font and draw the next chunk button with no spacing.
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.SameLine(0, 0);
            if (ImGui.Button($"{(char)0xF0E2}##{this.ID}UndoClearButton", ImGuiHelpers.ScaledVector2(25, 25)))
                UndoClearText();

            // Reset the font.
            ImGui.PushFont(UiBuilder.DefaultFont);
        }
        else // If there is only one chunk simply draw a normal button.
        {
            if (ImGui.Button($"Clear##ScratchPad{this.ID}", new(-1, 25 * ImGuiHelpers.GlobalScale)))
                DoClearText();
        }
    }
    #endregion
    #region History
    /// <summary>
    /// Draws the user's message history.
    /// </summary>
    protected void DrawHistory()
    {
        try
        {
            for ( int i = 0; i < this._text_history.Count; ++i )//( PadState p in this._text_history )
                DrawHistoryItem( this._text_history[i], i );
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    // TODO Move this to appropriate area
    protected int _selected_history = -1;

    /// <summary>
    /// Draws individual items in the history list.
    ///
    /// </summary>
    /// <param name="s">The text for the history.</param>
    protected void DrawHistoryItem(PadState p, int idx)
    {
        try
        {
            // Create a group.
            ImGui.BeginGroup();

            // Get the text chunks.
            List<TextChunk> tlist = Helpers.ChatHelper.FFXIVify(GetFullChatHeader(p.ChatType, p.TellTarget, p.CrossWorld, p.Linkshell), p.ScratchText, p.UseOOC);

            // Display the chunks.
            for ( int i = 0; i < tlist.Count; ++i )
            {
                if ( i > 0 )
                    ImGui.Spacing();

                DrawChunkItem( tlist[i], p.ChatType );
            }

            // End the group.
            ImGui.EndGroup();

            // Create a border around the group
            Rect2 r = new(ImGui.GetItemRectMin(), new( ImGui.GetWindowWidth() - 20 * ImGuiHelpers.GlobalScale, ImGui.GetItemRectMax().Y - ImGui.GetItemRectMin().Y));
            ImGui.GetWindowDrawList().AddRect( r.Position, r.Size + r.Position, ImGui.GetColorU32( ImGuiCol.Text ) );

            // TODO rebuild the history item.
            // * Replace scratch ( Confirmation if scratch not empty. )
            // * Replace header ( Confirmation if header locked. )
            // * Replace OOC.
            if ( ImGui.BeginPopup( $"ScratchPad{this.ID}History{idx}Popup" ) )
            {
                if ( ImGui.MenuItem( $"Reload Pad State##ScratchPad{this.ID}HistoryItem{idx}Reload" ) )
                {
                    // Get the current pad state
                    PadState snapshot = new( this );

                    // Get the selected pad state
                    PadState selected = this._text_history[this._selected_history];

                    // Revert to given padstate
                    this._chatType = selected.ChatType;
                    this._telltarget = selected.TellTarget;
                    this._crossWorld = selected.CrossWorld;
                    this._linkshell = selected.Linkshell;
                    this._scratch = selected.ScratchText;

                    // Exit history.
                    this._editorstate = Constants.EDITING_TEXT;

                    // Save the pre-change pad state.
                    AppendHistory( snapshot );

                    // Unselect the history time.
                    this._selected_history = -1;

                    // Close the popup
                    ImGui.CloseCurrentPopup();
                }
                else if ( ImGui.MenuItem( $"Copy Text To Clipboard##ScratchPad{this.ID}HistoryItem{idx}Copy" ) )
                {
                    // Get the selected pad state
                    PadState selected = this._text_history[this._selected_history];

                    // Get each chunk
                    List<TextChunk> chunks = Helpers.ChatHelper.FFXIVify(GetFullChatHeader(selected.ChatType, selected.TellTarget, selected.CrossWorld, selected.Linkshell), selected.ScratchText, selected.UseOOC);

                    // Get the text from every chunk.
                    List<string> text = new();
                    foreach ( TextChunk t in chunks )
                        text.Add( t.CompleteText );

                    // Set the clipboard text.
                    ImGui.SetClipboardText( string.Join( '\n', text.ToArray() ) );

                    // Notify the user that the text was copied.
                    Wordsmith.PluginInterface.UiBuilder.AddNotification( "Copied text to clipboard!", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Success );

                    // Unselect the history time.
                    this._selected_history = -1;

                    // Close the popup
                    ImGui.CloseCurrentPopup();
                }
                else if ( ImGui.MenuItem( $"Delete##ScratchPad{this.ID}HistoryItem{idx}Delete" ) )
                {
                    // Remove the history item.
                    this._text_history.RemoveAt( this._selected_history );

                    // Unselect the history time.
                    this._selected_history = -1;

                    // Close the popup
                    ImGui.CloseCurrentPopup();
                }
                else if ( ImGui.MenuItem( $"Delete All##ScratchPad{this.ID}HistoryItem{idx}DeleteAll" ) )
                {
                    // Remove the history item.
                    this._text_history.Clear();

                    // Unselect the history time.
                    this._selected_history = -1;

                    // Close the popup
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
            if ( ImGui.IsMouseClicked( ImGuiMouseButton.Right ) && r.Contains( ImGui.GetMousePos() ) )
            {
                ImGui.OpenPopup( $"ScratchPad{this.ID}History{idx}Popup" );
                this._selected_history = idx;
            }
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }
    
    /// <summary>
    /// Creates a new history entry while moving duplicates and overflow.
    /// </summary>
    /// <param name="p">The <see cref="PadState"/> to be added to history.</param>
    protected void AppendHistory( PadState p )
    {
        try
        {
            // Save the text state in case it was an accidental 
            // rebuild of the state.
            if ( p.ScratchText.Length > 0 )
            {
                // Remove any duplicates of this pad state to prevent
                // building a list of the same edit.
                this._text_history.RemoveAll( x => x.Equals( p ) );

                // Add the history to the end of the list.
                this._text_history.Add( p );
            }

            // If there are too many history states, remove the extra(s).
            if ( this._text_history.Count > Wordsmith.Configuration.ScratchPadHistoryLimit )
                this._text_history.RemoveAt( 0 );
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    #endregion
    #region Button Backend
    /// <summary>
    /// Gets the next chunk of text and copies it to the player's clipboard.
    /// </summary>
    protected void DoCopyToClipboard()
    {
        try
        {
            // If there are no chunks to copy exit the function.
            if ( this._chunks.Count == 0 )
                return;

            // Copy the next chunk over.
            ImGui.SetClipboardText( this._chunks[this._nextChunk++].CompleteText.Trim() );

            // If we're not at the last chunk, return.
            if ( this._nextChunk < this._chunks.Count )
                return;

            // After this point, we assume we've copied the last chunk.
            this._nextChunk = 0;

            // If configured to clear text after last copy
            if ( Wordsmith.Configuration.AutomaticallyClearAfterLastCopy )
                DoClearText();
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// Moves the text from the textbox to a hidden variable in case the user
    /// wants to undo the change.
    /// </summary>
    protected void DoClearText()
    {
        try
        {
            // Ignore empty strings.
            if ( this._scratch.Length == 0 )
                return;

            // Create a history state.
            AppendHistory( new( this ) );

            // Clear any corrections.
            this._corrections = new();

            // Clear scratch.
            this._scratch = "";

            // Enable undo.
            this._canUndo = true;
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// Saves the cleared text in case the user wants to undo the clear then
    /// clears it.
    /// </summary>
    protected void UndoClearText()
    {
        try
        {
            // If undo is locked or there are no previous states then abort
            if ( !this._canUndo || this._text_history.Count == 0 )
                return;

            // Get the last pad state
            PadState p = this._text_history.Last();

            // Recover the text
            this._scratch = p.ScratchText;

            // disable undoing further.
            this._canUndo = false;

            // Delete the history entry.
            this._text_history.Remove( p );
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// Clears out any error messages or notices and runs the spell checker.
    /// </summary>
    protected void DoSpellCheck()
    {
        try
        {
#if DEBUG
            PluginLog.LogDebug( "Running spell check." );
#endif

            // If there are any outstanding tokens, cancel them.
            _cancellationTokenSource?.Cancel();

            // Clear any errors and notifications.
            _notices.Clear();

            // Don't spell check an empty input.
            if ( _scratch.Length == 0 )
                return;

            // Notify the user that spelling is being checked.
            _notices.Add( (Constants.SPELL_CHECK_NOTICE, Constants.CHECKING_SPELLING) );

            // Create a new token source.
            this._cancellationTokenSource = new();

            // Create and start the spell check task.
            Task t = new(() => DoSpellCheckAsync(this._cancellationTokenSource.Token));
            t.Start();
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// The spell check task to run.
    /// </summary>
    protected unsafe void DoSpellCheckAsync(CancellationToken token)
    {
        try
        {
            if ( token.IsCancellationRequested )
                return;
            // Clear any old corrections to prevent them from stacking.
            Word[] results = Helpers.SpellChecker.CheckString( this._scratch, token );

#if DEBUG
            PluginLog.LogDebug( $"Found {results.Length} errors." );
#endif


            this._notices.RemoveAll( x => x.Item2 == Constants.CHECKING_SPELLING );

            if ( token.IsCancellationRequested )
            {
#if DEBUG
                PluginLog.LogDebug( "Cancelling spell check." );
#endif
                return;
            }
            this._corrections = new();
            this._corrections.AddRange( results );
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }
    #endregion
    #region Callbacks
    /// <summary>
    /// Replaces spelling errors with the given text or ignores an error if _replaceText is blank
    /// </summary>
    /// <param name="index"><see cref="int"/> index of the correction in correction list.</param>
    protected void OnReplace(int index)
    {
        try
        {
            // If the text box is not empty when the user hits enter then
            // update the text.
            if ( this._replaceText.Length > 0 && index < this._corrections.Count )
            {
                // Get the first object
                Word word = this._corrections[index];

                // Get the string builder on the unwrapped scratch string.
                StringBuilder sb = new(this._scratch.Unwrap());

                // Remove the original word.
                sb.Remove( word.WordIndex, word.WordLength );

                // Insert the new text.
                sb.Insert( word.WordIndex, this._replaceText.Trim() );

                this._scratch = sb.ToString();

                // Remove the word from the list.
                this._corrections.Remove( word );

                // Adjust the index of all following words.
                if ( this._replaceText.Length != word.WordLength )
                {
                    foreach ( Word w in this._corrections )
                        w.Offset( this._replaceText.Length - word.WordLength );
                }

#if DEBUG
                if ( this._replaceText == "CRASHTEST" )
                    WordsmithUI.ShowErrorWindow( this.Dump(), $"ScratchPad{this.ID}CrashTestErrorWindow" );
#endif

                // Clear out replacement text.
                this._replaceText = "";

                int ignore = -1;
                this._scratch = CheckForHeader( this._scratch, ref ignore );

                ignore = 0;
                // Rewrap the text string.
                this._scratch = WrapString( this._scratch, ref ignore );
            }
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// Adds the word to the dictionary and removes any subsequent correction requestions with
    /// the same word in it.
    /// </summary>
    /// <param name="index"><see cref="int"/> index of the correction in correction list.</param>
    protected void OnAddToDictionary(int index)
    {
        try
        {
            Word word = this._corrections[index];
            // Get the word
            string newWord = word.GetWordString(this._scratch);

            // Add the cleaned word to the dictionary.
            Lang.AddDictionaryEntry( newWord );

            // Remove the cleaned word from the dictionary
            this._corrections.RemoveAt( index );

            // Get rid of any spelling corrections with the same word
            foreach ( Word w in this._corrections )
                if ( this._scratch.Substring( w.WordIndex, w.WordLength ) == newWord )
                    this._corrections.Remove( w );

            if ( this._corrections.Count == 0 )
                Refresh();
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }

    /// <summary>
    /// Handles automatically deleting the pad if configured to do so.
    /// </summary>
    public override void OnClose()
    {
        if (Wordsmith.Configuration.DeleteClosedScratchPads)
        {
            this._cancellationTokenSource?.Cancel();
            WordsmithUI.RemoveWindow(this);
        }
    }

    /// <summary>
    /// Identifies the callback type and routes it accordingly
    /// </summary>
    /// <param name="data">Pointer to <see cref="ImGuiInputTextCallbackData"/></param>
    /// <returns>Always returns 0</returns>
    public unsafe int OnTextCallback( ImGuiInputTextCallbackData* data )
    {
        try
        {
            if ( data->EventFlag == ImGuiInputTextFlags.CallbackAlways )
            {
                // If the user hits the right key and this makes it so that a \r character is to the left of the cursor
                // move the cursor again until passed all \r keys. This will make the cursor go from \r|\r\n to \r\r|\n
                // then to \r\r\n|
                if ( ImGui.IsKeyPressed( ImGuiKey.RightArrow ) )//ImGui.GetKeyIndex(ImGuiKey.RightArrow)))
                {
                    while ( data->CursorPos < data->BufTextLen && data->Buf[(data->CursorPos - 1 > 0 ? data->CursorPos - 1 : 0)] == '\r' )
                    {
                        data->CursorPos++;
#if DEBUG
                        PluginLog.LogDebug( $"Moved cursor to the right." );
#endif
                    }
                }
                if ( data->CursorPos > 0 )
                {
                    while ( data->CursorPos > 0 && data->Buf[data->CursorPos - 1] == '\r' )
                    {
                        data->CursorPos--;
#if DEBUG
                        PluginLog.LogDebug( "Moved cursor to the left." );
#endif
                    }
                }
            }
            else
            {
                this._corrections?.Clear();
                OnTextEdit( data );

                if ( Wordsmith.Configuration.AutoSpellCheck )
                {
                    this._spellchecktimer.Stop();

                    if ( this._scratch.Length > 0 )
                        this._spellchecktimer.Start();
                }
            }
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
        return 0;
    }

    /// <summary>
    /// Alters text input buffer in real time to create word wrap functionality in multiline textbox.
    /// </summary>
    /// <param name="data">Pointer to callback data</param>
    /// <returns></returns>
    public unsafe int OnTextEdit(ImGuiInputTextCallbackData* data)
    {
        try
        {
            // If _ignoreTextEdit is true then the reason for the edit
            // was a resize and the text has already been wrapped so
            // we simply return from here.
            if ( this._ignoreTextEdit )
            {
                this._ignoreTextEdit = false;
                return 0;
            }

            UTF8Encoding utf8 = new();

            // For some reason, ImGui's InputText never verifies that BufTextLen never goes negative
            // which can lead to some serious problems and crashes with trying to get the string.
            // Here we do the check ourself with the turnery operator. If it does happen to be
            // a negative number, return a blank string so the rest of the code can continue as normal
            // at which point the buffer will be cleared and BufTextLen will be set to 0, preventing any
            // memory damage or crashes.
            string txt = data->BufTextLen >= 0 ? utf8.GetString(data->Buf, data->BufTextLen) : "";

            int pos = data->CursorPos;

            // Check for header input.
            if ( _header_parse )
                txt = CheckForHeader( txt, ref pos );

            // Wrap the string if there is enough there.
            if ( txt.Length > 0 )
                txt = WrapString( txt, ref pos );

            // Convert the string back to bytes.
            byte[] bytes = utf8.GetBytes(txt);

            // Replace with new values.
            for ( int i = 0; i < bytes.Length; ++i )
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

            // Set the text changed flag.
            _textchanged = true;
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
        // Return 0 to signal no errors.
        return 0;
    }
    #endregion
    #region String Manipulation
    /// <summary>
    /// Check for a chat header at the start of a string.
    /// </summary>
    /// <param name="text">Text to parse from</param>
    /// <param name="cursorPos">Cursor position</param>
    /// <returns>The text string with header removed.</returns>
    protected string CheckForHeader(string text, ref int cursorPos)
    {
        try
        {
            // The text must have a length and must start with a slash. If
            // there is no slash, it is impossible to contain a header.
            if ( text.Length > 1 )
            {
                // Default to ChatType None
                HeaderData headerData = new(text);

                // If the header data was not validated return to avoid
                // the rest of the checks.
                if ( !headerData.Valid )
                    return text;

#if DEBUG
                PluginLog.LogDebug( $"txt :: {text} | head :: {headerData.ChatType}" );
#endif

                //If a chat header was found
                if ( headerData.ChatType != ChatType.None && cursorPos >= headerData.Length )
                {
                    this._chatType = headerData.ChatType;
                    this._linkshell = headerData.Linkshell;
                    this._crossWorld = headerData.CrossWorld;
                    this._telltarget = headerData.TellTarget;

                    text = text.Remove( 0, headerData.Length );
                    cursorPos -= headerData.Length;
                }

                // If the header hasn't been detected, check for a corresponding alias
            }
            return text;
        }

        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
        return "";
    }

    /// <summary>
    /// Takes a string and wraps it based on the current width of the window.
    /// </summary>
    /// <param name="text">The string to be wrapped.</param>
    /// <returns></returns>
    protected string WrapString(string text, ref int cursorPos)
    {
        try
        {
            // If the string is empty then just return it.
            if ( text.Length == 0 )
                return text;

            // Trim any return carriages off the end. This can happen if the user
            // backspaces a new line character off of the end.
            text = text.TrimEnd( '\r' );

            // Replace all wrap markers with spaces and adjust cursor offset. Do this before
            // all non-spaced wrap markers because the Spaced marker contains the nonspaced marker
            while ( text.Contains( Constants.SPACED_WRAP_MARKER + '\n' ) )
            {
                int idx = text.IndexOf(Constants.SPACED_WRAP_MARKER + '\n');
                text = text[0..idx] + " " + text[(idx + (Constants.SPACED_WRAP_MARKER + '\n').Length)..^0];

                // We adjust the cursor position by one less than the wrap marker
                // length to account for the space that replaces it.
                if ( cursorPos > idx )
                    cursorPos -= Constants.SPACED_WRAP_MARKER.Length;

            }

            while ( text.Contains( Constants.NOSPACE_WRAP_MARKER + '\n' ) )
            {
                int idx = text.IndexOf(Constants.NOSPACE_WRAP_MARKER + '\n');
                text = text[0..idx] + text[(idx + (Constants.NOSPACE_WRAP_MARKER + '\n').Length)..^0];

                if ( cursorPos > idx )
                    cursorPos -= (Constants.NOSPACE_WRAP_MARKER + '\n').Length;
            }

            // Replace double spaces if configured to do so.
            if ( Wordsmith.Configuration.ReplaceDoubleSpaces )
                text = text.FixSpacing( ref cursorPos );

            // Get the maximum allowed character width.
            float width = this._lastWidth - (35 * ImGuiHelpers.GlobalScale);

            // Iterate through each character.
            int lastSpace = 0;
            int offset = 0;
            for ( int i = 1; i < text.Length; ++i )
            {
                // If the current character is a space, mark it as a wrap point.
                if ( text[i] == ' ' )
                    lastSpace = i;

                // If the size of the text is wider than the available size
                float txtWidth = ImGui.CalcTextSize(text[offset..i ]).X;
                if ( txtWidth + 10 * ImGuiHelpers.GlobalScale > width )
                {
                    // Replace the last previous space with a new line
                    StringBuilder sb = new(text);

                    if ( lastSpace > offset )
                    {
                        sb.Remove( lastSpace, 1 );
                        sb.Insert( lastSpace, Constants.SPACED_WRAP_MARKER + '\n' );
                        offset = lastSpace + Constants.SPACED_WRAP_MARKER.Length;
                        i += Constants.SPACED_WRAP_MARKER.Length;

                        // Adjust cursor position for the marker but not
                        // the new line as the new line is replacing the space.
                        if ( lastSpace < cursorPos )
                            cursorPos += Constants.SPACED_WRAP_MARKER.Length;
                    }
                    else
                    {
                        sb.Insert( i, Constants.NOSPACE_WRAP_MARKER + '\n' );
                        offset = i + Constants.NOSPACE_WRAP_MARKER.Length;
                        i += Constants.NOSPACE_WRAP_MARKER.Length;

                        // Adjust cursor position for the marker and the
                        // new line since both are inserted.
                        if ( cursorPos > i - Constants.NOSPACE_WRAP_MARKER.Length )
                            cursorPos += Constants.NOSPACE_WRAP_MARKER.Length + 1;
                    }
                    text = sb.ToString();
                }
            }
            return text;
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
        return "";
    }
    #endregion

    /// <summary>
    /// Updates the window.
    /// </summary>
    internal void Refresh()
    {
        try
        {
            PadState newState = new(this);
            if ( this._lastState == newState )
                return;

            // If text has been entered, disable undo.
            if ( this._scratch.Length > 0 )
                this._canUndo = false;

            // Update the last state.
            this._lastState = newState;

            // Rebuild chunks and reset chunk position counter.
            this._chunks = Helpers.ChatHelper.FFXIVify( GetFullChatHeader(), this.ScratchString, this._useOOC );
            this._nextChunk = 0;

            this._spellchecktimer.Interval = Wordsmith.Configuration.AutoSpellCheckDelay * 1000;
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = this.Dump();
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ScratchPad{this.ID}ErrorWindow" );
        }
    }
    internal Dictionary<string, object> Dump()
    {
        // Get the list of results
        IReadOnlyList<(int Type, string Name, string Value) > data = this.GetProperties();

        Dictionary<string, object> result = new Dictionary<string, object>();
        // Get Properties
        foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 0 ) )
            result[Name] = Value;

        // Get Fields
        foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 1 ) )
            result[Name] = Value;

        return result;
    }
}
