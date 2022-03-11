using Dalamud.Configuration;

namespace Wordsmith;

[Serializable]
public class Configuration : IPluginConfiguration
{
    [NonSerialized]
    internal bool RecentlySaved = false;

    public int Version { get; set; } = 0;

    public int SearchHistoryCount { get; set; } = 10;
    public bool ResearchToTop { get; set; } = true;

    public float FontSize { get; set; } = 17f;
    public float JpFontSize { get; set; } = 17f;
    public float SymbolFontSize { get; set; } = 17f;

    // Scratch Pad settings
    public bool DeleteClosedScratchPads { get; set; } = true;

    /// <summary>
    /// When true, a context menu item is added to context menus that contain 
    /// the "Send Tell" command.
    /// </summary>
    public bool AddContextMenuOption { get; set; } = true;

    /// <summary>
    /// If true, the spellchecker will not attempt to match words ending in a hyphen.
    /// This is because people often write a hyphen to indicate their sentence being
    /// cut off (i.e. "How dare yo-").
    /// </summary>
    public bool IgnoreWordsEndingInHyphen { get; set; } = true;

    /// <summary>
    /// If enabled, uses a custom label layout to display highlighted text.
    /// </summary>
    public bool EnableTextHighlighting { get; set; } = true;

    /// <summary>
    /// The spell checker will attempt to delete these punctuation marks from the beginning and end of every word
    /// </summary>
    public string PunctuationCleaningList { get; set; } = ",.'*\"-(){}[]!?<>`~♥@#$%^&*_=+\\/←→↑↓《》■※☀★★☆♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～";

    /// <summary>
    /// Toggles displaying text in copy chunks.
    /// </summary>
    public bool ShowTextInChunks { get; set; } = true;

    /// <summary>
    /// Attempts to break text chunks at the nearest sentence rather than the nearest space.
    /// </summary>
    public bool BreakOnSentence { get; set; } = true;

    /// <summary>
    /// The symbols to consider the end of a sentence.
    /// </summary>
    public string SplitPointDefinitions { get; set; } = ".?!";

    /// <summary>
    /// The symbols that count as encapsulation characters. These can be next to SplitPoints.
    /// </summary>
    public string EncapsulationCharacters { get; set; } = "\"'*-";

    /// <summary>
    /// Specifies the continuation marker to use at the end of each chunk.
    /// </summary>
    public string ContinuationMarker { get; set; } = "(#c/#m)";

    /// <summary>
    /// The tag to use at the begining of the OOC statement.
    /// </summary>
    public string OocOpeningTag { get; set; } = "(( ";

    /// <summary>
    /// The tag to use at the ending of the OOC statement.
    /// </summary>
    public string OocClosingTag { get; set; } = " ))";

    /// <summary>
    /// When enabled, it puts the continuation marker on the last chunk as well. This is useful
    /// when someone uses a continuation marker that has something (1/3) and they want (3/3) on
    /// the last chunk.
    /// </summary>
    public bool MarkLastChunk { get; set; } = false;

    /// <summary>
    /// If true, scratch pads will automatically clear their text after copying the last block.
    /// </summary>
    public bool AutomaticallyClearAfterLastCopy { get; set; } = false;

    /// <summary>
    /// Decides behavior when hitting enter in Scratch Pad text entry.
    /// </summary>
    public Enums.EnterKeyAction ScratchPadTextEnterBehavior { get; set; } = Enums.EnterKeyAction.None;

    /// <summary>
    /// Maximum length of input on ScratchPads
    /// </summary>
    public int ScratchPadMaximumTextLength { get; set; } = 4096;

    /// <summary>
    /// Automatically replace double spaces in the text.
    /// </summary>
    public bool ReplaceDoubleSpaces { get; set; } = true;

    public bool DetectHeaderInput { get; set; } = true;

    public Dictionary<int, Vector4> HeaderColors = new()
    {
        {(int)Enums.ChatType.Emote, new(0.9f, 0.9f, 0.9f, 1f) },
        {(int)Enums.ChatType.Reply, new(1f, 0.35f, 0.6f, 1f) },
        {(int)Enums.ChatType.Say, new(1f, 1f, 1f, 1f) },
        {(int)Enums.ChatType.Party, new(0f, 0.5f, 0.6f, 1f) },
        {(int)Enums.ChatType.FC, new(0.6f, 0.75f, 1f, 1f) },
        {(int)Enums.ChatType.Shout, new(1f, 0.5f, 0.2f, 1f) },
        {(int)Enums.ChatType.Yell, new(0.9f, 1f, 0.2f, 1f) },
        {(int)Enums.ChatType.Tell, new(1f, 0.35f, 0.6f, 1f) },
        {(int)Enums.ChatType.Echo, new(0.75f, 0.75f, 0.75f, 1f) },
        {(int)Enums.ChatType.Linkshell, new(0.8f, 1f, 0.6f, 1f) }
    };

    #region Spell Checker Settings
    /// <summary>
    /// Holds the dictionary of words added by the user.
    /// </summary>
    public List<string> CustomDictionaryEntries { get; set; } = new();

    /// <summary>
    /// The file to be loaded into Lang dictionary.
    /// </summary>
    public string DictionaryFile { get; set; } = "lang_en";

    public Vector4 SpellingErrorHighlightColor { get; set; } = new( 0.9f, 0.2f, 0.2f, 1f );
    #endregion

    #region Linkshell Settings
    /// <summary>
    /// Contains the nicknames of all Cross-World Linkshells
    /// </summary>
    public string[] CrossWorldLinkshellNames { get; set; } = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };

    /// <summary>
    /// Contains the names of all normal Linkshells.
    /// </summary>
    public string[] LinkshellNames { get; set; } = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
    #endregion

    internal void ResetToDefault()
    {
        // Thesaurus settings.
        SearchHistoryCount = 10;
        ResearchToTop = true;

        // Scratch Pad settings
        DeleteClosedScratchPads = true;
        IgnoreWordsEndingInHyphen = true;
        PunctuationCleaningList = ",.'*\"-(){}[]!?<>`~♥@#$%^&*_=+\\/";
        ShowTextInChunks = true;
        BreakOnSentence = true;
        OocOpeningTag = "(( ";
        OocClosingTag = " ))";
        SplitPointDefinitions = ".?!";
        EncapsulationCharacters = "\"'*-";
        AutomaticallyClearAfterLastCopy = false;
        ScratchPadTextEnterBehavior = 0;
        ScratchPadMaximumTextLength = 4096;
        DetectHeaderInput = true;
        ReplaceDoubleSpaces = true;
        EnableTextHighlighting = true;
        SpellingErrorHighlightColor = new( 0.9f, 0.2f, 0.2f, 1f );
        HeaderColors = new()
        {
            { (int)Enums.ChatType.Emote, new( 0.9f, 0.9f, 0.9f, 1f ) },
            { (int)Enums.ChatType.Reply, new( 1f, 0.35f, 0.6f, 1f ) },
            { (int)Enums.ChatType.Say, new( 1f, 1f, 1f, 1f ) },
            { (int)Enums.ChatType.Party, new( 0f, 0.5f, 0.6f, 1f ) },
            { (int)Enums.ChatType.FC, new( 0.6f, 0.75f, 1f, 1f ) },
            { (int)Enums.ChatType.Shout, new( 1f, 0.5f, 0.2f, 1f ) },
            { (int)Enums.ChatType.Yell, new( 0.9f, 1f, 0.2f, 1f ) },
            { (int)Enums.ChatType.Tell, new( 1f, 0.35f, 0.6f, 1f ) },
            { (int)Enums.ChatType.Echo, new( 0.75f, 0.75f, 0.75f, 1f ) },
            { (int)Enums.ChatType.Linkshell, new( 0.8f, 1f, 0.6f, 1f ) }
        };

        // Spell Check settings
        DictionaryFile = "lang_en";

        Save();
    }
    internal void Save()
    {
        Wordsmith.PluginInterface.SavePluginConfig(this);
        Wordsmith.PluginInterface.UiBuilder.AddNotification("Configuration saved!", "Wordsmith", Dalamud.Interface.Internal.Notifications.NotificationType.Success);
        RecentlySaved = true;
    }
}
