using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Wordsmith.Enums;
using Wordsmith.Helpers;

namespace Wordsmith.Gui;

internal sealed class ScratchPadUI : Window
{
    #region Constants
    internal const int CORRECTIONS_FOUND = -1;
    internal const int CHECKING_SPELLING = 1;
    internal const int CORRECTIONS_NOT_FOUND = 2;
    internal const int EDITING_TEXT = 0;
    internal const int VIEW_MODE_PAD = 0;
    internal const int VIEW_MODE_HISTORY = 1;
    internal const int VIEW_MODE_STATS = 2;
    #endregion

    #region Identity
    /// <summary>
    /// Contains all of the variables related to ID
    /// </summary>
    private static int _nextID = 0;
    private static int NextAvailableID()
    {
        int id;
        string s;
        do
        {
            id = ++_nextID;
            s = CreateWindowName( id );
        } while ( WordsmithUI.Contains( s ) );
        return id;
    }
    public int ID { get; set; }

    /// <summary>
    /// A custom text title to name the scratch pad.
    /// </summary>
    public string Title { get; private set; } = "";
    #endregion

    #region Chat Header
    private bool _header_parse = Wordsmith.Configuration.ParseHeaderInput;
    private HeaderData _header = new(true);
    public HeaderData Header => this._header;
    #endregion

    #region Chat Text
    /// <summary>
    /// Returns a trimmed, single-line version of scratch.
    /// </summary>
    internal string ScratchString
    {
        get => this._scratch;
        set
        {
            this._scratch = value;
            OnTextChanged();
        }
    }
    private string _scratch = "";
    private bool _useOOC = Wordsmith.Configuration.OocByDefault;
    internal bool UseOOC
    {
        get => this._useOOC;
        set
        {
            this._invalidateChunks |= this._useOOC != value;
            this._useOOC = value;
        }
    }
    private List<TextChunk> _chunks = new();
    private int _nextChunk = 0;
    private bool _canUndo = false;
    private bool _textchanged = false;
    private bool _ignoreTextEdit = false;
    private bool _invalidateChunks = false;

    /// <summary>
    /// The text used by the replacement inputtext.
    /// </summary>
    private string _replaceText = "";
    #endregion

    #region History
    private List<PadState> _text_history = new();
    private StatisticsTracker _statisticsTracker = new();

    private string _stats_order_by = "usesdescending";
    #endregion

    #region Spell Check
    private List<Word> _corrections = new();
    private float _rest_time = Wordsmith.Configuration.AutoSpellCheckDelay*1.0f;
    private bool _do_spell_check = false;
    #endregion

    #region Window State
    private bool _hideOnly = false;
    private float _lastWidth = 0;
    private float _lastScale = ImGuiHelpers.GlobalScale;
    #endregion

    #region Construction & Destruction
    internal static string CreateWindowName( int id ) => $"{Wordsmith.APPNAME} - Scratch Pad #{id}";
    internal static string CreateWindowName( string str ) => $"{Wordsmith.APPNAME} - Scratch Pad: {str.Replace( "%", "%%" )}";

    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with the next available ID.
    /// </summary>
    public ScratchPadUI() : this( NextAvailableID() ) { }

    /// <summary>
    /// Initializes a new <see cref="ScratchPadUI"/> object with a specific ID
    /// </summary>
    /// <param name="chatType"><see cref="int"/>ID to use</param>
    public ScratchPadUI( int id ) : base( CreateWindowName( id ) )
    {
        this.ID = id;
        this.SizeConstraints = new()
        {
            MinimumSize = new( 375, 200 ),
            MaximumSize = new( float.MaxValue, float.MaxValue ) // Do not scale
        };

        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        this.Flags |= ImGuiWindowFlags.MenuBar;

        //this._spellchecktimer.Elapsed += ( object? s, System.Timers.ElapsedEventArgs e ) => { this._spellchecktimer.Stop(); this.DoSpellCheck(); };
        this.Header.DataChanged += OnDataChanged;

        //InitWorker();
    }

    public ScratchPadUI( string name ) : this( NextAvailableID() )
    {
        this.WindowName = CreateWindowName( name );
        this.Title = name;
    }

    private void DoClose()
    {
        // To prevent the window from being created repeatedly
        // ensure that the dialog is not already open.
        WordsmithUI.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete Scratch Pad {this.ID}?\r\n(Cancel will close without deleting.)",
            MessageBox.ButtonStyle.OkCancel,
            ( mb ) =>
            {
                if( ( mb.Result & MessageBox.DialogResult.Ok ) == MessageBox.DialogResult.Ok )
                    WordsmithUI.RemoveWindow( this );
            } );
    }
    #endregion

    private int _view_mode = VIEW_MODE_PAD;

    #region Overrides
    /// <summary>
    /// The Draw entry point for the <see cref="WindowSystem"/> to draw the window.
    /// </summary>
    public override void Draw()
    {
        // Draw the menu bar
        DrawMenu();

        // If there are known spelling errors then show the alert.
        if ( this._corrections?.Count > 0 )
            ImGui.TextColored( new System.Numerics.Vector4( 1, 0, 0, 1 ), $"Found {this._corrections!.Count} spelling errors." );

        if ( this._view_mode == VIEW_MODE_PAD )
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
        else if ( this._view_mode == VIEW_MODE_HISTORY )
        {
            DrawHistory();
            DrawHistoryFooter();
        }
        else if ( this._view_mode == VIEW_MODE_STATS )
        {
            DrawStats();
            DrawStatsFooter();
        }

        // Update the last scale used. This will be used to recalculate
        // the size of text if necessary.
        this._lastScale = ImGuiHelpers.GlobalScale;
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
            if ( Wordsmith.Configuration.ConfirmDeleteClosePads)
                DoClose();

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
        if ( Wordsmith.Configuration.ReplaceDoubleSpaces && this.ScratchString.Contains( "  " ) )
        {
            // Only replace if a change is made. This is to prevent accidentally triggering text change events.
            string s = this.ScratchString.FixSpacing();
            if ( s != this.ScratchString )
                this.ScratchString = s;
        }

        // If the configuration was recently saved
        if ( Wordsmith.Configuration.RecentlySaved )
        {
            // Rebuild chunks
            this._invalidateChunks |= true;

            // Recheck spelling
            DoSpellCheck();
            this._do_spell_check = false;
        }

        // If the text chunks have been invalidated then update them
        if ( this._invalidateChunks )
        {
            // Rebuild the chunks
            FFXIVify();

            // Reset the chunk invalidation.
            this._invalidateChunks = false;
        }

        // If the configuration wasn't saved recently then check if spell check is required
        // iSpellcheckMode 2 disables the need to edit the text.
        else if ( this._do_spell_check || Helpers.Console.iSpellcheckMode == 2 )
        {
            this._rest_time -= WordsmithUI.Clock.Delta;
            // iSpellcheckModes 1 and 2 ignore timer
            if ( this._rest_time < 0 || Helpers.Console.iSpellcheckMode > 0 )
                DoSpellCheck();
        }
    }
    #endregion

    #region Top
    /// <summary>
    /// Draws the menu bar at the top of the window.
    /// </summary>
    private void DrawMenu()
    {
        if ( ImGui.BeginMenuBar() )
        {
            try
            {
                // Start the scratch pad menu
                if ( ImGui.BeginMenu( $"Scratch Pads##ScratchPadMenu{this.ID}" ) )
                {
                    // New scratchpad button.
                    if ( ImGui.MenuItem( $"New Scratch Pad##NewScratchPad{this.ID}MenuItem" ) )
                        WordsmithUI.ShowScratchPad();

                    // For each of the existing scratch pads, add a button that opens that specific one.
                    List<ScratchPadUI> padList = new(WordsmithUI.Windows.Where( x => x is ScratchPadUI && x != this).Cast<ScratchPadUI>());
                    foreach ( ScratchPadUI pad in padList )
                        if ( ImGui.MenuItem( $"{pad.WindowName}" ) )
                            WordsmithUI.ShowScratchPad( pad.ID );

                    // End the scratch pad menu
                    ImGui.EndMenu();
                }

                // Text menu
                if ( ImGui.BeginMenu( $"Text##ScratchPad{this.ID}TextMenu" ) )
                {
                    #region Clear
                    if ( !this._canUndo || this._text_history.Count == 0 )
                    {
                        // Show the clear text option.
                        if ( ImGui.MenuItem( $"Clear##ScratchPad{this.ID}TextClearMenuItem" ) )
                            DoClearText();
                    }
                    else
                    {
                        // Show the undo clear text option
                        if ( ImGui.MenuItem( $"Undo Clear##ScratchPad{this.ID}TextUndoClearMenuItem" ) )
                            UndoClearText();
                    }
                    #endregion

                    // Spell Check
                    if ( ImGui.MenuItem( $"Spell Check##ScratchPad{this.ID}SpellCheckMenuItem", this.ScratchString.Length > 0 ) )
                        DoSpellCheck();

                    #region Chunks
                    bool bNoChunks = this._chunks.Count == 0;
                    if ( bNoChunks )
                        ImGui.BeginDisabled();

                    // Create a chunk menu and create a copy button for each chunk.
                    if ( ImGui.BeginMenu( $"Chunks##ScratchPad{this.ID}ChunksMenu" ) )
                    {
                        for ( int i = 0; i < this._chunks.Count; ++i )
                            if ( ImGui.MenuItem( $"Copy Chunk {i + 1}##ScratchPad{this.ID}ChunkMenuItem{i}" ) )
                                ImGui.SetClipboardText( CreateCompleteTextChunk( this._chunks[i], this._useOOC, i, this._chunks.Count ) );

                        // End chunk menu
                        ImGui.EndMenu();
                    }

                    if ( bNoChunks )
                        ImGui.EndDisabled();
                    #endregion

                    #region History
                    // View/Close history
                    if ( this._view_mode != VIEW_MODE_HISTORY )
                    {
                        if ( ImGui.MenuItem( $"View History##ScratchPad{this.ID}MenuItem" ) )
                            this._view_mode = VIEW_MODE_HISTORY;
                    }
                    else
                    {
                        if ( ImGui.MenuItem( $"Close History##ScratchPad{this.ID}MenuItem" ) )
                            this._view_mode = VIEW_MODE_PAD;
                    }
                    #endregion
                    #region Statistics
                    // TODO Configuration track word statistics
                    if ( this._view_mode != VIEW_MODE_STATS )
                    {
                        if ( ImGui.MenuItem( $"Word Statistics##ScratchPad{this.ID}MenuItem" ) )
                            this._view_mode = VIEW_MODE_STATS;
                    }
                    else
                    {
                        if ( ImGui.MenuItem( $"Close Statistics##ScratchPad{this.ID}MenuItem" ) )
                            this._view_mode = VIEW_MODE_PAD;
                    }
                    #endregion

                    // End Text menu
                    ImGui.EndMenu();
                }

                // Thesaurus menu item
                // For the time being, the thesaurus is disabled.
                if ( ImGui.MenuItem( $"Thesaurus##ScratchPad{this.ID}ThesaurusMenu" ) )
                    WordsmithUI.ShowThesaurus();

                // Settings menu item
                if ( ImGui.MenuItem( $"Settings##ScratchPad{this.ID}SettingsMenu" ) )
                    WordsmithUI.ShowSettings();

                // Help menu item
                if ( ImGui.MenuItem( $"Help##ScratchPad{this.ID}HelpMenu" ) )
                    WordsmithUI.ShowScratchPadHelp();

#if DEBUG
                if ( ImGui.MenuItem( $"Debug UI##ScratchPad{this.ID}DebugMenu" ) )
                    WordsmithUI.ShowDebugUI();
#endif
            }
            catch ( Exception e ) { DumpError( e ); }
        }
        ImGui.EndMenuBar();
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
        if ( this._header.ChatType == ChatType.Tell || this._header.ChatType == ChatType.Linkshell )
            ++columns;

        if ( ImGui.BeginTable( $"##ScratchPad{this.ID}HeaderTable", columns ) )
        {
            // Setup the header lock and chat mode columns.
            ImGui.TableSetupColumn( $"Scratchpad{this.ID}HeaderLockColumn", ImGuiTableColumnFlags.WidthFixed, Wordsmith.BUTTON_Y.Scale() );

            // If there is an extra column, insert it here.
            if ( columns > default_columns )
                ImGui.TableSetupColumn( $"Scratchpad{this.ID}ExtraColumn", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale );

            ImGui.TableSetupColumn( $"ScratchPad{this.ID}MiddleColumn", ImGuiTableColumnFlags.WidthStretch, 2 );
            ImGui.TableSetupColumn( $"Scratchpad{this.ID}OOCColumn", ImGuiTableColumnFlags.WidthFixed, 75 * ImGuiHelpers.GlobalScale );

            // Header parse lock
            ImGui.TableNextColumn();
            if ( Dalamud.Interface.Components.ImGuiComponents.IconButton( this._header_parse ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Lock ) )
                this._header_parse = !this._header_parse;
            ImGuiExt.SetHoveredTooltip( $"{(this._header_parse ? "Locks" : "Unlocks")} header parsing on this pad." );

            #region Header Selection
            // Get the header options.
            string[] options = Enum.GetNames(typeof(ChatType));
            int ctype = (int)this._header.ChatType;

            // Display a combo box and reference ctype. Do not show the last option because it is handled
            // in a different way.
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth( -1 );
            ImGui.Combo( $"##ScratchPad{this.ID}ChatTypeCombo", ref ctype, options, options.Length - 1 );
            ImGuiExt.SetHoveredTooltip( "Select the chat header." );

            this._invalidateChunks |= (ChatType)ctype != this._header.ChatType;

            // Convert ctype back to ChatType and set _chatType
            this._header.ChatType = (ChatType)ctype;

            // Chat target bar is only shown if the mode is tell
            if ( this._header.ChatType == ChatType.Tell )
            {
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth( -1 );

                string str = this._header.TellTarget;
                ImGui.InputTextWithHint( $"##TellTargetText{this.ID}", "User Name@World", ref str, 128 );
                ImGuiExt.SetHoveredTooltip( "Enter the user and world or a placeholder here." );
                this._header.TellTarget = str;
            }

            // Linkshell selection
            else if ( this._header.ChatType == ChatType.Linkshell )
            {
                ImGui.TableNextColumn();
                bool b = this._header.CrossWorld;
                ImGui.Checkbox( "Cross-World", ref b );
                this._header.CrossWorld = b;

                ImGui.SameLine();
                ImGui.SetNextItemWidth( -1 );
                int l = this._header.Linkshell;
                ImGui.Combo( $"##ScratchPad{this.ID}LinkshellCombo", ref l, (this._header.CrossWorld ? Wordsmith.Configuration.CrossWorldLinkshellNames : Wordsmith.Configuration.LinkshellNames), 8 );
                ImGuiExt.SetHoveredTooltip( "Enter a custom target here such as /cwls1." );
                this._header.Linkshell = l;
            }
            #endregion

            ImGui.TableNextColumn();

            // Use a temporary variable here so that the UseOOC property can be triggered properly.
            bool ooc = this.UseOOC;
            ImGui.Checkbox( "((OOC))", ref ooc );
            this.UseOOC = ooc;

            ImGuiExt.SetHoveredTooltip( "Enables or disables OOC markers." );
            ImGui.EndTable();
        }
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

        if ( ImGui.BeginChild( $"{Wordsmith.APPNAME}##ScratchPad{this.ID}ChildFrame", new( -1, size_y ) ) )
        {
            // If the chunk data is null we abort the draw call and end the child.
            if ( this._chunks is null )
            {
                ImGui.EndChild();
                return;
            }

            // We still perform this check on the property for ShowTextInChunks in case the user is using single line input.
            // If ShowTextInChunks is enabled, we show the text in its chunked state.
            if ( Wordsmith.Configuration.ShowTextInChunks )
            {
                float fSpaceWidth = ImGui.CalcTextSize( " " ).X;
                for ( int i = 0; i < this._chunks.Count; ++i )
                {
                    //// If not the first chunk, add a spacing.
                    if ( i > 0 )
                        ImGui.Spacing();

                    // Put a separator at the top of the chunk.
                    ImGui.Separator();

                    if ( Wordsmith.Configuration.EnableTextHighlighting )
                    {
                        List<ChunkMarker> markers = new();
                        foreach ( ChunkMarker cm in Wordsmith.Configuration.ChunkMarkers )
                            if ( cm.AppliesTo( i, this._chunks.Count ) && cm.Visible( this._useOOC, this._chunks.Count ) )
                                markers.Add( cm );

                        DrawChunkItem( this._chunks[i], this._header.ChatType, this._useOOC, i, this._chunks.Count, fSpaceWidth, markers, this._corrections );
                    }
                    else
                    {
                        // Set width and display the chunk.
                        ImGui.SetNextItemWidth( -1 );
                        ImGui.TextWrapped( CreateCompleteTextChunk( this._chunks[i], this._useOOC, i, this._chunks.Count ) );
                    }
                }
            }
            // If it's disabled and the user has enabled UseOldSingleLineInput then we still need to draw a display for them.
            else
            {
                ImGui.SetNextItemWidth( -1 );
                ImGui.TextWrapped( $"{this._header}{(this._useOOC ? "(( " : "")}{this.ScratchString.Unwrap()}{(this._useOOC ? " ))" : "")}" );
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
    private static void DrawChunkItem( TextChunk chunk, ChatType ct, bool ooc, int index, int chunkCount, float spaceWidth, List<ChunkMarker> lMarkers, List<Word>? corrections )
    {
        // Don't attempt to draw null chunks.
        if ( chunk is null )
            return;

        float width = 0f;
        bool sameLine = false;

        // Draw header
        if ( chunk.Header.Length > 0 )
        {
            if ( ct == ChatType.CrossWorldLinkshell )
                ct = ChatType.Linkshell;

            ImGui.TextColored( Wordsmith.Configuration.HeaderColors[(int)ct], chunk.Header.Replace( "%", "%%" ) );
            width += ImGui.CalcTextSize( chunk.Header ).X;
            sameLine = true;
        }


        float regionWidth = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X;
        void DrawMarkers( List<ChunkMarker> lMarkerSublist )
        {
            foreach ( ChunkMarker cm in lMarkerSublist )
            {
                string cmText = cm.Text.ReplacePlaceholders( index+1, chunkCount );
                Vector2 vTextSize = ImGui.CalcTextSize( cmText );
                width += vTextSize.X;
                if ( width < regionWidth )
                    ImGui.SameLine( 0, spaceWidth );
                else
                    width = vTextSize.X;
                ImGui.Text( cmText );
                sameLine = true;
            }
        }

        DrawMarkers( lMarkers.Where( x => x.Position == MarkerPosition.BeforeOOC ).ToList() );

        // Draw OOC
        if ( ooc )
        {
            ImGui.SameLine( 0, spaceWidth );
            ImGui.Text( Wordsmith.Configuration.OocOpeningTag.Replace( "%", "%%" ) );
            width += ImGui.CalcTextSize( Wordsmith.Configuration.OocOpeningTag ).X;
            sameLine = true;
        }
        DrawMarkers( lMarkers.Where( x => x.Position == MarkerPosition.BeforeBody ).ToList() );

        // Draw body
        string chunktext = chunk.Text.Trim().Unwrap();
        List<Word> words = chunk.Words;

        for ( int i = 0; i < words.Count; ++i )
        {
            // Get the first word
            Word word = words[i];

            // Get the word string
            string text = word.GetString(chunktext);

            float objWidth = ImGui.CalcTextSize(text).X;
            width += objWidth + spaceWidth;
            if ( (i > 0 || sameLine) && width < regionWidth )
                ImGui.SameLine( 0, spaceWidth );

            else
                width = objWidth;

            if ( corrections?.Count > 0 && corrections[0].StartIndex == word.StartIndex + chunk.StartIndex )
                ImGui.TextColored( Wordsmith.Configuration.SpellingErrorHighlightColor, text.Replace( "%", "%%" ) );

            else
                ImGui.Text( text.Replace( "%", "%%" ) );

#if DEBUG
            ImGuiExt.SetHoveredTooltip( $"StartIndex: {word.StartIndex}, EndIndex: {word.EndIndex}, WordIndex: {word.WordIndex}, WordLength: {word.WordLength}, HyphenTerminated: {word.HyphenTerminated}" );
#endif
        }
        DrawMarkers( lMarkers.Where( x => x.Position == MarkerPosition.AfterBody ).ToList() );

        // Draw OOC
        if ( ooc )
        {
            // Calculate new width.
            float oocwidth = ImGui.CalcTextSize( Wordsmith.Configuration.OocClosingTag ).X;
            width += oocwidth;

            // If width is within bounds then draw same line.
            if ( width < regionWidth )
                ImGui.SameLine( 0, 2 * ImGuiHelpers.GlobalScale );

            // If out of bounds reset width to new width.
            else
                width = oocwidth;

            // Draw text.
            ImGui.Text( Wordsmith.Configuration.OocClosingTag.Replace( "%", "%%" ) );
        }

        // Draw after OOC markers
        DrawMarkers( lMarkers.Where( x => x.Position == MarkerPosition.AfterOOC ).ToList() );

        // If we are to draw the continuation marker then use the same DrawMarkers system 
        if ( chunkCount > 1 && (index + 1 < chunkCount || Wordsmith.Configuration.ContinuationMarkerOnLast) )
            DrawMarkers( new() { new( Wordsmith.Configuration.ContinuationMarker, 0, 0, 0 ) } );

        // Draw the after continuation markers
        DrawMarkers( lMarkers.Where( x => x.Position == MarkerPosition.AfterContinuationMarker ).ToList() );
    }

    /// <summary>
    /// Draws a multiline text entry.
    /// </summary>
    private unsafe void DrawMultilineTextInput()
    {
        // Default size of the text input.
        float size_y = GetDefaultInputHeight();

        // The following two variables don't necessarily need
        // to be variables but they are helpful for debugging
        // Get the cursor position
        float cursorPosY = ImGui.GetCursorPosY();

        // Get the content region.
        float contentRegionY = ImGui.GetContentRegionMax().Y;

        // This loop will shrink the size of the input text so that
        // it does not exceed the available space.
        while ( cursorPosY + GetFooterHeight( size_y ) > contentRegionY && size_y > 0 )
            size_y -= 1;

        // Create a temporary string for the textbox
        string scratch = this.ScratchString;
        
        // Convert to byte buffer for new API
        var buffer = System.Text.Encoding.UTF8.GetBytes(scratch + new string('\0', Wordsmith.Configuration.ScratchPadMaximumTextLength - scratch.Length));
        var span = new Span<byte>(buffer);

        // TODO: Fix InputTextMultiline with new API - callback functionality needs update  
        if (ImGui.InputTextMultiline( $"##ScratchPad{this.ID}MultilineTextEntry",
            span, 
            new System.Numerics.Vector2( -1, size_y ),
            ImGuiInputTextFlags.NoHorizontalScroll ))
        {
            // Convert back to string and update
            var nullIndex = Array.IndexOf(buffer, (byte)0);
            if (nullIndex >= 0)
            {
                scratch = System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex);
            }
        }

        // This will fix Ctrl+C copy/paste.
        if ( ImGui.IsItemFocused() && (ImGui.IsKeyDown( ImGuiKey.LeftCtrl ) || ImGui.IsKeyDown( ImGuiKey.RightCtrl )) && ImGui.IsKeyPressed( ImGuiKey.C ) )
        {
            string clipped = ImGui.GetClipboardText();
            clipped = clipped.Unwrap();
            ImGui.SetClipboardText( clipped );
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
        if ( this._corrections is null )
            return;

        if ( this._corrections.Count > 0 )
        {
            int index = 0;
            // Get the fist incorrect word.
            Word word = this._corrections[index];

            // Notify of the spelling error.
            ImGui.TextColored( new System.Numerics.Vector4( 1, 0, 0, 1 ), "Spelling Error:" );

            // Draw the text input.
            ImGui.SameLine( 0, 0 );
            ImGui.SetNextItemWidth( ImGui.GetContentRegionMax().X - ImGui.CalcTextSize( "Spelling Error: " ).X - (120 * ImGuiHelpers.GlobalScale) );
            string wordText = word.GetWordString( this.ScratchString.Trim().Unwrap() );
            this._replaceText = wordText;

            if ( ImGui.InputText( $"##ScratchPad{this.ID}ReplaceTextTextbox", ref this._replaceText, 128, ImGuiInputTextFlags.EnterReturnsTrue ) )
                OnReplace( index );

            // If the user right clicks the text show the popup.
            bool showPop = ImGui.IsItemClicked(ImGuiMouseButton.Right);
            if ( showPop )
                ImGui.OpenPopup( $"ScratchPad{this.ID}ReplaceContextMenu" );

            // Build the popup.
            if ( ImGui.BeginPopup( $"ScratchPad{this.ID}ReplaceContextMenu" ) )
            {
                try
                {
                    //    // Create a selectable to add the word to dictionary.
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
                            if ( ImGui.BeginListBox( "##SuggestionsListBox", new( childwidth, (word.Suggestions.Count > 5 ? 5 : word.Suggestions.Count) * Wordsmith.BUTTON_Y.Scale() ) ) )
                            {
                                // List the suggestions.
                                foreach ( string suggestion in word.Suggestions )
                                {
                                    if ( ImGui.Selectable( $"{suggestion}##ScratchPad{this.ID}Replacement" ) )
                                    {
                                        this._replaceText = suggestion;
                                        OnReplace( 0 );
                                    }
                                }
                                ImGui.EndListBox();
                            }
                        }
                    }
                }
                catch ( Exception e ) { DumpError( e ); }
                ImGui.EndPopup();
            }
            // If they mouse over the input, tell them to use the enter key to replace.
            ImGuiExt.SetHoveredTooltip( "Fix the spelling of the word and hit enter or right click for more options." );

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
        ImGuiStylePtr style = ImGui.GetStyle();

        // If automatic spellcheck is enabled then don't make room for a third button.
        int spellcheck = Wordsmith.Configuration.AutoSpellCheck ? 0 : 1;
        float buttonWidth = (ImGui.GetContentRegionMax().X - style.WindowPadding.X - (style.FramePadding.X * (spellcheck > 0 ? 4 : 2))) / (2 + spellcheck);
        DrawCopyButton( buttonWidth );

        ImGui.SameLine();
        DrawClearButton( buttonWidth );

        if ( spellcheck > 0 )
        {
            ImGui.SameLine();
            DrawSpellcheckButton( buttonWidth );
        }

        // If not configured to automatically delete scratch pads, draw the delete button.
        if ( !Wordsmith.Configuration.DeleteClosedScratchPads )
        {
            if ( ImGui.Button( $"Delete Pad##Scratch{this.ID}", ImGuiHelpers.ScaledVector2( -1, Wordsmith.BUTTON_Y ) ) )
            {
                if ( Wordsmith.Configuration.ConfirmDeleteClosePads )
                    DoClose();

                else
                    WordsmithUI.RemoveWindow( this );
            }
        }
    }

    /// <summary>
    /// Draws the copy button depending on how many chunks are available.
    /// </summary>
    private void DrawCopyButton( float width )
    {
        // If there is more than 1 chunk.
        if ( this._chunks.Count > 1 )
        {
            // Push the icon font for the character we need then draw the previous chunk button.
            ImGui.PushFont( UiBuilder.IconFont );
            if ( ImGui.Button( $"{(char)0xF100}##{this.ID}ChunkBackButton", ImGuiHelpers.ScaledVector2( Wordsmith.BUTTON_Y, Wordsmith.BUTTON_Y ) ) )
            {
                --this._nextChunk;
                if ( this._nextChunk < 0 )
                    this._nextChunk = this._chunks.Count - 1;
            }
            // Reset the font.
            ImGui.PushFont( UiBuilder.DefaultFont );

            // Draw the copy button with no spacing.
            ImGui.SameLine( 0, 0 );
            if ( ImGui.Button( $"Copy{(this._chunks.Count > 1 ? $" ({this._nextChunk + 1}/{this._chunks.Count})" : "")}##ScratchPad{this.ID}", new( width - Wordsmith.BUTTON_Y.Scale() * 2, Wordsmith.BUTTON_Y.Scale() ) ) )
                DoCopyToClipboard();

            // Push the font and draw the next chunk button with no spacing.
            ImGui.PushFont( UiBuilder.IconFont );
            ImGui.SameLine( 0, 0 );
            if ( ImGui.Button( $"{(char)0xF101}##{this.ID}ChunkBackButton", ImGuiHelpers.ScaledVector2( Wordsmith.BUTTON_Y, Wordsmith.BUTTON_Y ) ) )
            {
                ++this._nextChunk;
                if ( this._nextChunk >= this._chunks.Count )
                    this._nextChunk = 0;
            }
            // Reset the font.
            ImGui.PushFont( UiBuilder.DefaultFont );
        }
        else // If there is only one chunk simply draw a normal button.
        {
            if ( ImGui.Button( $"Copy{(this._chunks.Count > 1 ? $" ({this._nextChunk + 1}/{this._chunks.Count})" : "")}##ScratchPad{this.ID}", new( width, Wordsmith.BUTTON_Y.Scale() ) ) )
                DoCopyToClipboard();
        }
    }

    /// <summary>
    /// Draws the copy button depending on how many chunks are available.
    /// </summary>
    private void DrawClearButton( float width )
    {
        // If undo is enabled and there is history.
        if ( this._canUndo && this._text_history.Count > 0 )
        {
            if ( ImGui.Button( $"Clear##ScratchPad{this.ID}", new( width - Wordsmith.BUTTON_Y.Scale(), Wordsmith.BUTTON_Y.Scale() ) ) )
                DoClearText();

            // Push the font and draw the next chunk button with no spacing.
            ImGui.PushFont( UiBuilder.IconFont );
            ImGui.SameLine( 0, 0 );
            if ( ImGui.Button( $"{(char)0xF0E2}##{this.ID}UndoClearButton", new( Wordsmith.BUTTON_Y.Scale(), Wordsmith.BUTTON_Y.Scale() ) ) )
                UndoClearText();

            // Reset the font.
            ImGui.PushFont( UiBuilder.DefaultFont );
        }
        else // If there is only one chunk simply draw a normal button.
        {
            if ( ImGui.Button( $"Clear##ScratchPad{this.ID}", new( width, Wordsmith.BUTTON_Y.Scale() ) ) )
                DoClearText();
        }
    }

    /// <summary>
    /// Draws the spell check button
    /// </summary>
    /// <param name="width"></param>
    private void DrawSpellcheckButton( float width )
    {
        // If spell check is disabled, make the button dark so it appears as though it is disabled.
        if ( !Lang.Enabled )
            ImGui.PushStyleVar( ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f );

        if ( this.ScratchString.Length == 0 )
            ImGui.BeginDisabled();

        if ( ImGui.Button( $"Spell Check##Scratch{this.ID}", new( width, Wordsmith.BUTTON_Y.Scale() ) ) )
            DoSpellCheck();

        if ( this.ScratchString.Length == 0 )
            ImGui.EndDisabled();

        // If spell check is disabled, pop the stylevar to return to normal.
        if ( !Lang.Enabled )
            ImGui.PopStyleVar();
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
    /// </summary>
    /// <param name="s">The text for the history.</param>
    private void DrawHistoryItem( PadState pad, int idx )
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        string sUnwrapped = pad.ScratchText.Unwrap();
        ImGui.SetNextItemWidth( ImGui.GetContentRegionAvail().X - style.WindowPadding.X );
        if ( ImGui.CollapsingHeader( $"{sUnwrapped.Replace( "\n", "" )}##HistoryItemHeader" ) )
        {
            ImGui.Indent();
            try
            {
                ImGui.BeginGroup();
                // Get the text chunks.
                List<TextChunk>? result = ChatHelper.FFXIVify(pad.Header!, pad.ScratchText.Unwrap(), pad.UseOOC);
                if ( result == null )
                    return;

                List<TextChunk> tlist = result;
                float fSpaceWidth = ImGui.CalcTextSize( " " ).X;
                // Display the chunks.
                for ( int i = 0; i < tlist.Count; ++i )
                {
                    if ( i > 0 )
                        ImGui.Spacing();

                    List<ChunkMarker> markers = new();
                    foreach ( ChunkMarker cm in Wordsmith.Configuration.ChunkMarkers )
                        if ( cm.AppliesTo( i, this._chunks.Count ) && cm.Visible( this._useOOC, this._chunks.Count ) )
                            markers.Add( cm );
                    DrawChunkItem( tlist[i], pad.Header!.ChatType, pad.UseOOC, i, tlist.Count, fSpaceWidth, markers, null );
                }

                // End the group.
                ImGui.EndGroup();

                // Create a rectangle around ther group. This will be used to detect if the mouse is clicking
                // in this history item.
                Vector2 vMin = ImGui.GetItemRectMin();
                Vector2 vMax = ImGui.GetItemRectMax();
                Rect r;
                r.Left = (int)vMin.X;
                r.Top = (int)vMin.Y;
                r.Right = (int)vMax.X;
                r.Bottom = (int)vMax.Y;

                if ( ImGui.BeginPopup( $"ScratchPad{this.ID}History{idx}Popup" ) )
                {
                    if ( ImGui.MenuItem( $"Reload Pad State##ScratchPad{this.ID}HistoryItem{idx}Reload" ) )
                    {
                        // If there is currently written text, require confirmation.
                        if ( this.ScratchString != "" )
                            WordsmithUI.ShowMessageBox(
                                "Load History?", "Loading the history state will overwrite\nany currently written text and chat headers.",
                                MessageBox.ButtonStyle.OkCancel,
                                new Action<MessageBox>( ( m ) =>
                                {
                                    if ( m.Result == MessageBox.DialogResult.Ok )
                                        LoadState( pad );
                                } )
                            );

                        // If there is no currently written text just load the state.
                        else
                            LoadState( pad );

                        // Close the popup
                        ImGui.CloseCurrentPopup();
                    }
                    else if ( ImGui.MenuItem( $"Copy All To Clipboard##ScratchPad{this.ID}HistoryItem{idx}Copy" ) )
                    {
                        // Get each chunk
                        if ( result != null )
                        {
                            // Get the text from every chunk.
                            List<string> text = new();
                            for ( int i = 0; i < result.Count; i++ )
                                text.Add( CreateCompleteTextChunk( result[i], pad.UseOOC, i, result.Count ) );

                            // Set the clipboard text.
                            ImGui.SetClipboardText( string.Join( '\n', text ) );

                            // Notify the user that the text was copied.
                            //Wordsmith.PluginInterface.UiBuilder.AddNotification( "Copied text to clipboard!", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Success );

                            Wordsmith.NotificationManager.AddNotification(new()
                            {
                                Content = "Copied text to clipboard!",
                                Title = "Wordsmith",
                                Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
                            });
                        }

                        // Close the popup
                        ImGui.CloseCurrentPopup();
                    }
                    else if ( ImGui.MenuItem( $"Copy Text To Clipboard##ScratchPad{this.ID}HIstoryItem{idx}Copy" ) )
                        ImGui.SetClipboardText( pad.ScratchText.Unwrap() );

                    else if ( ImGui.MenuItem( $"Delete##ScratchPad{this.ID}HistoryItem{idx}Delete" ) )
                    {
                        // Remove the history item.
                        this._text_history.Remove( pad );

                        // Close the popup
                        ImGui.CloseCurrentPopup();
                    }
                    else if ( ImGui.MenuItem( $"Delete All##ScratchPad{this.ID}HistoryItem{idx}DeleteAll" ) )
                    {
                        // Remove the history item.
                        this._text_history.Clear();

                        // Close the popup
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }

                // If the window is focused check if a right click was performed on the object.
                if ( this.IsFocused && ImGui.IsMouseClicked( ImGuiMouseButton.Right ) && r.Contains( ImGui.GetMousePos() ) )
                    ImGui.OpenPopup( $"ScratchPad{this.ID}History{idx}Popup" );
            }
            catch ( Exception e ) { DumpError( e ); }
            ImGui.Unindent();
        }
    }

    /// <summary>
    /// Draws the footer for the History view.
    /// </summary>
    private void DrawHistoryFooter()
    {
        if ( ImGui.Button( $"Close##{this.ID}closehistorybutton", new( ImGui.GetWindowContentRegionMax().X - ImGui.GetStyle().FramePadding.X * 2, Wordsmith.BUTTON_Y.Scale() ) ) )
            this._view_mode = VIEW_MODE_PAD;
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
        catch ( Exception e ) { DumpError( e ); }
    }
    #endregion

    #region Stats
    /// <summary>
    /// Draws the user's word usage statistics
    /// </summary>
    private void DrawStats()
    {
        Vector2 contentRegion = ImGui.GetContentRegionMax();
        contentRegion.Y -= ImGui.GetCursorPosY() + GetFooterHeight();
        if ( ImGui.BeginChild( $"HistoryChild", contentRegion ) )
        {
            ImGui.TextWrapped( $"The following is a list of all words used in this scratch pad. Note that this list is only words that were copied to the clipboard." );
            // Display the stats as a table
            if ( ImGui.BeginTable( $"StatsTable{this.ID}", 2 ) )
            {
                ImGui.TableSetupColumn( $"StatsTableWordCol", ImGuiTableColumnFlags.WidthStretch );
                ImGui.TableSetupColumn( $"StatsTableUsesCol", ImGuiTableColumnFlags.WidthStretch );

                bool word = this._stats_order_by.StartsWith("word");
                bool desc = this._stats_order_by.EndsWith("descending");

                ImGui.TableNextColumn();
                ImGui.TableHeader( $"Word {(word ? (desc ? "↓" : "↑") : "")}" );
                if ( ImGui.IsItemClicked() )
                {
                    if ( this._stats_order_by == "word" )
                        this._stats_order_by = "worddescending";
                    else
                        this._stats_order_by = "word";
                }

                ImGui.TableNextColumn();
                ImGui.TableHeader( $"Uses {(!word ? (desc ? "↓" : "↑") : "")}" );
                if ( ImGui.IsItemClicked() )
                {
                    if ( this._stats_order_by == "usesdescending" )
                        this._stats_order_by = "uses";
                    else
                        this._stats_order_by = "usesdescending";
                }

                List<KeyValuePair<string, int>> wordUses = word
                    ? this._statisticsTracker.ListWordsByWord( desc )
                    : this._statisticsTracker.ListWordsByCount( desc );

                foreach ( KeyValuePair<string, int> kvp in wordUses )
                {
                    ImGui.TableNextColumn();
                    ImGui.Text( $"{kvp.Key}" );

                    ImGui.TableNextColumn();
                    ImGui.Text( $"{kvp.Value}" );
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Draws the footer for the statistics view.
    /// </summary>
    private void DrawStatsFooter()
    {
        if ( ImGui.Button( $"Clear Statistics##{this.ID}ClearStatisticsButton", new( ImGui.GetWindowContentRegionMax().X - ImGui.GetStyle().FramePadding.X * 2, Wordsmith.BUTTON_Y.Scale() ) ) )
        {
            this._statisticsTracker.Clear();
            this._view_mode = VIEW_MODE_PAD;
        }
        if ( ImGui.Button( $"Close##{this.ID}closehistorybutton", new( ImGui.GetWindowContentRegionMax().X - ImGui.GetStyle().FramePadding.X * 2, Wordsmith.BUTTON_Y.Scale() ) ) )
            this._view_mode = VIEW_MODE_PAD;
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
            ImGui.SetClipboardText( CreateCompleteTextChunk( this._chunks[this._nextChunk], this._useOOC, this._nextChunk, this._chunks.Count ) );

            // Add the text to the tracker if tracking enabled.
            if ( Wordsmith.Configuration.TrackWordStatistics )
                this._statisticsTracker.AddChunk( this._chunks[this._nextChunk] );

            this._nextChunk++;

            // If we're not at the last chunk, return.
            if ( this._nextChunk < this._chunks.Count )
                return;

            // After this point, we assume we've copied the last chunk.
            this._nextChunk = 0;

            // If configured to clear text after last copy
            if ( Wordsmith.Configuration.AutomaticallyClearAfterLastCopy )
                DoClearText();
        }
        catch ( Exception e ) { DumpError( e ); }
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
        catch ( Exception e ) { DumpError( e ); }
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
        catch ( Exception e ) { DumpError( e ); }
    }

    /// <summary>
    /// Checks the current scratch text for spelling errors.
    /// </summary>
    private void DoSpellCheck()
    {
        try
        {
            // Ensure that no automated spell checks happen.
            this._do_spell_check = false;

            // Do the spell check.
            this._corrections = SpellChecker.CheckString( this.ScratchString.Unwrap() );
        }
        catch ( Exception e )
        {
            DumpError( e );
        }
    }
    #endregion

    #region Callbacks
    /// <summary>
    /// Adds the word to the dictionary and removes any subsequent correction requestions with
    /// the same word in it.
    /// </summary>
    /// <param name="index"><see cref="int"/> index of the correction in correction list.</param>
    private bool OnAddToDictionary( int index )
    {
        try
        {
            // Get the word
            Word word = this._corrections[index];
            string newWord = word.GetWordString(this.ScratchString.Unwrap());

            // Remove the cleaned word from the dictionary
            this._corrections.RemoveAt( index );

            // Add the cleaned word to the dictionary.
            bool result = Lang.AddDictionaryEntry( newWord );

            return result;
        }
        catch ( Exception e ) { DumpError( e ); }
        return false;
    }

    /// <summary>
    /// Handles DataChanged event for the HeaderData
    /// </summary>
    /// <param name="sender">The object sending the data change event</param>
    /// <param name="e">Unused</param>
    internal void OnDataChanged( HeaderData sender )
    {
        if ( sender == null )
            return;

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
        catch ( Exception e ) { DumpError( e ); }
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
                        Wordsmith.PluginLog.Verbose( $"Moved cursor to the right." );
                    }
                }
                if ( data->CursorPos > 0 )
                {
                    while ( data->CursorPos > 0 && data->Buf[data->CursorPos - 1] == '\r' )
                    {
                        data->CursorPos--;
                        Wordsmith.PluginLog.Verbose( "Moved cursor to the left." );
                    }
                }
            }
            else
            {
                OnTextEdit( data );
                OnTextChanged();
            }
        }
        catch ( Exception e ) { DumpError( e ); }
        return 0;
    }

    /// <summary>
    /// Updates text chunks on text changed.
    /// </summary>
    internal void OnTextChanged()
    {
        try
        {
            // If text has been entered, disable undo.
            if ( this.ScratchString.Length > 0 )
                this._canUndo = false;

            // Invalidate the chunks.
            this._invalidateChunks |= true;
            this._nextChunk = 0;

            this._corrections?.Clear();
            if ( Wordsmith.Configuration.AutoSpellCheck )
            {
                this._rest_time = Wordsmith.Configuration.AutoSpellCheckDelay;
                this._do_spell_check = true;
            }
        }
        catch ( Exception e ) { DumpError( e ); }
    }

    /// <summary>
    /// Alters text input buffer in real time to create word wrap functionality in multiline textbox.
    /// </summary>
    /// <param name="data">Pointer to callback data</param>
    /// <returns></returns>
    public unsafe int OnTextEdit( ImGuiInputTextCallbackData* data )
    {
        try
        {
            // If _ignoreTextEdit is true then the reason for the edit
            // was a resize and the text has already been wrapped so
            // simply return from here.
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
                float width = this._lastWidth - ( style.FramePadding.X*2 + style.ScrollbarSize);
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
        catch ( Exception e ) { DumpError( e ); }
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
        if ( Math.Abs( ImGui.GetContentRegionMax().X - this._lastWidth ) > 0.1 )
        {
            // Don't flag to ignore text edit if the window was just opened.
            if ( this._lastWidth > 0.1 )
                this._ignoreTextEdit = true;

            // Rewrap scratch
            ImGuiStylePtr style = ImGui.GetStyle();
            float width = this._lastWidth - ( style.FramePadding.X + style.ScrollbarSize );

            // Update the string only if it is actually affected by the wrapping. This will prevent
            // spell checking rewraps that don't change anything.
            string s = this.ScratchString.Wrap( width );
            if ( s != this.ScratchString )
                this.ScratchString = s;

            // Update the last known width.
            this._lastWidth = ImGui.GetContentRegionMax().X;
        }
    }

    /// <summary>
    /// Check for a chat header at the start of a string.
    /// </summary>
    /// <param name="text">Text to parse from</param>
    /// <param name="cursorPos">Cursor position</param>
    /// <returns>The text string with header removed.</returns>
    private string CheckForHeader( string text, ref int cursorPos )
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

                text = text.Remove( 0, len + 1 );
                cursorPos -= len + 1;
            }
        }
        return text;
    }

    /// <summary>
    /// Assembles the chunk into a usable string.
    /// </summary>
    /// <param name="chunk">The <see cref="TextChunk"/> to build around.</param>
    /// <param name="OOC">A <see cref="bool"/> indicating whether to include OOC markers</param>
    /// <param name="index">A <see cref="int"/> index of the chunk in its list.</param>
    /// <param name="count">A <see cref="int"/> count of the number of chunks.</param>
    /// <returns>A <see cref="string"/> with all relevant data.</returns>
    private static string CreateCompleteTextChunk( TextChunk chunk, bool OOC, int index, int count )
    {
        // Build a string with:
        string result = chunk.Header.Length > 0 ? $"{chunk.Header} " : "";

        // Get the applicable markers.
        List<ChunkMarker> markers = new();
        foreach ( ChunkMarker cm in Wordsmith.Configuration.ChunkMarkers )
            if ( cm.AppliesTo( index, count ) && cm.Visible( OOC, count ) )
                markers.Add( cm );

        // Add the before OOC marks.
        List<ChunkMarker> current = markers.Where(x => x.Position == MarkerPosition.BeforeOOC).ToList();
        if ( current.Count > 0 )
            result += string.Join( ' ', current.Select( c => c.Text ) );

        // If using OOC then add the starting tag
        if ( OOC )
            result += chunk.OutOfCharacterStartTag;

        // Insert custom markers before the body.
        current = markers.Where( x => x.Position == MarkerPosition.BeforeBody ).ToList();
        if ( current.Count > 0 )
            result += string.Join( ' ', current.Select( c => c.Text ) );

        // The text body is the chunk text.
        result += chunk.Text;

        // Insert custom markers after the body.
        current = markers.Where( x => x.Position == MarkerPosition.AfterBody ).ToList();
        if ( current.Count > 0 )
            result += string.Join( ' ', current.Select( c => c.Text ) );

        // If using OOC then add the ending tag
        if ( OOC )
            result += chunk.OutOfCharacterEndTag;

        // Insert the after OOC marks
        current = markers.Where( x => x.Position == MarkerPosition.AfterOOC ).ToList();
        if ( current.Count > 0 )
            result += string.Join( ' ', current.Select( c => c.Text ) );

        // Add the continuation marker
        if ( count > 1 && (index + 1 < count || Wordsmith.Configuration.ContinuationMarkerOnLast) )
            result += ' '+chunk.ContinuationMarker.ReplacePlaceholders( index + 1, count );

        // Add the After Continuation marks.
        current = markers.Where( x => x.Position == MarkerPosition.AfterContinuationMarker ).ToList();
        if ( current.Count > 0 )
            result += string.Join( ' ', current.Select( c => c.Text ) );

        return result;
    }

    /// <summary>
    /// Dump the scratch pad data to an error JSON.
    /// </summary>
    /// <param name="e"><see cref="Exception"/> to pass along with the data.</param>
    private void DumpError( Exception e )
    {
        this.IsOpen = false;
        Dictionary<string, object> dump = this.Dump();
        dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
        dump["Window"] = $"ScratchPadUI #{this.ID}";
        WordsmithUI.ShowErrorWindow( dump );
    }

    /// <summary>
    /// Runs FFXIVify on this pad.
    /// </summary>
    internal void FFXIVify() => this._chunks = ChatHelper.FFXIVify( this.Header, this.ScratchString.Unwrap(), this.UseOOC ) ?? new();

    /// <summary>
    /// Returns the default height of the text input.
    /// </summary>
    /// <returns></returns>
    private static float GetDefaultInputHeight() => Wordsmith.Configuration.ScratchPadInputLineHeight * WordsmithUI.LineHeight;

    /// <summary>
    /// Gets the height of the footer.
    /// </summary>
    /// <param name="IncludeTextbox">If false, the height of the textbox is not added to the result.</param>
    /// <returns>Returns the height of footer elements as a <see langword="float"/></returns>
    private float GetFooterHeight( float inputSize = -1 )
    {
        if ( this._view_mode == VIEW_MODE_PAD )
        {
            // Text input size can either be given or calculated.
            float result = inputSize > 0 ? inputSize : GetDefaultInputHeight();
            int paddingCount = 2;

            // Delete pad button
            if ( !Wordsmith.Configuration.DeleteClosedScratchPads )
            {
                result += Wordsmith.BUTTON_Y.Scale();
                paddingCount++;
            }

            // Replace Text section
            if ( this._corrections.Count > 0 )
            {
                result += Wordsmith.BUTTON_Y.Scale();
                paddingCount++;
            }

            // Button row
            result += Wordsmith.BUTTON_Y.Scale();
            paddingCount++;

            // Padding
            result += ImGui.GetStyle().FramePadding.Y * paddingCount;

            return result;
        }
        else if ( this._view_mode == VIEW_MODE_HISTORY )
            return Wordsmith.BUTTON_Y.Scale() + ImGui.GetStyle().FramePadding.Y;
        else if ( this._view_mode == VIEW_MODE_STATS )
            return (Wordsmith.BUTTON_Y.Scale() + ImGui.GetStyle().FramePadding.Y) * 2;

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
    private void LoadState( PadState state )
    {
        // Revert to given padstate
        this._header = state.Header!;
        this.ScratchString = state.ScratchText;

        // Exit history.
        this._view_mode = VIEW_MODE_PAD;
    }
    #endregion
}
