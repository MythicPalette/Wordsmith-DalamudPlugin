using System.ComponentModel;
using Dalamud.Interface;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Wordsmith.Enums;
using Wordsmith.Helpers;

namespace Wordsmith.Gui;

internal sealed class ScratchPadUI : Window
{
    #region NoticeID
    internal const int CORRECTIONS_FOUND = -1;

    internal const int CHECKING_SPELLING = 1;
    internal const int CORRECTIONS_NOT_FOUND = 2;
    #endregion

    #region ScratchPad
    internal const int EDITING_TEXT = 0;
    internal const int VIEWING_HISTORY = 1;
    #endregion

    /// <summary>
    /// A private class used only for comparing multiple pad state elements at once.
    /// </summary>
    private class PadState
    {
        internal string ScratchText;
        internal bool UseOOC;
        internal HeaderData? Header = null;

        public PadState()
        {
            this.ScratchText = "";
            this.UseOOC = false;
        }

        public PadState(ScratchPadUI ui)
        {
            this.ScratchText = ui._scratch;
            this.UseOOC = ui._useOOC;
            this.Header = ui._header;
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
            if (o.ScratchText != this.ScratchText) return false;
            if (o.UseOOC != this.UseOOC) return false;
            return true;
        }

        public override int GetHashCode() => HashCode.Combine( this.Header?.ChatType, this.ScratchText, this.UseOOC, this.Header?.TellTarget );

        public override string ToString() => $"{{ ChatType: {this.Header?.ChatType}, ScratchText: \"{this.ScratchText}\", UseOOC: {this.UseOOC}, TellTarget: \"{this.Header?.TellTarget ?? ""}\", CrossWorld: {this.Header?.CrossWorld}, Linkshell: {this.Header?.Linkshell} }}";
    }

    /// <summary>
    /// Contains all of the variables related to ID
    /// </summary>
    #region ID
    private static int _nextID = 0;
    private static int NextAvailableID()
    {
        int id;
        string s;
        do
        {
            id = ++_nextID;
            s = CreateWindowName(id);
        } while ( WordsmithUI.Contains( s ) );
        return id;
    }
    public int ID { get; set; }
    #endregion

    /// <summary>
    /// Contains all of the variables related to the PadState
    /// </summary>
    private PadState _lastState = new();

    private List<Word> _corrections = new();

    #region Alerts
    private List<(string, int)> _errors = new();
    private List<(string, int)> _notices = new();
    #endregion

    /// <summary>
    /// Contains all of the variables related to the chat header
    /// </summary>
    #region Chat Header
    private bool _header_parse = Wordsmith.Configuration.ParseHeaderInput;
    private HeaderData _header = new(true);
    public HeaderData Header => this._header;
    #endregion

    /// <summary>
    /// Contains all of the variables related to chat text.
    /// </summary>
    #region Chat Text
    /// <summary>
    /// Returns a trimmed, single-line version of scratch.
    /// </summary>
    private string ScratchString
    {
        get => this._scratch;
        set
        {
            this._scratch = value;
            this.OnTextChanged();
        }
    }
    private string _scratch = "";
    private bool _useOOC = false;
    private List<TextChunk> _chunks = new();
    private int _nextChunk = 0;
    private bool _canUndo = false;
    #endregion

    /// <summary>
    /// Editor state is the functional state of the editor.
    /// </summary>
    private int _editorstate = EDITING_TEXT;
    private bool _textchanged = false;
    private int _selected_history = -1;
    private List<PadState> _text_history = new();

    private float _lastWidth = 0;
    private bool _ignoreTextEdit = false;

    private BackgroundWorker? _spellWorker;
    private bool _restartWorker;

    /// <summary>
    /// The text used by the replacement inputtext.
    /// </summary>
    private string _replaceText = "";

    private System.Timers.Timer _spellchecktimer = new(Wordsmith.Configuration.AutoSpellCheckDelay * 1000) { AutoReset = false, Enabled = false };

    private bool _hideOnly = false;

    #region Construction & Initialization
    internal static string CreateWindowName( int id ) => $"{Wordsmith.AppName} - Scratch Pad #{id}";
    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with the next available ID.
    /// </summary>
    public ScratchPadUI() : this(NextAvailableID()) {}

    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with a specific ID
    /// </summary>
    /// <param name="chatType"><see cref="int"/>ID to use</param>
    public ScratchPadUI(int id ) : base( CreateWindowName(id) )
    {
        this.ID = id;
        this.SizeConstraints = new()
        {
            MinimumSize = ImGuiHelpers.ScaledVector2( 375, 200 ),
            MaximumSize = ImGuiHelpers.ScaledVector2( 9999, 9999 )
        };

        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        this.Flags |= ImGuiWindowFlags.MenuBar;

        this._spellchecktimer.Elapsed += ( object? s, System.Timers.ElapsedEventArgs e ) => { this._spellchecktimer.Stop(); this.DoSpellCheck(); };
        this.Header.DataChanged += this.OnDataChanged;

        InitWorker();
    }

    /// <summary>
    /// Initializes the background worker for spell check.
    /// </summary>
    private void InitWorker()
    {
        this._spellWorker = new()
        {
            WorkerSupportsCancellation = true
        };
        this._spellWorker.RunWorkerCompleted += ( o, e ) =>
        {
            // Clear the corrections
            this._corrections.Clear();

            // If the spellcheck task is completed
            if ( e.Error != null )
                PluginLog.LogError( e.Error.Message );

            else if ( e.Cancelled )
            {
                if ( this._restartWorker )
                    ((BackgroundWorker?)o)?.RunWorkerAsync();

                this._restartWorker = false;
            }

            else if ( e.Result is SpellCheckResult result )
                this._corrections.AddRange( result.Words );
        };
    }
    #endregion
    #region Overrides
    /// <summary>
    /// The Draw entrypoint for the <see cref="WindowSystem"/> to draw the window.
    /// </summary>
    public override void Draw()
    {
        DrawMenu();
        DrawAlerts();

        if ( this._editorstate == EDITING_TEXT )
        {
            // Draw the form sections
            DrawHeader();
            DrawChunkDisplay();
            DrawMultilineTextInput();
            DrawWordReplacement();
            DrawEditFooter();

            // Rewrap text based on the width of the form.
            CheckWidth();
        }
        else if ( this._editorstate == VIEWING_HISTORY )
        {
            DrawHistory();
            DrawHistoryFooter();
        }
    }

    /// <summary>
    /// Handles automatically deleting the pad if configured to do so.
    /// </summary>
    public override void OnClose()
    {
        // If automatically deleting closed pads and we're not just hiding it
        if ( Wordsmith.Configuration.DeleteClosedScratchPads && !this._hideOnly )
        {
            // If confirmation required then launch a confirmation dialog
            if ( Wordsmith.Configuration.ConfirmCloseScratchPads )
            {
                WordsmithUI.ShowMessageBox(
                    "Confirm Delete",
                    $"Are you sure you want to delete Scratch Pad {this.ID}? (Cancel will close without deleting.)",
                    MessageBox.ButtonStyle.OkCancel,
                    ( mb ) =>
                    {
                        if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                            WordsmithUI.RemoveWindow( this );
                    });
            }
            else // No confirmation required, just delete.
                WordsmithUI.RemoveWindow( this );
        }

        // Always reset hide only.
        this._hideOnly = false;
    }

    /// <summary>
    /// Runs at each framework update.
    /// </summary>
    public override void Update()
    {
        base.Update();

        if ( Wordsmith.Configuration.ReplaceDoubleSpaces && this.ScratchString.Contains( "  " ) )
        {
            // Only replace if a change is made. This is to prevent accidentally triggering text change events.
            string s = this.ScratchString.FixSpacing();
            if ( s != this.ScratchString )
                this.ScratchString = s;
        }
    }
    #endregion
    #region Top
    /// <summary>
    /// Draws the menu bar at the top of the window.
    /// </summary>
    private void DrawMenu()
    {
        if (ImGui.BeginMenuBar())
        {
            // Start the scratch pad menu
            if (ImGui.BeginMenu($"Scratch Pads##ScratchPadMenu{this.ID}"))
            {
                // New scratchpad button.
                if (ImGui.MenuItem($"New Scratch Pad##NewScratchPad{this.ID}MenuItem"))
                    WordsmithUI.ShowScratchPad();

                // For each of the existing scratch pads, add a button that opens that specific one.
                foreach (ScratchPadUI pad in WordsmithUI.Windows.Where(x => x is ScratchPadUI && x != this).ToArray())
                    if (ImGui.MenuItem($"{pad.WindowName}"))
                        WordsmithUI.ShowScratchPad(pad.ID);

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
                if (ImGui.MenuItem($"Spell Check##ScratchPad{this.ID}SpellCheckMenuItem", this.ScratchString.Length > 0))
                    DoSpellCheck();

                // If there are chunks
                if ( this._chunks.Count > 0 )
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
                if ( this._editorstate == EDITING_TEXT )
                {
                    if ( ImGui.MenuItem( $"View History##ScratchPad{this.ID}MenuItem" ) )
                        this._editorstate = VIEWING_HISTORY;
                }
                else if ( this._editorstate == VIEWING_HISTORY )
                {
                    if ( ImGui.MenuItem( $"Close History##ScratchPad{this.ID}MenuItem" ) )
                        this._editorstate = EDITING_TEXT;
                }

                // End Text menu
                ImGui.EndMenu();
            }

            // Thesaurus menu item
            // For the time being, the thesaurus is disabled.
            if ( ImGui.MenuItem( $"Thesaurus##ScratchPad{this.ID}ThesaurusMenu" ) )
                WordsmithUI.ShowThesaurus();

            // Settings menu item
            if (ImGui.MenuItem($"Settings##ScratchPad{this.ID}SettingsMenu"))
                WordsmithUI.ShowSettings();

            // Help menu item
            if (ImGui.MenuItem($"Help##ScratchPad{this.ID}HelpMenu"))
                WordsmithUI.ShowScratchPadHelp();

            if ( Wordsmith.Configuration.EnableDebug )
                if ( ImGui.MenuItem( $"Debug UI##ScratchPad{this.ID}DebugMenu" ) )
                    WordsmithUI.ShowDebugUI();

            //end Menu Bar
            ImGui.EndMenuBar();
        }
    }

    /// <summary>
    /// Draws the chat type selection and the tell target entry box if set to /tell
    /// </summary>
    private void DrawHeader()
    {
        // Set the column count.
        int default_columns = 3;
        int columns = default_columns;

        // If using Tell or Linkshells, we need 3 columns
        if ( this._header.ChatType == ChatType.Tell || this._header.ChatType == ChatType.Linkshell)
            ++columns;

        if (ImGui.BeginTable($"##ScratchPad{this.ID}HeaderTable", columns))
        {
            // Setup the header lock and chat mode columns.
            ImGui.TableSetupColumn( $"Scratchpad{this.ID}HeaderLockColumn", ImGuiTableColumnFlags.WidthFixed, Global.BUTTON_Y );

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
            int ctype = (int)this._header.ChatType;

            // Display a combo box and reference ctype. Do not show the last option because it is handled
            // in a different way.
            ImGui.Combo($"##ScratchPad{this.ID}ChatTypeCombo", ref ctype, options, options.Length-1);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the chat header.");

            // Convert ctype back to ChatType and set _chatType
            this._header.ChatType = (ChatType)ctype;

            // Chat target bar is only shown if the mode is tell
            if ( this._header.ChatType == ChatType.Tell)
            {
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);

                string str = this._header.TellTarget;
                ImGui.InputTextWithHint($"##TellTargetText{this.ID}", "User Name@World", ref str, 128);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enter the user and world or a placeholder here.");
                this._header.TellTarget = str;
            }

            // Linkshell selection
            else if ( this._header.ChatType == ChatType.Linkshell)
            {
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                bool b = this._header.CrossWorld;
                ImGui.Checkbox("Cross-World", ref b );
                this._header.CrossWorld = b;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                int l = this._header.Linkshell;
                ImGui.Combo($"##ScratchPad{this.ID}LinkshellCombo", ref l, (this._header.CrossWorld ? Wordsmith.Configuration.CrossWorldLinkshellNames : Wordsmith.Configuration.LinkshellNames), 8);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enter a custom targer here such as /cwls1.");
                this._header.Linkshell = l;
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
    private void DrawAlerts()
    {
        // Draw all alerts.
        try
        {
            List<(string Message, int Level)> alerts = new();

            // Display errors
            if ( this._errors.Count > 0 )
                alerts.AddRange( this._errors );

            // Display notices.
            if ( _notices.Count > 0 )
                alerts.AddRange( this._notices );

            // Display spelling error message
            if ( (this._corrections?.Count ?? 0) > 0 )
                alerts.Add( new( $"Found {this._corrections!.Count} spelling errors.", CORRECTIONS_FOUND ) );

            // If there are no alerts, return.
            if ( alerts.Count == 0 )
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

        catch ( Exception e ) { PluginLog.LogError( e.ToString() ); }
    }
    #endregion
    #region Body
    /// <summary>
    /// Draws the text chunk display.
    /// </summary>
    /// <param name="FooterHeight">The size of the footer elements.</param>
    private void DrawChunkDisplay()
    {
        Vector2 vRegionMax = ImGui.GetContentRegionMax();
        Vector2 vCursorPos = ImGui.GetCursorPos();
        float fFooterHeight = GetFooterHeight();
        float size_y = vRegionMax.Y - vCursorPos.Y - fFooterHeight;
        if ( size_y < 1 )
            return;

        if (ImGui.BeginChild($"{Wordsmith.AppName}##ScratchPad{this.ID}ChildFrame", new(-1, size_y) ))
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
                        DrawChunkItem( this._chunks[i], this._header.ChatType );
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
                ImGui.TextWrapped($"{this._header}{(this._useOOC ? "(( " : "")}{this.ScratchString.Unwrap()}{(this._useOOC ? " ))" : "")}");
            }

            if ( this._textchanged )
            {
                ImGui.SetScrollHereY();
                this._textchanged = false;
            }
            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Draws an individual chunk to the window.
    /// </summary>
    /// <param name="chunk">Chunk to be drawn.</param>
    /// <param name="ct">Chat type to display with the chunk.</param>
    private void DrawChunkItem(TextChunk chunk, ChatType ct)
    {
        // Split it into words.

        float width = 0f;

        bool sameLine = false;

        // Draw header
        if ( chunk.Header.Length > 0 )
        {
            if ( ct == ChatType.CrossWorldLinkshell )
                ct = ChatType.Linkshell;

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

            if ( this._corrections?.Count > 0 && this._corrections[0].StartIndex == word.StartIndex+chunk.StartIndex)
                ImGui.TextColored( Wordsmith.Configuration.SpellingErrorHighlightColor, text.Replace( "%", "%%") );
            else
                ImGui.Text(text.Replace( "%", "%%"));

            if ( Wordsmith.Configuration.EnableDebug )
                if ( ImGui.IsItemHovered() )
                    ImGui.SetTooltip( $"StartIndex: {word.StartIndex}, EndIndex: {word.EndIndex}, WordIndex: {word.WordIndex}, WordLength: {word.WordLength}, HyphenTerminated: {word.HyphenTerminated}" );
        }

        // Draw OOC
        if ( this._useOOC )
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
    private unsafe void DrawMultilineTextInput()
    {
        ImGui.SetNextItemWidth(-1);

        // Default size of the text input.
        float size_y = GetDefaultInputHeight();

        float cursorPosY = ImGui.GetCursorPosY();
        float contentRegionY = ImGui.GetContentRegionMax().Y;
        while ( cursorPosY + GetFooterHeight(size_y) > contentRegionY && size_y > 0)
            size_y -= 1;

        // Create a temporary string for the textbox
        string scratch = this.ScratchString;

        // If the user has their option set to SpellCheck or Copy then
        // handle it with an EnterReturnsTrue.
        if (ImGui.InputTextMultiline($"##ScratchPad{this.ID}MultilineTextEntry",
            ref scratch, (uint)Wordsmith.Configuration.ScratchPadMaximumTextLength,
            new(-1, size_y),
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

        // If the string has changed then assign it to this.ScratchString. This will force
        // Wordsmith to update the text chunks.
        if ( scratch != this.ScratchString )
            this.ScratchString = scratch;
    }

    /// <summary>
    /// Draws the word replacement section if there are known spelling errors.
    /// </summary>
    private void DrawWordReplacement()
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
            string wordText = word.GetWordString( this.ScratchString );
            this._replaceText = wordText;

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

                // If the suggestions haven't been generated then get them.
                if ( word.Suggestions is null )
                    word.GenerateSuggestions( wordText );

                else
                {
                    // Don't try to draw any children if there are none.
                    if ( word.Suggestions.Count > 0 )
                    {
                        ImGui.Separator();

                        float childwidth = ImGui.CalcTextSize(this._replaceText).X + (20*ImGuiHelpers.GlobalScale) > ImGui.CalcTextSize(" Add To Dictionary ").X ? ImGui.CalcTextSize(this._replaceText).X + (20*ImGuiHelpers.GlobalScale) : ImGui.CalcTextSize(" Add To Dictionary ").X;
                        if ( ImGui.BeginChild( $"ScratchPad{this.ID}ReplaceContextChild",
                            new(
                                childwidth,
                                (word.Suggestions.Count > 5 ? 105 : word.Suggestions.Count * Global.BUTTON_Y) * ImGuiHelpers.GlobalScale
                                ) ) )
                        {
                            // List the suggestions.
                            foreach ( string suggestion in word.Suggestions )
                            {
                                if ( ImGui.Selectable( $"{suggestion}##ScratchPad{this.ID}Replacement" ) )
                                {
                                    this._replaceText = $"{suggestion}";
                                    OnReplace( 0 );
                                }
                            }
                            ImGui.EndChild();
                        }
                    }
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
    private void DrawEditFooter()
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

            if ( !Wordsmith.Configuration.AutoSpellCheck )
            {
                // If spell check is disabled, make the button dark so it appears as though it is disabled.
                if ( !Lang.Enabled )
                    ImGui.PushStyleVar( ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f );

                // Draw the spell check button.
                ImGui.TableNextColumn();
                if ( this.ScratchString.Length == 0 )
                    ImGui.BeginDisabled();

                if ( ImGui.Button( $"Spell Check##Scratch{this.ID}", ImGuiHelpers.ScaledVector2( -1, Global.BUTTON_Y ) ) )
                    if ( Lang.Enabled ) // If the dictionary is functional then do the spell check.
                        DoSpellCheck();

                if ( this.ScratchString.Length == 0 )
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
            if (ImGui.Button($"Delete Pad##Scratch{this.ID}", ImGuiHelpers.ScaledVector2(-1, Global.BUTTON_Y ) ))
            {
                if ( Wordsmith.Configuration.ConfirmCloseScratchPads )
                {
                    WordsmithUI.ShowMessageBox(
                        "Confirm Delete",
                        $"Are you sure you want to delete this pad?",
                        MessageBox.ButtonStyle.OkCancel,
                        ( mb ) =>
                        {
                            if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                                WordsmithUI.RemoveWindow( this );
                        });
                }
                else
                    WordsmithUI.RemoveWindow(this);
            }
        }
    }

    /// <summary>
    /// Draws the copy button depending on how many chunks are available.
    /// </summary>
    private void DrawCopyButton()
    {
        // If there is more than 1 chunk.
        if ( this._chunks.Count > 1)
        {
            // Push the icon font for the character we need then draw the previous chunk button.
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)0xF100}##{this.ID}ChunkBackButton", ImGuiHelpers.ScaledVector2( Global.BUTTON_Y, Global.BUTTON_Y ) ))
            {
                --this._nextChunk;
                if ( this._nextChunk < 0)
                    this._nextChunk = this._chunks.Count - 1;
            }
            // Reset the font.
            ImGui.PushFont(UiBuilder.DefaultFont);

            // Draw the copy button with no spacing.
            ImGui.SameLine(0, 0);
            if (ImGui.Button($"Copy{(this._chunks.Count > 1 ? $" ({this._nextChunk + 1}/{this._chunks.Count})" : "")}##ScratchPad{this.ID}", new(ImGui.GetColumnWidth() - Global.BUTTON_Y_SCALED, Global.BUTTON_Y_SCALED ) ))
                DoCopyToClipboard();

            // Push the font and draw the next chunk button with no spacing.
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.SameLine(0, 0);
            if (ImGui.Button($"{(char)0xF101}##{this.ID}ChunkBackButton", ImGuiHelpers.ScaledVector2( Global.BUTTON_Y, Global.BUTTON_Y ) ))
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
            if (ImGui.Button($"Copy{(this._chunks.Count > 1 ? $" ({this._nextChunk + 1}/{this._chunks.Count})" : "")}##ScratchPad{this.ID}", new(-1, Global.BUTTON_Y_SCALED ) ))
                DoCopyToClipboard();
        }
    }

    /// <summary>
    /// Draws the copy button depending on how many chunks are available.
    /// </summary>
    private void DrawClearButton()
    {
        // If undo is enabled and there is history.
        if ( this._canUndo && this._text_history.Count > 0 )
        {
            if (ImGui.Button($"Clear##ScratchPad{this.ID}", new(ImGui.GetColumnWidth() - Global.BUTTON_Y_SCALED, Global.BUTTON_Y_SCALED ) ))
                DoClearText();

            // Push the font and draw the next chunk button with no spacing.
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.SameLine(0, 0);
            if (ImGui.Button($"{(char)0xF0E2}##{this.ID}UndoClearButton", ImGuiHelpers.ScaledVector2( Global.BUTTON_Y, Global.BUTTON_Y ) ))
                UndoClearText();

            // Reset the font.
            ImGui.PushFont(UiBuilder.DefaultFont);
        }
        else // If there is only one chunk simply draw a normal button.
        {
            if (ImGui.Button($"Clear##ScratchPad{this.ID}", new(-1, Global.BUTTON_Y_SCALED ) ))
                DoClearText();
        }
    }
    #endregion
    #region History
    /// <summary>
    /// Draws the user's message history.
    /// </summary>
    private void DrawHistory()
    {
        Vector2 contentRegion = ImGui.GetContentRegionMax();
        contentRegion.Y -= ImGui.GetCursorPosY() + GetFooterHeight();
        if ( ImGui.BeginChild( $"HistoryChild", contentRegion ) )
        {
            for ( int i = 0; i < this._text_history.Count; ++i )
                DrawHistoryItem( this._text_history[i], i );
            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Draws individual items in the history list.
    ///
    /// </summary>
    /// <param name="s">The text for the history.</param>
    private void DrawHistoryItem(PadState p, int idx)
    {
        try
        {
            // Create a group.
            ImGui.BeginGroup();

            // Get the text chunks.
            List<TextChunk>? result = ChatHelper.FFXIVify(p.Header!, p.ScratchText, p.UseOOC);
            if ( result == null )
                return;

            List<TextChunk> tlist = result;

            // Display the chunks.
            for ( int i = 0; i < tlist.Count; ++i )
            {
                if ( i > 0 )
                    ImGui.Spacing();

                DrawChunkItem( tlist[i], p.Header!.ChatType );
            }

            // End the group.
            ImGui.EndGroup();

            // Create a border around the group
            Vector2 vMin = ImGui.GetItemRectMin();

            Rect r;
            r.Left = (int)vMin.X;
            r.Top = (int)vMin.Y;
            r.Right = (int)(vMin.X + ImGui.GetWindowWidth() - ImGui.GetStyle().FramePadding.X * 2);
            r.Bottom = (int)(ImGui.GetItemRectMax().Y - ImGui.GetItemRectMin().Y);
            ImGui.GetWindowDrawList().AddRect( r.Position, r.Size + r.Position, ImGui.GetColorU32( ImGuiCol.Border ) );

            if ( ImGui.BeginPopup( $"ScratchPad{this.ID}History{idx}Popup" ) )
            {
                if ( ImGui.MenuItem( $"Reload Pad State##ScratchPad{this.ID}HistoryItem{idx}Reload" ) )
                {
                    int index = this._selected_history;

                    // If there is currently written text, require confirmation.
                    if ( this.ScratchString != "" )
                        WordsmithUI.ShowMessageBox(
                            "Load History?", "Loading the history state will overwrite\nany currently written text and chat headers.",
                            MessageBox.ButtonStyle.OkCancel,
                            new Action<MessageBox>((m) => {
                                if ( m.Result == MessageBox.DialogResult.Ok )
                                    LoadState( this._text_history[index] );
                            } )
                        );

                    // If there is no currently written text just load the state.
                    else
                        LoadState( this._text_history[index] );
                    
                    // Close the popup
                    ImGui.CloseCurrentPopup();
                }
                else if ( ImGui.MenuItem( $"Copy Text To Clipboard##ScratchPad{this.ID}HistoryItem{idx}Copy" ) )
                {
                    // Get the selected pad state
                    PadState selected = this._text_history[this._selected_history];

                    // Get each chunk
                    List<TextChunk>? results = ChatHelper.FFXIVify(selected.Header!, selected.ScratchText, selected.UseOOC);
                    if ( results != null )
                    {
                        List<TextChunk> chunks = results;

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
                    }
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
    /// Draws the footer for the History view.
    /// </summary>
    private void DrawHistoryFooter()
    {
        if ( ImGui.Button( $"Close##{ID}closehistorybutton", new( ImGui.GetWindowContentRegionMax().X - ImGui.GetStyle().FramePadding.X * 2, Global.BUTTON_Y_SCALED ) ) )
            this._editorstate = EDITING_TEXT;
    }

    /// <summary>
    /// Creates a new history entry while moving duplicates and overflow.
    /// </summary>
    /// <param name="p">The <see cref="PadState"/> to be added to history.</param>
    private void AppendHistory( PadState p )
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
            int count = this._text_history.Count - Wordsmith.Configuration.ScratchPadHistoryLimit;
            for ( int i = 0; i < count; i++ )
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
    private void DoCopyToClipboard()
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
    private void DoClearText()
    {
        try
        {
            // Ignore empty strings.
            if ( this.ScratchString.Length == 0 )
                return;

            // Create a history state.
            AppendHistory( new( this ) );

            // Clear any corrections.
            this._corrections = new();

            // Clear scratch.
            this.ScratchString = "";

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
    private void UndoClearText()
    {
        try
        {
            // If undo is locked or there are no previous states then abort
            if ( !this._canUndo || this._text_history.Count == 0 )
                return;

            // Get the last pad state
            PadState p = this._text_history.Last();

            // Recover the text
            this.ScratchString = p.ScratchText;

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
    private void DoSpellCheck()
    {
        this._restartWorker = true;
        this._spellWorker?.CancelAsync();

        PluginLog.LogVerbose( "Running spell check." );
        this._spellWorker!.DoWork += ( o, e ) => SpellChecker.CheckString( this.ScratchString, (BackgroundWorker?)o, e );
        this._spellWorker!.RunWorkerAsync();
    }
    #endregion
    #region Callbacks
    /// <summary>
    /// Adds the word to the dictionary and removes any subsequent correction requestions with
    /// the same word in it.
    /// </summary>
    /// <param name="index"><see cref="int"/> index of the correction in correction list.</param>
    private void OnAddToDictionary(int index)
    {
        try
        {
            Word word = this._corrections[index];
            // Get the word
            string newWord = word.GetWordString(this.ScratchString);

            // Add the cleaned word to the dictionary.
            Lang.AddDictionaryEntry( newWord );

            // Remove the cleaned word from the dictionary
            this._corrections.RemoveAt( index );

            // Get rid of any spelling corrections with the same word
            foreach ( Word w in this._corrections )
                if ( this.ScratchString.Substring( w.WordIndex, w.WordLength ) == newWord )
                    this._corrections.Remove( w );
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
    /// Handles DataChanged event for the HeaderData
    /// </summary>
    /// <param name="sender">The object sending the data change event</param>
    /// <param name="e">Unused</param>
    internal void OnDataChanged(HeaderData sender)
    {
        if ( sender == this.Header )
            foreach ( TextChunk tc in this._chunks )
                tc.Header = this.Header.ToString();
    }

    /// <summary>
    /// Replaces spelling errors with the given text or ignores an error if _replaceText is blank
    /// </summary>
    /// <param name="index"><see cref="int"/> index of the correction in correction list.</param>
    private void OnReplace( int index )
    {
        try
        {
            // If the text box is not empty when the user hits enter then
            // update the text.
            if ( this._replaceText.Length > 0 && index < this._corrections.Count )
            {
                if ( Helpers.Console.ProcessCommand( this, _replaceText ) )
                    return;

                // Get the first object
                Word word = this._corrections[index];

                // Get the string builder on the unwrapped scratch string.
                StringBuilder sb = new(this.ScratchString.Unwrap());

                // Remove the original word.
                sb.Remove( word.WordIndex, word.WordLength );

                // Insert the new text.
                sb.Insert( word.WordIndex, this._replaceText.Trim() );

                string newScratch = sb.ToString();

                // Remove the word from the list.
                this._corrections.Remove( word );

                // Adjust the index of all following words.
                if ( this._replaceText.Length != word.WordLength )
                {
                    foreach ( Word w in this._corrections )
                        w.Offset( this._replaceText.Length - word.WordLength );
                }

                // Clear out replacement text.
                this._replaceText = "";

                int ignore = -1;
                newScratch = CheckForHeader( newScratch, ref ignore );

                // Rewrap the text string.
                // Rewrap scratch
                ImGuiStylePtr style = ImGui.GetStyle();
                float width = this._lastWidth - ( style.FramePadding.X + style.ScrollbarSize * ImGuiHelpers.GlobalScale);
                newScratch = newScratch.Wrap( width );

                this.ScratchString = newScratch;
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
                        PluginLog.LogVerbose( $"Moved cursor to the right." );
                    }
                }
                if ( data->CursorPos > 0 )
                {
                    while ( data->CursorPos > 0 && data->Buf[data->CursorPos - 1] == '\r' )
                    {
                        data->CursorPos--;
                        PluginLog.LogVerbose( "Moved cursor to the left." );
                    }
                }
            }
            else
            {
                this._corrections?.Clear();
                OnTextEdit( data );
                OnTextChanged();
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
    /// Updates text chunks on text changed.
    /// </summary>
    internal void OnTextChanged()
    {
        // If text has been entered, disable undo.
        if ( this.ScratchString.Length > 0 )
            this._canUndo = false;

        // Rebuild chunks and reset chunk position counter.
        this._chunks = ChatHelper.FFXIVify( this._header, this.ScratchString.Unwrap(), this._useOOC ) ?? new();
        this._nextChunk = 0;

        if ( Wordsmith.Configuration.AutoSpellCheck )
        {
            this._spellchecktimer.Stop();
            if ( this.ScratchString.Length > 0 )
            {
                this._spellchecktimer.Interval = Wordsmith.Configuration.AutoSpellCheckDelay * 1000;
                this._spellchecktimer.Start();
            }
        }
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

            // There is no need to operate on an empty string.
            if ( txt.Length == 0 )
                return 0;

            int pos = data->CursorPos;

            // Check for header input.
            if ( _header_parse )
                txt = CheckForHeader( txt, ref pos );

            // Wrap the string if there is enough there.
            if ( txt.Length > 0 )
            {
                // Rewrap scratch
                ImGuiStylePtr style = ImGui.GetStyle();
                float width = this._lastWidth - ( style.FramePadding.X + style.ScrollbarSize * ImGuiHelpers.GlobalScale);
                txt = txt.Wrap( width, ref pos );
            }

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
    #region General Methods
    /// <summary>
    /// Checks to see if the width of the window has changed and rewraps text if it has.
    /// </summary>
    private void CheckWidth()
    {
        // We do this here in case the window is being resized and we want
        // to rewrap the text in the textbox.
        if ( Math.Abs(ImGui.GetWindowWidth() - this._lastWidth) > 0.1 )
        {
            // Don't flag to ignore text edit if the window was just opened.
            if ( this._lastWidth > 0.1 )
                this._ignoreTextEdit = true;

            // Rewrap scratch
            ImGuiStylePtr style = ImGui.GetStyle();
            float width = this._lastWidth - ( style.FramePadding.X + style.ScrollbarSize * ImGuiHelpers.GlobalScale);

            // Update the string only if it is actually affected by the wrapping. This will prevent
            // spell checking rewraps that don't change anything.
            string s = this.ScratchString.Wrap( width );
            if ( s != this.ScratchString )
                this.ScratchString = s;

            // Update the last known width.
            this._lastWidth = ImGui.GetWindowWidth();
        }
    }

    /// <summary>
    /// Check for a chat header at the start of a string.
    /// </summary>
    /// <param name="text">Text to parse from</param>
    /// <param name="cursorPos">Cursor position</param>
    /// <returns>The text string with header removed.</returns>
    private string CheckForHeader(string text, ref int cursorPos)
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

            //If a chat header was found
            int len = headerData.AliasLength > 0 ? headerData.AliasLength : headerData.Length;
            if ( headerData.ChatType != ChatType.None && cursorPos >= len )
            {
                this._header = headerData;

                text = text.Remove( 0, len + 1);
                cursorPos -= len + 1;
            }
        }
        return text;
    }

    private float GetDefaultInputHeight() => Wordsmith.Configuration.ScratchPadInputLineHeight * ImGui.CalcTextSize( "A" ).Y;

    /// <summary>
    /// Gets the height of the footer.
    /// </summary>
    /// <param name="IncludeTextbox">If false, the height of the textbox is not added to the result.</param>
    /// <returns>Returns the height of footer elements as a <see langword="float"/></returns>
    private float GetFooterHeight( float inputSize = -1 )
    {
        if ( this._editorstate == EDITING_TEXT )
        {
            // Text input size can either be given or calculated.
            float result = inputSize > 0 ? inputSize : GetDefaultInputHeight();
            int paddingCount = 2;

            // Delete pad button
            if ( !Wordsmith.Configuration.DeleteClosedScratchPads )
            {
                result += Global.BUTTON_Y_SCALED;
                paddingCount++;
            }

            // Replace Text section
            if ( this._corrections.Count > 0 )
            {
                result += Global.BUTTON_Y_SCALED;
                paddingCount++;
            }

            // Button row
            result += Global.BUTTON_Y_SCALED;
            paddingCount++;

            // Padding
            result += ImGui.GetStyle().FramePadding.Y * paddingCount;

            return result;
        }
        else if ( this._editorstate == VIEWING_HISTORY )
            return Global.BUTTON_Y_SCALED + ImGui.GetStyle().FramePadding.Y;

        return 0;
    }

    /// <summary>
    /// Hides the window without deleting. Ignores automatic deletion.
    /// </summary>
    internal void Hide()
    {
        this._hideOnly = true;
        this.IsOpen = false;
    }

    /// <summary>
    /// Replaces the current pad state with the given state.
    /// </summary>
    /// <param name="state"></param>
    private void LoadState(PadState state)
    {
        // Get the current pad state
        PadState snapshot = new( this );

        // Revert to given padstate
        this._header = state.Header!;
        this.ScratchString = state.ScratchText;

        // Exit history.
        this._editorstate = EDITING_TEXT;

        // Unselect the history time.
        this._selected_history = -1;
    }
    #endregion
}
