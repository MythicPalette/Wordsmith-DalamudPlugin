using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Wordsmith.Enums;
using Wordsmith.Helpers;

namespace Wordsmith.Gui;

internal sealed class SettingsUI : Window
{
    private const int MAX_MARKER_FRAME_Y = 10;

    /// <summary>
    /// Gets the available size for tab pages while leaving room for the footer.
    /// </summary>
    /// <returns>Returns a float representing the available canvas height for settings tabs.</returns>
    private float GetCanvasSize() => ImGui.GetContentRegionMax().Y - ImGui.GetCursorPosY() - (Wordsmith.BUTTON_Y*ImGuiHelpers.GlobalScale) - (this._style.FramePadding.Y * 2);

    private bool _newMarkerHeaderOpen = false;

    // General settings.
    private bool _showAdvancedSettings = Wordsmith.Configuration.ShowAdvancedSettings;
    private bool _neverShowNotices = Wordsmith.Configuration.NeverShowNotices;
    private string _lastSeenNotice = Wordsmith.Configuration.LastNoticeRead;
    private int _searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
    private bool _researchToTopChange = Wordsmith.Configuration.ResearchToTop;
    private bool _trackWordStats = Wordsmith.Configuration.TrackWordStatistics;

    // Scratch Pad settings.
    private bool _autoClear = Wordsmith.Configuration.AutomaticallyClearAfterLastCopy;
    private bool _deleteClosed = Wordsmith.Configuration.DeleteClosedScratchPads;
    private bool _confirmDeleteClosed = Wordsmith.Configuration.ConfirmDeleteClosePads;
    private bool _ignoreHypen = Wordsmith.Configuration.IgnoreWordsEndingInHyphen;
    private bool _showChunks = Wordsmith.Configuration.ShowTextInChunks;
    private bool _onSentence = Wordsmith.Configuration.SplitTextOnSentence;
    private bool _detectHeader = Wordsmith.Configuration.ParseHeaderInput;
    private string _oocOpening = Wordsmith.Configuration.OocOpeningTag;
    private string _oocClosing = Wordsmith.Configuration.OocClosingTag;
    private bool _oocByDefault = Wordsmith.Configuration.OocByDefault;
    private string _sentenceTerminators = Wordsmith.Configuration.SentenceTerminators;
    private string _encapTerminators = Wordsmith.Configuration.EncapsulationTerminators;
    private List<ChunkMarker> _chunkMarkers = Wordsmith.Configuration.ChunkMarkers;
    private ChunkMarker _newMarker = new();
    private string _continuationMarker = Wordsmith.Configuration.ContinuationMarker;
    private bool _markLastChunk = Wordsmith.Configuration.ContinuationMarkerOnLast;
    private int _scratchMaxTextLen = Wordsmith.Configuration.ScratchPadMaximumTextLength;
    private int _scratchInputLineHeight = Wordsmith.Configuration.ScratchPadInputLineHeight;

    // Alias Settings
    private int _newAliasSelection = 0;
    private string _newAlias = "";
    private string _newAliasTarget = "";
    private List<(int ChatType, string Alias, object? data)> _headerAliases = new(Wordsmith.Configuration.HeaderAliases);

    // Spellcheck Settings
    private bool _fixDoubleSpace = Wordsmith.Configuration.ReplaceDoubleSpaces;
    private string _dictionaryFilename = Wordsmith.Configuration.DictionaryFile;
    private int _maxSuggestions = Wordsmith.Configuration.MaximumSuggestions;
    private bool _autospellcheck = Wordsmith.Configuration.AutoSpellCheck;
    private float _autospellcheckdelay = Wordsmith.Configuration.AutoSpellCheckDelay;
    private string _punctuationCleaningString = Wordsmith.Configuration.PunctuationCleaningList;

    // Linkshell Settings
    private string[] _cwlinkshells = Wordsmith.Configuration.CrossWorldLinkshellNames;
    private string[] _linkshells = Wordsmith.Configuration.LinkshellNames;

    // Colors Settings
    private Vector4 _backupColor = new();
    private bool _enableTextColor = Wordsmith.Configuration.EnableTextHighlighting;
    private Vector4 _spellingErrorColor = Wordsmith.Configuration.SpellingErrorHighlightColor;
    private Dictionary<int, Vector4> _headerColors = Wordsmith.Configuration.HeaderColors.Clone();

    private ImGuiStylePtr _style = ImGui.GetStyle();

    internal static string GetWindowName() => $"{Wordsmith.APPNAME} - Settings";

    public SettingsUI() : base( GetWindowName() )
    {
        this._searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
        this._researchToTopChange = Wordsmith.Configuration.ResearchToTop;
        //Size = new(375, 350);
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new( 510, 450 ),
            MaximumSize = new( 9999, 9999 )
        };

        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
    }

    public override void Update()
    {
        base.Update();

        if (!this.IsOpen)
            WordsmithUI.RemoveWindow(this);
        if ( Wordsmith.Configuration.RecentlySaved )
            ResetValues();
    }

    public override void Draw()
    {
        try
        {
            if ( Wordsmith.Configuration.RecentlySaved )
                ResetValues();

            if ( ImGui.BeginTabBar( "SettingsUITabBar" ) )
            {
                DrawGeneralTab();
                DrawScratchPadTab();
                DrawAliasesTab();
                DrawSpellCheckTab();
                DrawLinkshellTab();
                DrawColorsTab();
                ImGui.EndTabBar();
            }

            ImGui.Separator();
            DrawFooter();
        }
        catch ( Exception e )
        {
            OnException( e );
        }
    }

    private void DrawGeneralTab()
    {
        if ( ImGui.BeginTabItem( "General##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "GeneralSettingsChildFrame", new( -1, GetCanvasSize() ) ) )
            {
                ImGui.Checkbox( $"Show Advanced Settings", ref this._showAdvancedSettings );
                ImGui.SameLine();

                ImGui.Checkbox( "Never Show Notices", ref this._neverShowNotices );
                ImGuiExt.SetHoveredTooltip( "Enabling this will prevent Wordsmith from showing new notices upon opening.\nAlready seen notices don't repeat." );

                ImGui.SameLine();
                ImGui.Checkbox( "Track Word Usage.", ref this._trackWordStats );
                ImGuiExt.SetHoveredTooltip( "This is a metric to help you avoid using the same words too often by counting each time you use a word." );

                if ( Wordsmith.Configuration.ShowAdvancedSettings )
                {
                    ImGui.SetNextItemWidth( ImGui.CalcTextSize( " AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA " ).X + this._style.FramePadding.X * 2 );
                    ImGui.InputText( "Last Notice GUID", ref this._lastSeenNotice, 26 );
                    ImGuiExt.SetHoveredTooltip( "Please note that if you change this or clear this it will cause the latest notice to be shown again." );

                    if ( ImGui.Button( $"Refresh Web manifest##RefreshDictionaryManifestButton" ) )
                        Wordsmith.ReloadManifest();
                    ImGuiExt.SetHoveredTooltip( $"Refresh the list of available dictionaries." );
                }
                //Search history count
                //ImGui.DragInt("Search History Size", ref _searchHistoryCountChange, 0.1f, 1, 50);
                ImGui.SetNextItemWidth( ImGui.GetContentRegionMax().X - this._style.WindowPadding.X - ImGui.CalcTextSize("Thesaurus History Size").X );
                ImGui.DragInt( "Thesaurus History Size", ref this._searchHistoryCountChange, 1, 1, 100 );
                ImGuiExt.SetHoveredTooltip( "This is the number of searches to keep in memory at one time. Setting to 0 is unlimited.\nNote: The more you keep, them more memory used." );

                //Re-search to top
                ImGui.Checkbox( "Move repeated search to top of history.", ref this._researchToTopChange );
                ImGuiExt.SetHoveredTooltip( "If enabled, when searching for a word you've searched\nalready, it will move it to the top of the list." );

                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawScratchPadTab()
    {
        if ( ImGui.BeginTabItem( "Scratch Pad##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "SettingsUIScratchPadChildFrame", new( -1, GetCanvasSize() ) ) )
            {
                //ImGui.Text( "General Behavior" );
                if ( ImGui.CollapsingHeader( "General Behavior" ) )
                {
                    ImGui.Indent();
                    if ( ImGui.BeginTable( "SettingsUiScratchPadBehaviorTable", 2, ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersInnerV ) )
                    {
                        ImGui.TableSetupColumn( "SettingsUiScratchPadBehaviorTableLeftColumn" );
                        ImGui.TableSetupColumn( "SettingsUiScratchPadBehaviorTableRightColumn" );

                        ImGui.TableNextColumn();
                        // Auto-Clear Scratch Pad
                        ImGui.Checkbox( "Auto-clear Scratch Pad", ref this._autoClear );
                        ImGuiExt.SetHoveredTooltip( "Automatically clears text from scratch pad after copying last chunk." );

                        ImGui.TableNextColumn();
                        // Auto Delete Scratch Pads
                        ImGui.Checkbox( "Auto-Delete Scratch Pads On Close##SettingsUICheckbox", ref this._deleteClosed );
                        ImGuiExt.SetHoveredTooltip( "When enabled it will delete the scratch pad on close.\nWhen disabled you will have a delete button at the bottom." );

                        ImGui.TableNextColumn();
                        // Auto Delete Scratch Pads
                        ImGui.Checkbox( "Confirm Scratch Pad Delete##SettingsUICheckbox", ref this._confirmDeleteClosed );
                        ImGuiExt.SetHoveredTooltip( "When enabled a confirmation window will appear before deleting the Scratch Pad." );

                        ImGui.TableNextColumn();
                        // Show text in chunks.
                        ImGui.Checkbox( "Show Text In Chunks##SettingsUICheckbox", ref this._showChunks );
                        ImGuiExt.SetHoveredTooltip( "When enabled it will display a large box with text above your entry form.\nThis box will show you how the text will be broken into chunks.\nIn single-line input mode this text will always show but without chunking." );

                        ImGui.TableNextColumn();
                        // Split on sentence
                        ImGui.Checkbox( "Split Text On Sentence##SettingsUICheckbox", ref this._onSentence );
                        ImGuiExt.SetHoveredTooltip( "When enabled, Scratch Pad attempts to do chunk breaks at the end of sentences instead\nof between any words." );

                        ImGui.TableNextColumn();
                        ImGui.Checkbox( "Parse Header From Text##SettingsUICheckbox", ref this._detectHeader );
                        ImGuiExt.SetHoveredTooltip( "When enabled, typing a header into the input text of a scratch pad will cause\nthe scratchpad to try to parse the desired header automatically." );

                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                }
                ImGui.Spacing();

                if ( Wordsmith.Configuration.ShowAdvancedSettings )
                { 
                    if (ImGui.CollapsingHeader( "Text Parsing & Interpolation##settingsheader" ) )
                    {
                        ImGui.Indent();
                        if ( ImGui.BeginTable( "SettingsUiScratchPadBehaviorTable", 2, ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersInnerV ) )
                        {
                            ImGui.TableSetupColumn( "SettingsUiScratchPadChildTableLeftColumn" );
                            ImGui.TableSetupColumn( "SettingsUiScratchPadChildTableRightColumn" );


                            // The string of X's is just a placeholder that gives room for the largest expected text size.
                            float width = ImGui.GetColumnWidth() - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXXXX").X;
                            if ( Wordsmith.Configuration.ShowAdvancedSettings )
                            {
                                // Enter Key Behavior
                                // Get all of the enum options.
                                string[] enterKeyActions = Enum.GetNames(typeof(EnterKeyAction));

                                // Add a space in front of all capital letters.
                                for ( int i = 1; i < enterKeyActions.Length; ++i )
                                    enterKeyActions[i] = enterKeyActions[i].SpaceByCaps();

                                ImGui.TableNextColumn();
                                // Sentence terminators
                                ImGui.SetNextItemWidth( ImGui.GetColumnWidth() - ImGui.CalcTextSize( "Sentence Terminators" ).X );
                                ImGui.InputText( "Sentence Terminators##ScratchPadSplitCharsText", ref this._sentenceTerminators, 32 );
                                ImGuiExt.SetHoveredTooltip( "Each of these characters can mark the end of a sentence when followed by a space of encapsulator.\ni.e. \"A.B\" is not a sentence terminator but \"A. B\" is." );


                                ImGui.TableNextColumn();
                                // Encapsulation terminators
                                ImGui.SetNextItemWidth( ImGui.GetColumnWidth() - ImGui.CalcTextSize( "Encpasulation Terminators" ).X );
                                ImGui.InputText( $"Encpasulation Terminators##ScratchPadEncapCharsText", ref this._encapTerminators, 32 );
                                ImGuiExt.SetHoveredTooltip( $"Each of these characters ends an encapsulator.\nThis is used with sentence terminators in case of encapsulation for chunk breaks\ni.e. \"A) B\" will not count but \"A.) B\" will." );
                            }
                            ImGui.EndTable();
                        }
                        ImGui.Unindent();
                    }
                    ImGui.Spacing();
                }

                if ( ImGui.CollapsingHeader( "Marks & Tags##settingsheader" ) )
                {
                    ImGui.Indent();

                    #region OOC Tags
                    // Draws the OOC tag editor
                    ImGui.SetNextItemWidth( 50 * ImGuiHelpers.GlobalScale );
                    ImGui.InputText( "##OocOpeningTagInputText", ref this._oocOpening, 5 );
                    ImGuiExt.SetHoveredTooltip( "The opening tag for your OOC text." );

                    ImGui.SameLine( 0, 2 );
                    ImGui.Text( "OOC Tags" );

                    ImGui.SameLine( 0, 2 );
                    ImGui.SetNextItemWidth( 50 * ImGuiHelpers.GlobalScale );
                    ImGui.InputText( "##OocClosingTagInputText", ref this._oocClosing, 5 );
                    ImGuiExt.SetHoveredTooltip( "The closing tag for your OOC text." );

                    ImGui.SameLine();
                    ImGui.Checkbox("On By Default##settingscheckbox", ref this._oocByDefault);
                    ImGuiExt.SetHoveredTooltip( "Sets the default state of OOC for Scratch Pads" );
                    ImGui.Separator();

                    ImGui.SetNextItemWidth( ImGui.CalcTextSize( "############" ).X );
                    ImGui.InputText( "Continuation Marker", ref this._continuationMarker, 64 );

                    ImGui.SameLine();
                    ImGui.Checkbox( "On Last Chunk", ref this._markLastChunk );
                    #endregion

                    if ( Wordsmith.Configuration.ShowAdvancedSettings )
                    {
                        ImGui.Separator();

                        float size_y = this._newMarkerHeaderOpen ? (Wordsmith.BUTTON_Y.Scale() + this._style.FramePadding.Y) * 6 : 0;

                        int size_scaler = MAX_MARKER_FRAME_Y - this._chunkMarkers.Count >= 3 ? this._chunkMarkers.Count + 3 : MAX_MARKER_FRAME_Y;
                        size_y += (Wordsmith.BUTTON_Y.Scale() + this._style.FramePadding.Y) * size_scaler;

                        if ( ImGui.BeginChild( "MarkersChildObject", new Vector2( -1, size_y ), true ) )
                        {
                            if ( ImGui.BeginTable( "MarkersChildTable", 7, ImGuiTableFlags.RowBg ) )
                            {
                                ImGui.TableSetupColumn( "MarkerMoveColumn", ImGuiTableColumnFlags.WidthFixed, Wordsmith.BUTTON_Y.Scale() * 2 );
                                ImGui.TableSetupColumn( "MarkerTextColumn", ImGuiTableColumnFlags.WidthStretch );
                                ImGui.TableSetupColumn( "MarkerPositionColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "After Continuation" ).X );
                                ImGui.TableSetupColumn( "MarkerChunkCountColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "With Chunk# " ).X );
                                ImGui.TableSetupColumn( "MarkerOOCColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize( "When OOC is ").X );
                                ImGui.TableSetupColumn( "MarkerShowOnColumn", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn( "MarkerDeleteColumn", ImGuiTableColumnFlags.WidthFixed, Wordsmith.BUTTON_Y.Scale() );

                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "Move##MoveMarkerColumnHeader" );
                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "Text##MarkerColumnHeader" );
                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "Postion##MarkerColumnHeader" );
                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "With Chunk# ##MarkerColumnHeader" );
                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "When OOC is##MarkerColumnHeader" );
                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "Show On##MarkerColumnHeader" );
                                ImGui.TableNextColumn();
                                ImGui.TableHeader( "##MarkerColumnHeaderDelete" );

                                Vector2 padding = this._style.FramePadding;
                                int i = 0;
                                for ( ; i < this._chunkMarkers.Count; i++ )
                                {
                                    ChunkMarker cm = this._chunkMarkers[i];

                                    ImGui.PushID( $"IconButtonID{i}" );
                                    ImGui.TableNextColumn();
                                    // If this is the first item or the item above it is in a different position then
                                    // disable the button to prevent the user from trying to improperly organize the list
                                    bool bDisableMove = i == 0 || this._chunkMarkers[i-1].Position < this._chunkMarkers[i].Position;
                                    if ( bDisableMove )
                                        ImGui.BeginDisabled();
                                    if ( Dalamud.Interface.Components.ImGuiComponents.IconButton( FontAwesomeIcon.ArrowUp ) )
                                    {
                                        this._chunkMarkers.RemoveAt( i );
                                        this._chunkMarkers.Insert( i - 1, cm );
                                        this._chunkMarkers = ChunkMarker.SortList( this._chunkMarkers );
                                    }
                                    ImGuiExt.SetHoveredTooltip( "Move this marker higher in the list. Markers are drawn in order from top\nto bottom. If two markers are set to show in the same\nposition then the higher one will be drawn first. Markers\ncan only be moved inside their own position group." );
                                    if ( bDisableMove )
                                        ImGui.EndDisabled();

                                    ImGui.SameLine( 0, 0 );
                                    // If this is the last item in the list or the item beneath it is in a different position
                                    // then disable the button to prevent the user from trying to improperly organize the list
                                    bDisableMove = i == this._chunkMarkers.Count - 1 || this._chunkMarkers[i+1].Position > this._chunkMarkers[i].Position;
                                    if ( bDisableMove )
                                        ImGui.BeginDisabled();
                                    if ( Dalamud.Interface.Components.ImGuiComponents.IconButton( FontAwesomeIcon.ArrowDown ) )
                                    {
                                        this._chunkMarkers.RemoveAt( i );
                                        this._chunkMarkers.Insert( i + 1, cm );
                                        this._chunkMarkers = ChunkMarker.SortList( this._chunkMarkers );
                                    }
                                    ImGuiExt.SetHoveredTooltip( "Move this marker lower in the list. Markers are drawn in order from top\nto bottom. If two markers are set to show in the same\nposition then the higher one will be drawn first. Markers\ncan only be moved inside their own position group." );
                                    if ( bDisableMove )
                                        ImGui.EndDisabled();

                                    ImGui.TableNextColumn();
                                    ImGui.Text( $"{cm.Text}" );

                                    ImGui.TableNextColumn();
                                    ImGui.Text( $"{Enum.GetName( typeof( MarkerPosition ), cm.Position )!.SpaceByCaps()}" );

                                    ImGui.TableNextColumn();
                                    if ( (cm.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithAnyChunkCount )
                                        ImGui.Text( $"Any" );
                                    else if ( (cm.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithSingleChunk )
                                        ImGui.Text( $"One" );
                                    else if ( (cm.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithMultipleChunks )
                                        ImGui.Text( $"Two Or More" );

                                    ImGui.TableNextColumn();
                                    if ( (cm.DisplayMode & DisplayMode.AnyOOC) == DisplayMode.AnyOOC )
                                        ImGui.Text( $"Any" );
                                    else if ( (cm.DisplayMode & DisplayMode.AnyOOC) == DisplayMode.WithOOC )
                                        ImGui.Text( $"ON" );
                                    else if ( (cm.DisplayMode & DisplayMode.AnyOOC) == DisplayMode.WithoutOOC )
                                        ImGui.Text( $"OFF" );

                                    string sRepeat = $"{Enum.GetName(typeof(RepeatMode), cm.RepeatMode)!.SpaceByCaps()}";
                                    if ( cm.RepeatMode == RepeatMode.EveryNth )
                                        sRepeat = $"Every {cm.Nth} chunk from {cm.StartPosition}";

                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth( -1 );
                                    ImGui.Text( sRepeat );

                                    ImGui.TableNextColumn();
                                    if ( Dalamud.Interface.Components.ImGuiComponents.IconButton( FontAwesomeIcon.Trash ) )
                                        this._chunkMarkers.RemoveAt( i );
                                    ImGuiExt.SetHoveredTooltip( "Delete this entry." );
                                    ImGui.PopID();
                                }
                                ImGui.EndTable();
                            }

                            ImGui.Spacing();
                            ImGui.Separator();
                            if ( ImGui.CollapsingHeader( "Create New Marker##SettingHeader" ) )
                            {
                                this._newMarkerHeaderOpen = true;
                                ImGui.Indent();
                                string sTemp = this._newMarker.Text;
                                ImGui.SetNextItemWidth( -1 );
                                ImGui.InputTextWithHint( "##SettingsUINewMarkerText", "Marker Text", ref sTemp, 32 );
                                this._newMarker.Text = sTemp;

                                #region New Marker Position
                                bool bBeforeOOCRadioValue   = this._newMarker.Position == MarkerPosition.BeforeOOC;
                                bool bBeforeTextRadioValue  = this._newMarker.Position == MarkerPosition.BeforeBody;
                                bool bAfterTextRadioValue   = this._newMarker.Position == MarkerPosition.AfterBody;
                                bool bAfterOOCRadioValue    = this._newMarker.Position == MarkerPosition.AfterOOC;
                                bool bAfterConRadioValue    = this._newMarker.Position == MarkerPosition.AfterContinuationMarker;

                                ImGui.SetNextItemWidth( ImGui.CalcTextSize( "Position: " ).X );
                                ImGui.Text( $"Position:" );

                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "##BeforeOOCRadioButton", bBeforeOOCRadioValue ) )
                                    this._newMarker.Position = MarkerPosition.BeforeOOC;
                                ImGuiExt.SetHoveredTooltip( "Put this marker before the opening OOC marker." );


                                ImGui.SameLine( 0, 0 );
                                ImGui.Text( "OOC" );

                                ImGui.SameLine( 0, 0 );
                                if ( ImGui.RadioButton( "##BeforeTextRadioButton", bBeforeTextRadioValue ) )
                                    this._newMarker.Position = MarkerPosition.BeforeBody;
                                ImGuiExt.SetHoveredTooltip( "Put this marker before the text body but after the OOC opening marker." );

                                ImGui.SameLine( 0, 0 );
                                ImGui.Text( "Body" );

                                ImGui.SameLine( 0, 0 );
                                if ( ImGui.RadioButton( "##AfterTextRadioButton", bAfterTextRadioValue ) )
                                    this._newMarker.Position = MarkerPosition.AfterBody;
                                ImGuiExt.SetHoveredTooltip( "Put this marker after the text body but before the OOC closing marker." );

                                ImGui.SameLine( 0, 0 );
                                ImGui.Text( "OOC" );

                                ImGui.SameLine( 0, 0 );
                                if ( ImGui.RadioButton( "##AfterOOCRadioButton", bAfterOOCRadioValue ) )
                                    this._newMarker.Position = MarkerPosition.AfterOOC;
                                ImGuiExt.SetHoveredTooltip( "Put this marker after the closing OOC marker." );

                                ImGui.SameLine( 0, 0 );
                                ImGui.Text( "Con. Mark" );

                                ImGui.SameLine( 0, 0 );
                                if ( ImGui.RadioButton( "##AfterConRadioButton", bAfterConRadioValue ) )
                                    this._newMarker.Position = MarkerPosition.AfterContinuationMarker;
                                ImGuiExt.SetHoveredTooltip( "Put this marker after the continuation marker" );
                                #endregion

                                #region New Marker Repeat Style
                                ImGui.Text( $"Show On: " );
                                ImGui.SameLine();
                                if ( (this._newMarker.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithSingleChunk )
                                {
                                    ImGui.BeginDisabled();
                                    this._newMarker.RepeatMode = RepeatMode.All;
                                }
                                int iRepeatSelection = (int)this._newMarker.RepeatMode;
                                string[] aRepeatStyles = Enum.GetNames(typeof(RepeatMode)).Select(s => s.SpaceByCaps()).ToArray();
                                ImGui.SetNextItemWidth( ImGui.GetContentRegionAvail().X );
                                ImGui.Combo( "##RepeatStyleSelection", ref iRepeatSelection, aRepeatStyles, aRepeatStyles.Length );
                                ImGuiExt.SetHoveredTooltip( "Set the display mode for the marker.\nNote: If you display with only one chunk then this option is disabled." );
                                if ( (this._newMarker.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithSingleChunk )
                                    ImGui.EndDisabled();
                                this._newMarker.RepeatMode = (RepeatMode)iRepeatSelection;

                                if ( this._newMarker.RepeatMode == RepeatMode.EveryNth )
                                {
                                    ImGui.Indent();
                                    float fRepeatLabelWidth = ImGui.CalcTextSize("Repeat every ").X;
                                    ImGui.SetNextItemWidth( fRepeatLabelWidth );
                                    ImGui.Text( "Repeat every " );
                                    ImGui.SameLine( 0, 0 );

                                    int nth = (int)this._newMarker.Nth;
                                    ImGui.SetNextItemWidth( ImGui.GetContentRegionAvail().X - this._style.WindowPadding.X );
                                    ImGui.DragInt( "##EveryNthInput", ref nth, 0.1f, 1, 100 );
                                    this._newMarker.Nth = (uint)nth;
                                    ImGuiExt.SetHoveredTooltip( $"This the repeat step size. For instance, a value of 3 would have\nevery third chunk marked." );

                                    ImGui.Text( "Starting at: " );
                                    ImGui.SameLine( 0, fRepeatLabelWidth - ImGui.CalcTextSize( "Starting at: " ).X );

                                    int offset = (int)this._newMarker.StartPosition;
                                    ImGui.SetNextItemWidth( ImGui.GetContentRegionAvail().X - this._style.WindowPadding.X );
                                    ImGui.DragInt( "##StartPositioninput", ref offset, 0.1f, 1, 100 );
                                    ImGuiExt.SetHoveredTooltip( $"This the starting position for the repeat. For instance, a value of 5\nwould have this mark not start repeating until the 5th." );
                                    this._newMarker.StartPosition = (uint)offset;
                                }
                                #endregion

                                #region Display Mode
                                ImGui.Text( "Display when there is/are _____ chunks:" );
                                ImGui.Indent();

                                // Convert the flags to booleans
                                bool bSingleChunk   = (this._newMarker.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithSingleChunk;
                                bool bMultiChunk    = (this._newMarker.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithMultipleChunks;
                                bool bAnyChunk      = (this._newMarker.DisplayMode & DisplayMode.WithAnyChunkCount) == DisplayMode.WithAnyChunkCount;


                                // Display the booleans as radio buttons
                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "one##MarkChunkRadioButton", bSingleChunk ) )
                                {
                                    bSingleChunk = true;
                                    bMultiChunk = false;
                                    bAnyChunk = false;
                                }
                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "two or more##MarkChunkRadioButton", bMultiChunk ) )
                                {
                                    bSingleChunk = false;
                                    bMultiChunk = true;
                                    bAnyChunk = false;
                                }
                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "any##MarkChunkRadioButton", bAnyChunk ) )
                                {
                                    bSingleChunk = false;
                                    bMultiChunk = false;
                                    bAnyChunk = true;
                                }
                                ImGui.Unindent();

                                ImGui.Text( "Display when OOC is _____:" );
                                ImGui.Indent();
                                bool bWithOOC = (this._newMarker.DisplayMode & DisplayMode.AnyOOC) == DisplayMode.WithOOC;
                                bool bWithoutOOC = (this._newMarker.DisplayMode & DisplayMode.AnyOOC) == DisplayMode.WithoutOOC;
                                bool bAnyOOC = (this._newMarker.DisplayMode & DisplayMode.AnyOOC) == DisplayMode.AnyOOC;
                                // Display the booleans as radio buttons
                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "on##MarkOOCRadioButton", bWithOOC ) )
                                {
                                    bWithOOC = true;
                                    bWithoutOOC = false;
                                    bAnyOOC = false;
                                }
                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "off##MarkOOCRadioButton", bWithoutOOC ) )
                                {
                                    bWithOOC = false;
                                    bWithoutOOC = true;
                                    bAnyOOC = false;
                                }
                                ImGui.SameLine();
                                if ( ImGui.RadioButton( "any##MarkOOCRadioButton", bAnyOOC ) )
                                {
                                    bWithOOC = false;
                                    bWithoutOOC = false;
                                    bAnyOOC = true;
                                }
                                ImGui.Unindent();

                                // Convert the booleans back to flags
                                this._newMarker.DisplayMode = 0;
                                this._newMarker.DisplayMode |= bSingleChunk ? DisplayMode.WithSingleChunk : 0;
                                this._newMarker.DisplayMode |= bMultiChunk ? DisplayMode.WithMultipleChunks : 0;
                                this._newMarker.DisplayMode |= bAnyChunk ? DisplayMode.WithAnyChunkCount : 0;

                                this._newMarker.DisplayMode |= bWithOOC ? DisplayMode.WithOOC : 0;
                                this._newMarker.DisplayMode |= bWithoutOOC ? DisplayMode.WithoutOOC : 0;
                                this._newMarker.DisplayMode |= bAnyOOC ? DisplayMode.AnyOOC : 0;
                                #endregion

                                if ( ImGui.Button( "Add", new( -1, Wordsmith.BUTTON_Y.Scale() ) ) )
                                {
                                    if ( this._newMarker.DisplayMode == 0 )
                                    {
                                        this.IsOpen = false;
                                        WordsmithUI.ShowMessageBox(
                                            "Invalid Settings",
                                            "Unable to add marker.\nMarkers must have at least one display option selected",
                                            MessageBox.ButtonStyle.Ok,
                                            ( o ) => { this.IsOpen = true; } 
                                            );
                                    }
                                    else
                                    {
                                        this._chunkMarkers.Add( this._newMarker );
                                        this._chunkMarkers = ChunkMarker.SortList( this._chunkMarkers );
                                        this._newMarker = new();
                                    }
                                }
                                ImGui.Unindent();
                            }
                            else
                                this._newMarkerHeaderOpen = false;
                        }
                        ImGui.EndChild();
                    }
                    ImGui.Unindent();
                }
                ImGui.Spacing();

                if ( ImGui.CollapsingHeader( "Text Input Options##settingsheader" ) )
                {
                    ImGui.Indent();
                    // Max Text Length
                    float dragWidth = ImGui.GetContentRegionAvail().X - (125 * ImGuiHelpers.GlobalScale);
                    ImGui.SetNextItemWidth( dragWidth );
                    ImGui.DragInt( "Max Text Length##ScratchPadSettingsSlider", ref this._scratchMaxTextLen, 8f, 512, Wordsmith.MAX_SCRATCH_LENGTH );
                    if ( this._scratchMaxTextLen > Wordsmith.MAX_SCRATCH_LENGTH )
                        this._scratchMaxTextLen = Wordsmith.MAX_SCRATCH_LENGTH;

                    ImGuiExt.SetHoveredTooltip( $"This is the buffer size for text input.\nThe higher this value is the more\nmemory consumed up to a maximum of\n{Wordsmith.MAX_SCRATCH_LENGTH / 1024}KB per Scratch Pad." );

                    ImGui.Separator();
                    ImGui.SetNextItemWidth( dragWidth );
                    ImGui.DragInt( "Input Height##ScratchPadInputLineHeight", ref this._scratchInputLineHeight, 0.1f, 3, 25 );
                    ImGuiExt.SetHoveredTooltip( "This is the maximum height of the text input (in lines).\nThe text input will grow up to the maximum size as\nlong as there is room for it to do so." );

                    ImGui.Unindent();
                    ImGui.Separator();
                }
                ImGui.Spacing();

                if ( ImGui.CollapsingHeader( "Open Scratch Pads##settingsheader" ) )
                {
                    ImGui.Indent();
                    float size_y = ImGui.GetContentRegionAvail().Y - Wordsmith.BUTTON_Y.Scale() - this._style.FramePadding.Y*2;
                    if ( ImGui.BeginChild( "OpenPadsChildObject", new(-1, size_y > Wordsmith.BUTTON_Y.Scale() *2 ? size_y : Wordsmith.BUTTON_Y.Scale() *2 ) ))
                    {
                        if ( ImGui.BeginTable( $"SettingsPadListTable", 4 ) )
                        {
                            ImGui.TableSetupColumn( "SettingsUIPadListIDColumn", ImGuiTableColumnFlags.WidthFixed, Wordsmith.BUTTON_Y.Scale() );
                            ImGui.TableSetupColumn( "SettingsUIPadListDescColumn", ImGuiTableColumnFlags.WidthStretch, 1 );
                            ImGui.TableSetupColumn( "SettingsUIPadListShowColumn", ImGuiTableColumnFlags.WidthFixed, 75 * ImGuiHelpers.GlobalScale );
                            ImGui.TableSetupColumn( "SettingsUIPadListCloseColumn", ImGuiTableColumnFlags.WidthFixed, 75 * ImGuiHelpers.GlobalScale );

                            ImGui.TableNextColumn();
                            ImGui.TableHeader( "ID##SettingsUIPadListColumnHeader" );
                            ImGui.TableNextColumn();
                            ImGui.TableHeader( "Description##SettingsUIPadListColumnHeader." );
                            ImGui.TableNextColumn();
                            ImGui.TableHeader( "##ShowSettingsUIPadListColumnHeader" );
                            ImGui.TableNextColumn();
                            ImGui.TableHeader( "##CloseSettingsUIPadListColumnHeader" );

                            // Create a list of Scratch Pads
                            List<ScratchPadUI> scratchpads = new();
                            foreach ( Window w in WordsmithUI.Windows )
                                if ( w is ScratchPadUI pad )
                                    scratchpads.Add( pad );

                            scratchpads = scratchpads.OrderBy( pad => pad.ID ).ToList();

                            foreach ( ScratchPadUI pad in scratchpads )
                            {

                                ImGui.TableNextColumn();
                                ImGui.Text( pad.ID.ToString() );

                                ImGui.TableNextColumn();
                                if ( pad.Title.Length > 0 )
                                    ImGui.Text( pad.Title );
                                else
                                    ImGui.Text( pad.Header.Length > 0 ? pad.Header.ToString() : "None" );

                                ImGui.TableNextColumn();
                                if ( !pad.IsOpen )
                                {
                                    if ( ImGui.Button( $"Show##SettingsUIPadListOpen{pad.ID}", new Vector2( -1, Wordsmith.BUTTON_Y.Scale() ) ) )
                                        pad.IsOpen = true;
                                }
                                else
                                {
                                    if ( ImGui.Button( $"Hide##SettingsUIPadListHIde{pad.ID}", new Vector2( -1, Wordsmith.BUTTON_Y.Scale() ) ) )
                                        pad.Hide();
                                }

                                ImGui.TableNextColumn();

                                if ( ImGui.Button( $"Close##SettingsUIPadListClose{pad.ID}", new( -1, Wordsmith.BUTTON_Y.Scale() ) ) )
                                {
                                    try
                                    {
                                        if ( this._confirmDeleteClosed )
                                        {
                                            WordsmithUI.ShowMessageBox(
                                                "Confirm Delete",
                                                $"Are you sure you want to delete Scratch Pad {pad.ID}?",
                                                MessageBox.ButtonStyle.OkCancel, 
                                                ( mb ) =>
                                                {
                                                    if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                                                        WordsmithUI.RemoveWindow( pad );
                                                });
                                        }
                                        else
                                            WordsmithUI.RemoveWindow( pad );
                                    }
                                    catch ( Exception e )
                                    {
                                        OnException( e );
                                    }
                                }
                            }
                            ImGui.EndTable();
                        }
                    }
                    ImGui.EndChild();
                    if (ImGui.Button( "Close All", new( -1, Wordsmith.BUTTON_Y.Scale() ) ))
                        foreach (Window w in WordsmithUI.Windows)
                        {
                            if ( w is ScratchPadUI pad)
                                WordsmithUI.RemoveWindow( pad );
                        }
                    ImGui.Unindent();
                 }
                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawAliasesTab()
    {
        if ( ImGui.BeginTabItem( "Aliases##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "AliasSettingsChild", new( -1, GetCanvasSize() ) ) )
            {
                //
                // Aliases
                //
                ImGui.TextWrapped( $"Aliases are like nicknames for chat commands. For example if you set the alias \"me\" for Emote you can type /me instead of /em and the correct chat channel will be parsed." );
                int spacing = 10;
                int tellBarWidth = 200;

                if ( ImGui.BeginTable( "AliasesTabTable", 3, ImGuiTableFlags.BordersH ) )
                {
                    ImGui.TableSetupColumn( "AliasChatTypeHeaderColumn", ImGuiTableColumnFlags.WidthFixed, 150*ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "AliasTextInputColumn", ImGuiTableColumnFlags.WidthStretch );
                    ImGui.TableSetupColumn( "AliasAddDeleteColumn", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 30*ImGuiHelpers.GlobalScale );

                    // Get the chat type options.
                    List<string> options = new(Enum.GetNames(typeof(ChatType)));

                    options[0] = "Please Choose";
                    options.Remove( "Linkshell" );
                    options.Remove( "CrossWorldLinkshell" );

                    for ( int toggle = 0; toggle < 2; ++toggle )
                        for ( int i = 1; i <= 8; ++i )
                            options.Add( $"{(toggle == 1 ? "CW-" : "")}Linkshell{i}" );

                    #region Existing Aliases
                    for ( int i = 0; i < this._headerAliases.Count; ++i)
                    {
                        (int ChatType, string Alias, object? Data) alias = this._headerAliases[i];
                        string chatTypeName;

                        if ( alias.ChatType < (int)ChatType.Linkshell )
                            chatTypeName = options[alias.ChatType];
                        else
                        {
                            // Get the linkshell type.
                            chatTypeName = alias.ChatType == (int)ChatType.Linkshell ? "Linkshell" : "CW-Linkshell";

                            // Get the linkshell channel.
                            if ( alias.Data is int idata )
                                chatTypeName += $"{idata + 1}";
                            else if ( alias.Data is long ldata )
                                chatTypeName += $"{ldata + 1}";
                        }

                        // Draw the chat type.
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth( -1 );
                        ImGui.Spacing();
                        ImGui.Text( $"{chatTypeName}" );
                        ImGuiExt.SetHoveredTooltip( "Chat header to use alias on." );

                        string output = alias.Alias;
                        // Add the input field.
                        ImGui.TableNextColumn();

                        bool update = false;
                        ImGui.Spacing();
                        if ( chatTypeName == "Tell" && alias.Data is not null)
                        {
                            // Get the data as a string.
                            string data = alias.Data as string ?? "";

                            // Set the width of the target entry.
                            ImGui.SetNextItemWidth( tellBarWidth * ImGuiHelpers.GlobalScale );
                            if (ImGui.InputTextWithHint( $"##{chatTypeName}Alias{alias}TargetTextEdit", $"User Name@World", ref data, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                                update = data != (alias.Data as string) && data.GetTarget() is not null;
                            ImGuiExt.SetHoveredTooltip( "Edt the target of your tell. Hit the enter key after making changes to update.\nYou must still click Apply to save the changes." );

                            ImGui.SameLine( 0, 0 );
                            ImGui.Spacing();
                            // Same line.
                            ImGui.SameLine( 0, spacing * ImGuiHelpers.GlobalScale );

                            // Draw the alias input. If the user hits enter, set update flag if alias has changed.
                            // Get the size of the column.
                            float size = ImGui.GetColumnWidth();
                            ImGui.SetNextItemWidth( size );
                        }
                        else
                            ImGui.SetNextItemWidth( -1 );

                        update |= ImGui.InputText( $"##{chatTypeName}Alias{alias}Input", ref output, 128, ImGuiInputTextFlags.EnterReturnsTrue );
                        ImGuiExt.SetHoveredTooltip( "Edit the alias here. Hit the enter key after making changes to update.\nYou must still click Apply to save the changes." );

                        // If update flag is enabled then update the item.
                        if ( update )
                            this._headerAliases[i] = new( alias.ChatType, output.ToLower().Trim( '\'', '/', ' ', '\r', '\n' ), alias.Data );

                        ImGui.TableNextColumn();
                        if ( ImGui.Button( $"X##{chatTypeName}Alias{alias}", ImGuiHelpers.ScaledVector2( Wordsmith.BUTTON_Y, Wordsmith.BUTTON_Y ) ))
                            this._headerAliases.RemoveAt( i-- );
                    }
                    #endregion

                    #region New Alias
                    // Display the chat type selection.
                    ImGui.TableNextColumn();
                    ImGui.Spacing();
                    ImGui.SetNextItemWidth( -1 );
                    ImGui.Combo( "##NewAliasChatTypeSelection", ref this._newAliasSelection, options.ToArray(), options.Count );
                    ImGuiExt.SetHoveredTooltip( "Chat header to use alias on." );
                    // Show an input field for the new alias.
                    ImGui.TableNextColumn();
                    ImGui.Spacing();

                    if ( options[this._newAliasSelection] == "Tell" )
                    {
                        // Set the size of the tell target.
                        ImGui.SetNextItemWidth( tellBarWidth * ImGuiHelpers.GlobalScale );

                        // Display the target entry.
                        ImGui.InputTextWithHint( $"##NewAliasTargetTextInput", $"User Name@World", ref this._newAliasTarget, 128 );
                        ImGuiExt.SetHoveredTooltip( "This is the target of your tell. i.e. \"<t>\" or \"User Name@World\"" );

                        // Insert the spacing.
                        ImGui.SameLine( 0, spacing * ImGuiHelpers.GlobalScale );

                        // Get the size of alias text area.
                        float size = ImGui.GetColumnWidth();
                        ImGui.SetNextItemWidth( size );
                    }

                    else
                        ImGui.SetNextItemWidth( -1 );

                    bool add = ImGui.InputTextWithHint( $"##NewAliasTextInput", $"Enter alias here without /.", ref this._newAlias, 128, ImGuiInputTextFlags.EnterReturnsTrue );
                    ImGuiExt.SetHoveredTooltip( "Enter the desired alias here without the \"/\" character" );

                    // Determine if the add button should be enabled or not
                    // It should be enable dif there is a type choice made and an alias
                    // given except in the case of a tell.
                    bool bEnableAddAlias = false;
                    if ( this._newAliasSelection > 0 && this._newAlias.Length > 0)
                    {
                        if ( this._newAliasSelection == (int)ChatType.Tell && !this._newAliasTarget.isTarget() )
                            bEnableAddAlias = false;
                        else
                            bEnableAddAlias = true;
                    }
                    ImGui.TableNextColumn();
                    ImGui.Spacing();

                    // Put the + button. Disable until valid data is entered.
                    if ( !bEnableAddAlias )
                        ImGui.BeginDisabled();
                    add |= ImGui.Button( "+##NewAliasAddButton", ImGuiHelpers.ScaledVector2( Wordsmith.BUTTON_Y, Wordsmith.BUTTON_Y ) );
                    if ( !bEnableAddAlias )
                        ImGui.EndDisabled();

                    if ( add && this._newAliasSelection > 0 && bEnableAddAlias)
                    {
                        // Create a flag to check validity.
                        bool valid = true;

                        // Check if the alias is already in use.
                        foreach ( (int i, string s, object? o) in this._headerAliases )
                        {
                            // If it is, flag it as invalid
                            if ( this._newAlias == s )
                            {
                                valid = false;
                                break;
                            }
                        }

                        // Check if the alias is used by a default option.
                        for( int i = 0; i < Enum.GetNames(typeof(ChatType)).Length; ++i )
                        {
                            ChatType ct = (ChatType)i;
                            if ( $"/{this._newAlias.ToLower()}" == ct.GetShortHeader() || $"/{this._newAlias.ToLower()}" == ct.GetLongHeader() )
                            {
                                valid = false;
                                break;
                            }
                        }

                        // If the user did not enter a valid target then flag as invalid.
                        if ( options[this._newAliasSelection] == "Tell" && !this._newAliasTarget.isTarget() )
                            valid = false;

                        // If the alias doesn't exist and the data is valid then add it.
                        if ( valid )
                        {
                            int cType = this._newAliasSelection;
                            string sAlias = this._newAlias.ToLower().Trim( '\'', '/', ' ', '\r', '\n' );
                            object? oData = this._newAliasTarget.Length > 0 ? this._newAliasTarget : null;

                            // Try to match the data with linkshell data.
                            Match m = Regex.Match(options[this._newAliasSelection], "(CW-)?[Ll]inkshell(\\d)");
                            if ( m.Success )
                            {
                                cType = m.Groups[1].Success ? (int)ChatType.CrossWorldLinkshell : (int)ChatType.Linkshell;
                                oData = int.Parse( m.Groups[2].Value )-1;
                            }
                            this._headerAliases.Add( new( cType, sAlias, oData ) );
                            this._newAliasSelection = 0;
                            this._newAlias = "";
                            this._newAliasTarget = "";
                        }
                    }
                    ImGui.Spacing();
                    #endregion

                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawSpellCheckTab()
    {
        if (ImGui.BeginTabItem("Spell Check##SettingsUITabItem"))
        {
            if (ImGui.BeginChild("DictionarySettingsChild", new(-1, GetCanvasSize() ) ))
            {
                ImGui.Checkbox( "Auto-Spell Check", ref this._autospellcheck );
                ImGuiExt.SetHoveredTooltip( "When enabled, spell check will automatically run after a pause in typing is detected." );
                ImGui.SameLine();

                // Ignore Hyphen terminated words.
                ImGui.Checkbox("Ignore Hyphen-Terminated Words##SettingsUICheckbox", ref this._ignoreHypen);
                ImGuiExt.SetHoveredTooltip( "This is useful in roleplay for emulating cut speech.\ni.e. \"How dare yo-,\" she was cut off by the rude man." );
                ImGui.SameLine();

                // Auto-Fix Spaces
                ImGui.Checkbox("Fix Spacing.", ref this._fixDoubleSpace);
                ImGuiExt.SetHoveredTooltip( "When enabled, Scratch Pads will programmatically remove extra\nspaces from your text for you." );
                ImGui.Separator();

                // Get half the width
                float bar_width = ImGui.GetWindowContentRegionMax().X / 2.0f;
                ImGui.SetNextItemWidth( bar_width - 170 * ImGuiHelpers.GlobalScale );
                ImGui.DragInt( "Maximum Suggestions", ref this._maxSuggestions, 0.1f, 0, 100 );
                ImGuiExt.SetHoveredTooltip( "The number of spelling suggestions to return with spell checking. 0 is unlimited results." );
                ImGui.SameLine();

                ImGui.SetNextItemWidth( bar_width - 160 * ImGuiHelpers.GlobalScale );
                ImGui.DragFloat( "Auto-Spellcheck Delay (Seconds)", ref this._autospellcheckdelay, 0.1f, 0.1f, 100f );
                ImGuiExt.SetHoveredTooltip( "The time in seconds to wait after typing stops to spell check." );
                ImGui.Separator();

                // Dictionaries

                List<string> dictionaries = new();

                // Add the dictionaries from the web manifest.
                foreach ( string s in Wordsmith.WebManifest.Dictionaries )
                    dictionaries.Add( $"web: {s}" );

                // Add all local dictionaries.
                if ( Directory.Exists( Path.Combine( Wordsmith.PluginInterface.AssemblyLocation.Directory?.FullName!, "Dictionaries" ) ) )
                    foreach ( string s in Directory.GetFiles( Path.Combine( Wordsmith.PluginInterface.AssemblyLocation.Directory?.FullName!, "Dictionaries" ) ) )
                        dictionaries.Add( $"local: {Path.GetFileName( s )}" );

                if ( ImGui.Button( "Reload Dictionary##ReinitLangButton" ) )
                    Lang.Reinit();

                ImGuiExt.SetHoveredTooltip( $"Reload the dictionary file and custom dictionary, including any changes." );

                // If no files are returned
                if (dictionaries.Count == 0)
                {
                    // Alert the user to the missing dictionaries.
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "ERROR.");
                    ImGui.TextWrapped($"There are no dictionary files in the manifest or {{{Path.Combine(Wordsmith.PluginInterface.AssemblyLocation.Directory?.FullName!, "Dictionaries")}}}.");
                }
                else
                {
                    ImGui.SameLine();

                    // Get the index of the current dictionary file if it exists.
                    int selection = dictionaries.IndexOf(this._dictionaryFilename);

                    // If the file isn't found, default to option 0.
                    if (selection < 0)
                        selection = 0;

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X - 380*ImGuiHelpers.GlobalScale);

                    // Display a combo with all of the available dictionaries.
                    ImGui.Combo("Dictionary Selection", ref selection, dictionaries.ToArray(), dictionaries.Count);
                    ImGuiExt.SetHoveredTooltip( $"This is the file to be used for the dictionary. To use a custom spell check\ndictionary it must be inside the plug-in's Dictionary folder." );
                    

                    // If the selection is different from the previous dictionary then
                    // update the filename.
                    if (selection != dictionaries.IndexOf(this._dictionaryFilename))
                        this._dictionaryFilename = dictionaries[selection];
                }
                if ( Wordsmith.Configuration.ShowAdvancedSettings )
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth( ImGui.GetContentRegionMax().X - this._style.WindowPadding.X - ImGui.CalcTextSize("Cleaning String").X );
                    ImGui.InputText( $"Cleaning String", ref this._punctuationCleaningString, 1024 );
                    ImGuiExt.SetHoveredTooltip( $"This is the complete list of punctuation to be cleaned from the start/end of the word when checking for spelling errors.\nWARNING: Altering this can cause undesired behavior." );
                }
                ImGui.Separator();

                // Custom Dictionary Table
                if (ImGui.BeginTable($"CustomDictionaryEntriesTable", 2, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn("CustomDictionaryWordColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn("CustomDictionaryDeleteColumn", ImGuiTableColumnFlags.WidthFixed, 65 * ImGuiHelpers.GlobalScale);

                    ImGui.TableNextColumn();
                    ImGui.TableHeader( "Custom Dictionary Entries" );

                    // Delete all
                    ImGui.TableNextColumn();
                    if (ImGui.Button("Delete All##DeleteAllDictionaryEntriesButton", new(-1, Wordsmith.BUTTON_Y.Scale() ) ))
                        WordsmithUI.ShowMessageBox(
                            $"{Wordsmith.APPNAME} - Reset Dictionary",
                            "This will delete all entries that you added to the\ndictionary.This cannot be undone.\nProceed?",
                            MessageBox.ButtonStyle.OkCancel,
                            ( mb ) => {
                                if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                                {
                                    Wordsmith.Configuration.CustomDictionaryEntries = new();
                                    Wordsmith.Configuration.Save();
                                }
                            } );
                    ImGuiExt.SetHoveredTooltip( $"Deletes all dictionary entries. This action cannot be undone." );

                    // display each entry.
                    string sRemoveEntry = "";
                    for (int i = 0; i < Wordsmith.Configuration.CustomDictionaryEntries.Count; ++i)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(Wordsmith.Configuration.CustomDictionaryEntries[i]);

                        ImGui.TableNextColumn();
                        if ( ImGui.Button( $"Delete##CustomDictionaryDelete{i}Buttom", new( -1, Wordsmith.BUTTON_Y.Scale() ) ) )
                            sRemoveEntry = Wordsmith.Configuration.CustomDictionaryEntries[i]; //Lang.RemoveDictionaryEntry( Wordsmith.Configuration.CustomDictionaryEntries[i] );

                        ImGuiExt.SetHoveredTooltip($"Permanently deletes {Wordsmith.Configuration.CustomDictionaryEntries[i]} from your custom dictionary.");
                    }
                    if ( sRemoveEntry.Length > 0 )
                    {
                        Lang.RemoveDictionaryEntry( sRemoveEntry );
                        sRemoveEntry = "";
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawLinkshellTab()
    {
        if (ImGui.BeginTabItem("Linkshells##SettingsUITabItem"))
        {
            if (ImGui.BeginChild("LinkshellsSettingsChildFrame", new(-1, GetCanvasSize() ) ))
            {
                ImGui.Text("Linkshell Names");
                
                if (ImGui.BeginTable("LinkshellsNamesTable", 3, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn("LinkshellRowHeaderColumn", ImGuiTableColumnFlags.WidthFixed, 10 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("LinkshellNameColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                    ImGui.TableSetupColumn("CrossworldLinkshellNameColumn", ImGuiTableColumnFlags.WidthStretch, 1);

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("#");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Linkshell");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Cross-World");

                    // For each linkshell, create an id | custom name row.
                    for(int i = 0; i<8; ++i)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{i + 1}");

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputText($"##SettingsLinkshellName{i}", ref this._linkshells[i], 32);

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputText($"##SettingsCWLinkshellName{i}", ref this._cwlinkshells[i], 32);
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawColorsTab()
    {
        if ( ImGui.BeginTabItem( "Colors##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "ColorsSettingsChildFrame", new( -1, GetCanvasSize() ) ) )
            {
                ImGui.Checkbox( "Enable Text Colorization##SettingsUICheckbox", ref this._enableTextColor );
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                bool spellingErrorColorPopup = false;
                if ( this._enableTextColor )
                    spellingErrorColorPopup = ImGui.ColorButton( "##ColorPreviewButton", this._spellingErrorColor );
                else
                    ImGui.ColorButton( "##ColorPreviewButtonDisabled", new( 0.2f, 0.2f, 0.2f, 0.5f ), ImGuiColorEditFlags.NoTooltip );
                if ( spellingErrorColorPopup )
                {
                    ImGui.OpenPopup( "SettingsUIErrorHighlightingColorPickerPopup" );
                    this._backupColor = this._spellingErrorColor;
                }
                if ( ImGui.BeginPopup( "SettingsUIErrorHighlightingColorPickerPopup" ) )
                {
                    if ( ImGui.ColorPicker4( "##SettingsUIErrorHighlightingPicker", ref this._backupColor ) )
                        this._spellingErrorColor = this._backupColor;

                    ImGui.EndPopup();
                }
                ImGui.SameLine( 0, 5 * ImGuiHelpers.GlobalScale );
                ImGui.Text( $"Spelling Error Color" );
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.Text( "Chat Header Colors" );

                if (ImGui.BeginTable("ColorSettingsTable", 10))
                {
                    ImGui.TableSetupColumn( "LeftColorOuterColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "LeftColorOuterChatColumn" );
                    ImGui.TableSetupColumn( "LeftColorInnerColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "LeftColorInnerChatColumn" );
                    ImGui.TableSetupColumn( "CenterColorColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "CenterColorChatColumn" );
                    ImGui.TableSetupColumn( "RightColorInnerColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "RightColorInnerChatColumn" );
                    ImGui.TableSetupColumn( "RightColorOuterColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "RightColorOuterChatColumn" );

                    string[] options = Enum.GetNames(typeof(ChatType));
                    for ( int i = 0; i < options.Length-1; ++i )
                    {
                        if ( !this._headerColors.ContainsKey( i ) )
                            continue;

                        ImGui.TableNextColumn();

                        bool highlightColorPopup = false;
                        if ( this._enableTextColor )
                            highlightColorPopup = ImGui.ColorButton( $"##ColorPreviewButton{options[i]}", this._headerColors[i] );
                        else
                            ImGui.ColorButton( $"##ColorPreviewButtonDisabled{options[i]}", new( 0.2f, 0.2f, 0.2f, 0.5f ), ImGuiColorEditFlags.NoTooltip );

                        if ( highlightColorPopup )
                        {
                            ImGui.OpenPopup( $"SettingsUIHighlightingColorPickerPopup{options[i]}" );
                            this._backupColor = this._headerColors[i];
                        }
                        if ( ImGui.BeginPopup( $"SettingsUIHighlightingColorPickerPopup{options[i]}" ) )
                        {

                            if ( ImGui.ColorPicker4( "##SettingsUIErrorHighlightingPicker", ref this._backupColor ) )
                                this._headerColors[i] = this._backupColor;

                            ImGui.EndPopup();
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text( $"{options[i]}" );
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawFooter()
    {
        if (ImGui.BeginTable("SettingsUISaveCloseCancelButtonTable", 6))
        {
            ImGui.TableSetupColumn( "SettingsUIFoundBugColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUIKoFiButtonColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUITableSpacerColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn( "SettingsUISaveAndCloseButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUIDefaultsButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUICancelButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale );



            // Leave the first column blank for spacing.
            ImGui.TableNextColumn();
            if ( ImGui.Button( $"Found A Bug?", new(-1, Wordsmith.BUTTON_Y.Scale() ) ))
            {
                WordsmithUI.ShowMessageBox( "Found a bug?", "If you found a bug, please post as much useful information as possible.\nThe more you are able to share with me the faster I can find the problem and fix it.\nUseful information could be:\n\t* Screenshots\n\t* Description of what you were doing\n\t* Number of pads open\n\t* Dalamud.log file\n\t* Wordsmith.json config file\n\nGo to GitHub to report the bug?", MessageBox.ButtonStyle.YesNo, (m) =>
                {
                    if ( m.Result == MessageBox.DialogResult.Yes )
                        System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo( "https://github.com/MythicPalette/Wordsmith-DalamudPlugin/issues" ) { UseShellExecute = true } );
                } );                
            }

            ImGui.TableNextColumn();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0, 0, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.3f, 0.3f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.3f, 0.1f, 0.1f, 1f));

            // Create the donation button. The link to my kofi is stored in the web manifest in case it ever changes.
            if (ImGui.Button("Buy Me A Ko-Fi##SettingsUIBuyAKoFiButton", new(-1, Wordsmith.BUTTON_Y.Scale() ) ))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Wordsmith.WebManifest.Kofi) { UseShellExecute = true });
            ImGuiExt.SetHoveredTooltip( $"This is a donation/tip button. This is absolutely not required at all.\nWhile I work hard to make Wordsmith the best I can, I do so out of passion\nand not for money. That being said, if you would like to support me then\nthank you so, so much. It's super appreciated." );
            ImGui.PopStyleColor(3);

            //Skip the next column.
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            // Save and close buttons
            if (ImGui.Button("Apply", new( -1, Wordsmith.BUTTON_Y.Scale() ) ))
                Save();

            ImGui.TableNextColumn();
            // Reset settings to default.
            if (ImGui.Button("Defaults", new( -1, Wordsmith.BUTTON_Y.Scale() ) ))
            {
                WordsmithUI.ShowMessageBox( $"{Wordsmith.APPNAME} - Restore Default Settings",
                    "Restoring defaults resets all settings to their original values\n(not including words added to your dictionary).\nProceed?",
                    buttonStyle: MessageBox.ButtonStyle.OkCancel,
                    ( mb ) =>
                    {
                        if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                            Wordsmith.ResetConfig();

                        this.IsOpen= true;
                    });
                this.IsOpen = false;
            }

            ImGui.TableNextColumn();
            // Cancel button
            if (ImGui.Button("Close", new( -1, Wordsmith.BUTTON_Y.Scale() ) ))
                this.IsOpen = false;

            ImGui.EndTable();
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        ResetValues();
    }

    public void OnException(Exception e)
    {
        Wordsmith.PluginLog.Error( e.ToString() );
        this.IsOpen = false;
        Dictionary<string, object> dump = this.Dump();
        dump["Exception"] = new Dictionary<string, object>()
                                        {
                                            { "Error", e.ToString() },
                                            { "Message", e.Message }
                                        };
        dump["Window"] = "SettingsUI";
        WordsmithUI.ShowErrorWindow( dump );
    }
    
    private void ResetValues()
    {
        // General settings.
        this._showAdvancedSettings = Wordsmith.Configuration.ShowAdvancedSettings;
        this._neverShowNotices = Wordsmith.Configuration.NeverShowNotices;
        this._lastSeenNotice = Wordsmith.Configuration.LastNoticeRead;
        this._searchHistoryCountChange = Wordsmith.Configuration.SearchHistoryCount;
        this._researchToTopChange = Wordsmith.Configuration.ResearchToTop;
        this._trackWordStats = Wordsmith.Configuration.TrackWordStatistics;

        // Scratch Pad settings.
        this._autoClear = Wordsmith.Configuration.AutomaticallyClearAfterLastCopy;
        this._deleteClosed = Wordsmith.Configuration.DeleteClosedScratchPads;
        this._confirmDeleteClosed = Wordsmith.Configuration.ConfirmDeleteClosePads;
        this._ignoreHypen = Wordsmith.Configuration.IgnoreWordsEndingInHyphen;
        this._showChunks = Wordsmith.Configuration.ShowTextInChunks;
        this._onSentence = Wordsmith.Configuration.SplitTextOnSentence;
        this._detectHeader = Wordsmith.Configuration.ParseHeaderInput;
        this._oocOpening = Wordsmith.Configuration.OocOpeningTag;
        this._oocClosing = Wordsmith.Configuration.OocClosingTag;
        this._oocByDefault = Wordsmith.Configuration.OocByDefault;
        this._sentenceTerminators = Wordsmith.Configuration.SentenceTerminators;
        this._encapTerminators = Wordsmith.Configuration.EncapsulationTerminators;
        this._chunkMarkers = Wordsmith.Configuration.ChunkMarkers;
        this._continuationMarker = Wordsmith.Configuration.ContinuationMarker;
        this._markLastChunk = Wordsmith.Configuration.ContinuationMarkerOnLast;
        this._scratchMaxTextLen = Wordsmith.Configuration.ScratchPadMaximumTextLength;
        this._scratchInputLineHeight = Wordsmith.Configuration.ScratchPadInputLineHeight;

        // Alias Settings
        this._headerAliases = new(Wordsmith.Configuration.HeaderAliases);

        // Spell Check Settings
        this._fixDoubleSpace = Wordsmith.Configuration.ReplaceDoubleSpaces;
        this._dictionaryFilename = Wordsmith.Configuration.DictionaryFile;
        this._maxSuggestions = Wordsmith.Configuration.MaximumSuggestions;
        this._autospellcheck = Wordsmith.Configuration.AutoSpellCheck;
        this._autospellcheckdelay = Wordsmith.Configuration.AutoSpellCheckDelay;
        this._punctuationCleaningString = Wordsmith.Configuration.PunctuationCleaningList;

        // Linkshell Settings
        this._linkshells = Wordsmith.Configuration.LinkshellNames;
        this._cwlinkshells = Wordsmith.Configuration.CrossWorldLinkshellNames;

        // Color Settings
        this._enableTextColor = Wordsmith.Configuration.EnableTextHighlighting;
        this._spellingErrorColor = Wordsmith.Configuration.SpellingErrorHighlightColor;
        this._headerColors = Wordsmith.Configuration.HeaderColors.Clone();
}

    private void Save()
    {
        // General Settings.
        Wordsmith.Configuration.ShowAdvancedSettings = this._showAdvancedSettings;
        Wordsmith.Configuration.NeverShowNotices = this._neverShowNotices;
        Wordsmith.Configuration.LastNoticeRead = this._lastSeenNotice;
        Wordsmith.Configuration.SearchHistoryCount = this._searchHistoryCountChange;
        Wordsmith.Configuration.ResearchToTop = this._researchToTopChange;
        Wordsmith.Configuration.TrackWordStatistics = this._trackWordStats;

        // Scratch Pad settings.
        Wordsmith.Configuration.AutomaticallyClearAfterLastCopy = _autoClear;
        Wordsmith.Configuration.DeleteClosedScratchPads = this._deleteClosed;
        Wordsmith.Configuration.ConfirmDeleteClosePads = this._confirmDeleteClosed;
        Wordsmith.Configuration.IgnoreWordsEndingInHyphen = this._ignoreHypen;
        Wordsmith.Configuration.ShowTextInChunks = this._showChunks;
        Wordsmith.Configuration.SplitTextOnSentence = this._onSentence;
        Wordsmith.Configuration.ParseHeaderInput = this._detectHeader;
        Wordsmith.Configuration.OocOpeningTag = this._oocOpening;
        Wordsmith.Configuration.OocClosingTag = this._oocClosing;
        Wordsmith.Configuration.OocByDefault = this._oocByDefault;
        Wordsmith.Configuration.SentenceTerminators = this._sentenceTerminators;
        Wordsmith.Configuration.EncapsulationTerminators = this._encapTerminators;
        Wordsmith.Configuration.ChunkMarkers = this._chunkMarkers;
        Wordsmith.Configuration.ContinuationMarker = this._continuationMarker;
        Wordsmith.Configuration.ContinuationMarkerOnLast = this._markLastChunk;
        Wordsmith.Configuration.ScratchPadMaximumTextLength = this._scratchMaxTextLen;
        Wordsmith.Configuration.ScratchPadInputLineHeight = this._scratchInputLineHeight;

        // Alias Settings
        Wordsmith.Configuration.HeaderAliases = this._headerAliases;

        // Spell Check settings.
        Wordsmith.Configuration.ReplaceDoubleSpaces = this._fixDoubleSpace;
        Wordsmith.Configuration.AutoSpellCheck = this._autospellcheck;
        Wordsmith.Configuration.MaximumSuggestions = this._maxSuggestions;
        Wordsmith.Configuration.AutoSpellCheckDelay = this._autospellcheckdelay;
        Wordsmith.Configuration.PunctuationCleaningList = this._punctuationCleaningString;

        if (this._dictionaryFilename != Wordsmith.Configuration.DictionaryFile)
        {
            Wordsmith.Configuration.DictionaryFile = this._dictionaryFilename;
            Lang.Reinit();
        }


        // Linkshell settings
        Wordsmith.Configuration.LinkshellNames = this._linkshells;
        Wordsmith.Configuration.CrossWorldLinkshellNames = this._cwlinkshells;

        // Color settings
        Wordsmith.Configuration.EnableTextHighlighting = this._enableTextColor;
        Wordsmith.Configuration.SpellingErrorHighlightColor = this._spellingErrorColor;
        Wordsmith.Configuration.HeaderColors = this._headerColors;

        // Save the configuration
        Wordsmith.Configuration.Save();
    }
}
