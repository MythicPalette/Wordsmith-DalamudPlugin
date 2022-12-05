using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Wordsmith.Enums;

namespace Wordsmith;

internal sealed class HeaderData
{
    // Event for when header data is changed.
    internal delegate void DataChangedHandler( HeaderData data );
    internal event DataChangedHandler? DataChanged;

    private ChatType _chatType;
    public ChatType ChatType
    {
        get { return _chatType; }
        set
        {
            // To avoid invoking DataChanged when it hasn't return
            // if the value is the same.
            if (_chatType == value)
                return;

            _chatType = value;
            DataChanged?.Invoke(this);
        }
    }

    private int _linkshell = 0;
    public int Linkshell
    {
        get { return _linkshell; }
        set
        {
            // To avoid invoking DataChanged when it hasn't return
            // if the value is the same.
            if (_linkshell == value)
                return;

            _linkshell = value;
            DataChanged?.Invoke(this);
        }
    }

    private bool _crossWorld = false;
    public bool CrossWorld
    {
        get { return _crossWorld; }
        set
        {
            // To avoid invoking DataChanged when it hasn't return
            // if the value is the same.
            if (_crossWorld == value)
                return;

            _crossWorld = value;
            DataChanged?.Invoke(this);
        }
    }

    private string _tellTarget = "";
    public string TellTarget
    {
        get { return _tellTarget; }
        set
        {
            // To avoid invoking DataChanged when it hasn't return
            // if the value is the same.
            if (_tellTarget == value)
                return;

            _tellTarget = value;
            DataChanged?.Invoke(this);
        }
    }

    private string _alias = string.Empty;
    private bool _useLongName = false;

    public string Headstring
    {
        get
        {
            string rtn;

            // If there is no chat type, just return the empty string
            if (ChatType == ChatType.None)
                rtn = ChatType.GetShortHeader();

            // If the chat type is linkshell, return the specific linkshell.
            else if ( ChatType == ChatType.Linkshell )
                rtn = $"/{(CrossWorld ? "cw" : "")}linkshell{Linkshell + 1}";

            // Get the long or short header based on the bool.
            else
                rtn = this._useLongName ? ChatType.GetLongHeader() : ChatType.GetShortHeader();

            // If ChatType is Tell append the target name
            if ( ChatType == ChatType.Tell )
                rtn += $" {TellTarget}";

            return rtn;
        }
    }

    public int Length => Headstring.Length;
    public int AliasLength => this._alias.Length > 0 ? this._alias.Length+1 : -1;

    public bool Valid { get; private set; }

    public HeaderData(bool valid) { Valid = valid; }

    public HeaderData(string headstring)
    {
        // If it doesn't start with a slash, we don't even try.
        Match re = Regex.Match(headstring, @"^\s*(/(\w+))\s+");
        if (!re.Success)
            return;

        // Find a matching header by default.
        for (int i = 1; i < Enum.GetValues(typeof(ChatType)).Length; ++i)
        {
            // Pattern will match long and short values.
            string pattern = ((ChatType)i).GetPattern();
            Match m = Regex.Match(headstring, pattern +"\\s+");

            // If the string starts with the header for the chat type then mark it
           if ( !m.Success )
                continue;

            // Get the chat type
            this.ChatType = (ChatType)i;

            // Groups = [match, short, long] so check if long matched for
            // whether or not to use the long name.
            this._useLongName = m.Groups["long"].Success;

            // Get the target if there is one.
            if ( this.ChatType == ChatType.Tell )
                this.TellTarget = m.Groups["target"].Value;

            // Get the linkshell channel if there is one.
            else if ( this.ChatType >= ChatType.Linkshell )
            {
                // Get the Linkshell channel but subtract 1 to account for 0 indexing.
                this.Linkshell = int.Parse( m.Groups["channel"].Value )-1;
                this.CrossWorld = this.ChatType > ChatType.Linkshell;
                this.ChatType = ChatType.Linkshell;
            }

            this.Valid = true;
            break;
        }

        // No match was found.
        if ( this.ChatType == ChatType.None)
        {
            // For each alias
            foreach ((int id, string alias, object? data) in Wordsmith.Configuration.HeaderAliases)
            {
                // If a matching alias is found
                Match m = Regex.Match(headstring, $"^/{alias}\\s+");
                if (m.Success)
                {
                    this.ChatType = (ChatType)id;
                    if ( this.ChatType == ChatType.None )
                        break;

                    else if (this.ChatType == ChatType.Tell && data is string dataString)
                        this.TellTarget = dataString;

                    else if ( this.ChatType >= ChatType.Linkshell && data is int iData )
                        this.Linkshell = iData;

                    else if ( this.ChatType >= ChatType.Linkshell && data is long lData )
                        this.Linkshell = (int)(long)lData;

                    if ( this.ChatType == ChatType.CrossWorldLinkshell )
                    {
                        this.ChatType = ChatType.Linkshell;
                        this.CrossWorld = true;
                    }

                    this._alias = alias;
                    this.Valid = true;
                    break;
                }
            }
        }
    }

    public HeaderData(ChatType type, int shell = 0, bool xworld = false, string target = "")
    {
        this.ChatType = type;
        this.Linkshell = shell;
        this.CrossWorld = xworld;
        this.TellTarget = target;
        this.Valid = true;
    }

    public override string ToString() => Headstring;
}

internal sealed class TextChunk
{
    /// <summary>
    /// Chat header to put at the beginning of each chunk when copied.
    /// </summary>
    internal string Header = "";

    /// <summary>
    /// Original text.
    /// </summary>
    internal string Text = "";

    /// <summary>
    /// Text split into words.
    /// </summary>
    internal Word[] Words => Text.Words();

    /// <summary>
    /// The number of words in the text chunk.
    /// </summary>
    internal int WordCount => Words.Count();

    /// <summary>
    /// Assembles the complete chunk with header, OOC tags, continuation markers, and user-defined text.
    /// </summary>
    internal string CompleteText => $"{(Header.Length > 0 ? $"{Header} " : "")}{OutOfCharacterStartTag}{Text.CleanMarkers().Replace("\n", "").Trim()}{OutOfCharacterEndTag}{(ContinuationMarker.Length > 0 ? $" {ContinuationMarker}" : "")}";

    /// <summary>
    /// The continuation marker to append to the end of the Complete Text value.
    /// </summary>
    internal string ContinuationMarker = "";

    /// <summary>
    /// The OOC starting tag to insert into the Complete Text value before Text.
    /// </summary>
    internal string OutOfCharacterStartTag = "";

    /// <summary>
    /// The OOC ending tag to insert into the Complete Text value after Text.
    /// </summary>
    internal string OutOfCharacterEndTag = "";

    /// <summary>
    /// The index where this chunk starts within the original text.
    /// </summary>
    internal int StartIndex = -1;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="text">The text that forms the chunk.</param>
    internal TextChunk(string text) => Text = text;

    public override string ToString() => $"{{ StartIndex: {StartIndex}, Header: \"{Header}\", Text: \"{Text}\", Words: {Words}, Marker: {ContinuationMarker}, OOC Start: \"{OutOfCharacterStartTag}\", OOC End: \"{OutOfCharacterEndTag}\" }}";
}

internal sealed class ThesaurusEntry : WordEntry
{
    // Synonyms
    private List<string> _syn = new List<string>();
    public string[] Synonyms { get => _syn.ToArray(); }
    public void AddSynonym(string word) => _syn.Add(word);
    public void AddSynonyms(IEnumerable<string> words) => _syn.AddRange(words);
    public string SynonymString => string.Join(", ", Synonyms ?? new string[] { });

    // Related words
    private List<string> _rel = new List<string>();
    public string[] Related { get => _rel.ToArray(); }
    public void AddRelatedWord(string word) => _rel.Add(word);
    public void AddRelatedWords(IEnumerable<string> words) => _rel.AddRange(words);
    public string RelatedString => string.Join(", ", Related ?? new string[] { });

    // Near antonyms
    private List<string> _near = new List<string>();
    public string[] NearAntonyms { get => _near.ToArray(); }
    public void AddNearAntonym(string word) => _near.Add(word);
    public void AddNearAntonyms(IEnumerable<string> words) => _near.AddRange(words);
    public string NearAntonymString => string.Join(", ", NearAntonyms ?? new string[] { });

    // Antonyms
    private List<string> _ant = new List<string>();
    public string[] Antonyms { get => _ant.ToArray(); }
    public void AddAntonym(string word) => _ant.Add(word);
    public void AddAntonyms(IEnumerable<string> words) => _ant.AddRange(words);
    public string AntonymString => string.Join(", ", Antonyms ?? new string[] { });
}

[StructLayout( LayoutKind.Sequential )]
internal struct Rect
{
    public int Left;        // x position of upper-left corner
    public int Top;         // y position of upper-left corner
    public int Right;       // x position of lower-right corner
    public int Bottom;      // y position of lower-right corner
    public Vector2 Position => new Vector2( Left, Top );
    public Vector2 Size => new Vector2( Right - Left, Bottom - Top );

    internal bool Contains(Vector2 v) => v.X > Position.X && v.X < Position.X + Size.X && v.Y > Position.Y && v.Y < Position.Y + Size.Y;
}

internal sealed class WebManifest
{
    internal bool IsLoaded { get; set; } = false;
    public string[] Dictionaries = Array.Empty<string>();
    public string[] Notice = Array.Empty<string>();
}

internal sealed class Word
{
    /// <summary>
    /// The start of the entire text segment.
    /// </summary>
    internal int StartIndex = -1;

    /// <summary>
    /// The index where the word starts. This can be different
    /// from StartIndex when the text starts with punctuation.
    /// </summary>
    internal int WordIndex = -1;

    /// <summary>
    /// The length of the word excluding any punctuation marks.
    /// </summary>
    internal int WordLength = -1;

    internal int WordEndIndex => WordIndex + WordLength;

    /// <summary>
    /// The last index of the text. This can be offset from
    /// StartIndex+WordLength if there is punctuation in the
    /// text.
    /// </summary>
    internal int EndIndex = -1;

    /// <summary>
    /// A value indicating whether or not the word is in the dictionary.
    /// </summary>
    internal bool InDictionary = true;

    /// <summary>
    /// A value indicating whether or not the word is hyphen-terminated.
    /// </summary>
    internal bool HyphenTerminated = false;

    internal List<string>? Suggestions;

    public Word() { }

    internal string GetString(string s) => GetString(s, 0);
    internal string GetString(string s, int offset) => StartIndex + offset >= 0 && StartIndex < EndIndex && EndIndex + offset <= s.Unwrap().Length ? s.Unwrap()[(StartIndex + offset)..(EndIndex + offset)] : "";
    internal string GetWordString(string s) => GetWordString(s, 0);
    internal string GetWordString(string s, int offset) => WordIndex + offset >= 0 && WordLength > 0 && WordIndex + WordLength + offset <= s.Unwrap().Length ? s.Unwrap()[(WordIndex + offset)..(WordIndex + WordLength + offset)] : "";
    internal void Offset(int value)
    {
        StartIndex += value;
        WordIndex += value;
        EndIndex += value;
    }
    internal void GenerateSuggestions(string wordText)
    {
        this.Suggestions = new();
        BackgroundWorker bw = new();
        bw.DoWork += ( s, e ) => { e.Result = Helpers.Lang.GetSuggestions( wordText ); };
        bw.RunWorkerCompleted += ( s, e ) => { if ( e.Result is not null ) this.Suggestions = (List<string>)e.Result; };
        bw.RunWorkerAsync();
    }
}

internal class WordEntry
{
    private static long _nextid;
    public readonly long ID;

    /// <summary>
    /// The word as a string
    /// </summary>
    public string Word { get; set; } = "";

    /// <summary>
    /// Holds the type of the word (i.e. Noun)
    /// </summary>
    private string _type = "";

    /// <summary>
    /// Gets or sets the type of the word (i.e. Noun)
    /// </summary>
    public string Type
    {
        get => _type;
        set => _type = value.CaplitalizeFirst();
    }

    /// <summary>
    /// The definition of this variant of the word.
    /// One word can have multiple variants
    /// </summary>
    public string Definition { get; set; } = "";
    
    /// <summary>
    /// A textual description of the word usage.
    /// </summary>
    public string Visualization { get; set; } = "";

    /// <summary>
    /// Default constructor that just assigns an ID.
    /// </summary>
    public WordEntry() { ID = ++_nextid; }
}

internal sealed class WordSearchResult
{
    /// <summary>
    /// Static value that holds the next available ID
    /// </summary>
    private static long _nextid = 0;

    /// <summary>
    /// Holds the ID of this instance.
    /// </summary>
    public readonly long ID;

    /// <summary>
    /// The original string used for the search.
    /// </summary>
    public string Query { get; set; }

    /// <summary>
    /// A list of all WordEntries that hold the word variant data.
    /// </summary>
    private List<WordEntry> _entries = new List<WordEntry>();

    /// <summary>
    /// An array of all word variant entries.
    /// </summary>
    public WordEntry[] Entries { get => _entries.ToArray(); }

    /// <summary>
    /// Adds a single entry to the collection.
    /// </summary>
    /// <param name="entry">The entry to be added.</param>
    public void AddEntry(WordEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Adds a range of entries to the collection.
    /// </summary>
    /// <param name="entries">The IEnumerable of WordEntry to add.</param>
    public void AddEntries(IEnumerable<WordEntry> entries) => _entries.AddRange(entries);

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="query">The string that was searched.</param>
    public WordSearchResult(string query)
    {
        Query = query;
        ID = ++_nextid;
    }
}