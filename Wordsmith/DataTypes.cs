using System.ComponentModel;
using System.Runtime.InteropServices;
using Wordsmith.Enums;
using Wordsmith.Gui;

namespace Wordsmith;

public enum MarkerPosition { BeforeOOC, BeforeBody, AfterBody, AfterOOC, AfterContinuationMarker }
public enum RepeatMode { All, AllExceptFirst, AllExceptLast, OnlyOnFirst, OnlyOnLast, EveryNth }

[Flags]
public enum DisplayMode { WithMultipleChunks = 1, WithSingleChunk = 2, WithAnyChunkCount = 3, WithOOC = 4, WithoutOOC = 8, AnyOOC = 12, Always = 15}

public sealed class ChunkMarker
{
    /// <summary>
    /// Designates which <see cref="MarkerPosition"/> within the final <see cref="string"/> to place the text.
    /// </summary>
    public MarkerPosition Position { get { return this._position; } set { this._position = value; } }
    private MarkerPosition _position = MarkerPosition.AfterOOC;

    /// <summary>
    /// The <see cref="RepeatMode"/> designates how to determine which <see cref="string"/> results will
    /// use this marker.
    /// </summary>
    public RepeatMode RepeatMode { get { return this._repeatMode; } set { this._repeatMode = value; } }
    private RepeatMode _repeatMode = RepeatMode.All;

    /// <summary>
    /// The <see cref="DisplayMode"/> flags for how and when this marker will be displayed.
    /// </summary>
    public DisplayMode DisplayMode { get { return this._displayMode; } set { this._displayMode = value; } }
    private DisplayMode _displayMode = DisplayMode.WithMultipleChunks | DisplayMode.AnyOOC;

    /// <summary>
    /// <see cref="uint"/> iterative number specificing how often to repeat.
    /// </summary>
    public uint Nth
    {
        get { return this._nth; }
        set
        {
            // nth can't be zero.
            if ( value == 0 )
                this._nth = 1;
            else
                this._nth = value;
        }
    }
    private uint _nth = 1;

    /// <summary>
    /// <see cref="uint"/> chunk number for the first iteration to appear on (one-based, not zero-based)
    /// </summary>
    public uint StartPosition { get { return this._offset; } set { this._offset = value; } }
    private uint _offset = 1;

    /// <summary>
    /// <see cref="string"/> seed for the final text. This may contain placeholders to be replaced.
    /// </summary>
    public string Text { get { return this._text;} set { this._text = value; } }
    private string _text = "";

    public ChunkMarker() { }
    public ChunkMarker( string text, MarkerPosition position, RepeatMode repeat, DisplayMode display) : this( text, position, repeat, display, 1, 0 ) { }
    public ChunkMarker(string text, MarkerPosition position, RepeatMode repeat, DisplayMode display, uint nth, uint offset)
    {
        this.Text = text;
        this.Position = position;
        this.DisplayMode = display;

        // If the display mode is single chunk the repeat mode can only be "All"
        this.RepeatMode = (display & DisplayMode.WithAnyChunkCount) == DisplayMode.WithSingleChunk ? RepeatMode.All : repeat;
        this.Nth = nth;
        this.StartPosition = offset;
    }
    
    /// <summary>
    /// Determines if <see langword="this"/> <see cref="ChunkMarker"/> applies to the <see cref="string"/>
    /// based on the current <see cref="RepeatMode"/>
    /// </summary>
    /// <param name="chunkNumber">The one-based chunk number to test for.</param>
    /// <param name="chunkCount">The total count of chunks in the set.</param>
    /// <returns><see cref="true"/> if <see langword="this"/> <see cref="ChunkMarker"/> applies.</returns>
    internal bool AppliesTo(int chunkNumber, int chunkCount)
    {
        // If the repeat mode is All then every other check can be skipped.
        if ( this.RepeatMode != RepeatMode.All )
        {
            if ( this.RepeatMode == RepeatMode.AllExceptFirst && chunkNumber == 0 )
                return false;

            else if ( this.RepeatMode == RepeatMode.AllExceptLast && chunkNumber == chunkCount - 1 )
                return false;

            else if ( this.RepeatMode == RepeatMode.OnlyOnFirst && chunkNumber != 0 )
                return false;

            else if ( this.RepeatMode == RepeatMode.OnlyOnLast && chunkNumber != chunkCount - 1 )
                return false;

            else if ( this.RepeatMode == RepeatMode.EveryNth )
            {
                int startpos = (int)(this.StartPosition - 1);

                if ( chunkNumber < startpos )
                    return false;
                if ( (chunkNumber - startpos) % this.Nth != 0 )
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Determines if the <see cref="ChunkMarker"/> should be visible in the string based on
    /// the current <see cref="DisplayMode"/>
    /// </summary>
    /// <param name="UseOOC"><see cref="bool"/> indicating the OOC state of the text.</param>
    /// <param name="chunkCount"><see cref="int"/> count of all chunks in the collection.</param>
    /// <returns><see cref="true"/> if the <see cref="ChunkMarker"/> should be visible in the string.</returns>
    internal bool Visible(bool UseOOC, int chunkCount)
    {
        // DisplayMode should never be 0 but in any case that it is
        // consider this an immeditate false. This is to future-proof
        // in the case that a disabling mode is added.
        if ( this.DisplayMode == 0 )
            return false;

        // If DisplayMode is not Always then we need to begin filtering  
        if ( this.DisplayMode != DisplayMode.Always )
        {
            // If there is an OOC specifier
            if ( (this.DisplayMode & DisplayMode.AnyOOC) != DisplayMode.AnyOOC)
            {
                // If only showing with OOC and OOC is off then skip
                if ( (this.DisplayMode & DisplayMode.WithOOC) == DisplayMode.WithOOC && !UseOOC )
                    return false;

                // If not showing with OOC and OOC is on then skip
                else if ( (this.DisplayMode & DisplayMode.WithoutOOC) == DisplayMode.WithoutOOC && UseOOC )
                    return false;
            }

            // If there is a chunk count specifier
            if ( (this.DisplayMode & DisplayMode.WithAnyChunkCount) != DisplayMode.WithAnyChunkCount )
            {
                // If only displaying with single chunk and there is more than one then skip
                if ( (this.DisplayMode & DisplayMode.WithSingleChunk) == DisplayMode.WithSingleChunk && chunkCount > 1 )
                    return false;

                // If only displaying with multiple chunks and there is only one then skip
                if ( (this.DisplayMode & DisplayMode.WithMultipleChunks) == DisplayMode.WithMultipleChunks && chunkCount == 1 )
                    return false;
            }
        }
        return true;
    }

    public override string ToString()
    {
        // Dump the object then covert the resulting dictionary to a string.
        Dictionary<string, object> dict = this.Dump();
        List<string> lResults = new();
        foreach ( string key in dict.Keys )
            lResults.Add($"{{\"{key}\", {dict[key]}}}");
        return $"{{{string.Join(", ", lResults)}}}";
    }

    /// <summary>
    /// Sorts a list of markers based on their position in the string.
    /// </summary>
    /// <param name="list">The list of markers</param>
    /// <returns>A <see cref="List{ChunkMarker}"/> of sorted markers.</returns>
    public static List<ChunkMarker> SortList(List<ChunkMarker> list )
    {
        List<ChunkMarker> result = new();
        foreach ( MarkerPosition mp in Enum.GetValues( typeof( MarkerPosition ) ) )
            foreach ( ChunkMarker cm in list.Where( x => x.Position == mp ) )
                result.Add( cm );
        return result;
    }
}

/// <summary>
/// A class designed for tracking delta.
/// </summary>
internal sealed class Clock
{
    // A C# DateTime tick is 1/10,000,000 of a second.
    private const float TICKS_PER_SECOND = 10000000f;

    // The last frame time.
    private DateTime _lastTick = DateTime.MinValue;

    /// <summary>
    /// The time it took from the last call to <see cref="Tick()"/>
    /// to the most recent.
    /// </summary>
    internal float Delta { get; private set; }

    /// <summary>
    /// The longest recorded <see cref="Delta"/> since the
    /// </summary>
    internal float LongestFrame { get; private set; }
    public Clock()
    {
        this._lastTick = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the new <see cref="Delta"/> value from
    /// the current time and sets <see cref="LongestFrame"/> if
    /// the current delta is the longest.
    /// </summary>
    internal void Tick()
    {
        // Get the current tick
        DateTime tick = DateTime.UtcNow;

        // Compare that the last tick to find the difference and convert it to seconds.
        float delta = (tick.Ticks - this._lastTick.Ticks) / TICKS_PER_SECOND;

        // If the delta is longer than any others and this is not the first tick then
        // apply this to the longest frame.
        if ( delta > this.LongestFrame && _lastTick > DateTime.MinValue)
            this.LongestFrame = delta;

        // Set Delta and _lastTick
        this.Delta = delta;
        this._lastTick = tick;
    }

    /// <summary>
    /// Sets <see cref="LongestFrame"/> to the current Delta.
    /// </summary>
    internal void ResetLongest() => this.LongestFrame = this.Delta;
}

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
            if (this.ChatType == ChatType.None)
                rtn = this.ChatType.GetShortHeader();

            // If the chat type is linkshell, return the specific linkshell.
            else if ( this.ChatType == ChatType.Linkshell )
                rtn = $"/{(this.CrossWorld ? "cw" : "")}linkshell{this.Linkshell + 1}";

            // Get the long or short header based on the bool.
            else
                rtn = this._useLongName ? this.ChatType.GetLongHeader() : this.ChatType.GetShortHeader();

            // If ChatType is Tell append the target name
            if ( this.ChatType == ChatType.Tell )
                rtn += $" {this.TellTarget}";

            return rtn;
        }
    }

    public int Length => this.Headstring.Length;
    public int AliasLength => this._alias.Length > 0 ? this._alias.Length+1 : -1;

    public bool Valid { get; private set; }

    public HeaderData(bool valid) { this.Valid = valid; }

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

    public override string ToString() => this.Headstring;
}

/// <summary>
/// A class used for comparing the states of a <see cref="ScratchPadUI"/>
/// </summary>
internal sealed class PadState
{
    /// <summary>
    /// The <see cref="string"/> of the <see cref="ScratchPadUI"/>
    /// </summary>
    internal string ScratchText;

    /// <summary>
    /// A <see cref="bool"/> that indicates whether the <see cref="ScratchPadUI"/>
    /// is/was using OOC.
    /// </summary>
    internal bool UseOOC;

    /// <summary>
    /// The <see cref="HeaderData"/> that is/was used by the <see cref="ScratchPadUI"/>
    /// </summary>
    internal HeaderData? Header = null;

    public PadState()
    {
        this.ScratchText = "";
        this.UseOOC = false;
    }

    public PadState( ScratchPadUI ui )
    {
        this.ScratchText = ui.ScratchString;
        this.UseOOC = ui.UseOOC;
        this.Header = ui.Header;
    }

    public static bool operator ==( PadState state, object other ) => state.Equals( other );

    public static bool operator !=( PadState state, object other ) => !state.Equals( other );

    public override bool Equals( object? obj )
    {
        if ( obj == null )
            return false;

        if ( obj is not PadState )
            return false;


        PadState o = (PadState)obj;
        if ( o.ScratchText != this.ScratchText )
            return false;
        if ( o.UseOOC != this.UseOOC )
            return false;
        return true;
    }

    public override int GetHashCode() => HashCode.Combine( this.Header?.ChatType, this.ScratchText, this.UseOOC, this.Header?.TellTarget );

    public override string ToString() => $"{{ ChatType: {this.Header?.ChatType}, ScratchText: \"{this.ScratchText}\", UseOOC: {this.UseOOC}, TellTarget: \"{this.Header?.TellTarget ?? ""}\", CrossWorld: {this.Header?.CrossWorld}, Linkshell: {this.Header?.Linkshell} }}";
}

internal sealed class TextChunk
{
    /// <summary>
    /// Chat header to put at the beginning of each chunk when copied.
    /// </summary>
    internal string Header { get; set; } = "";

    /// <summary>
    /// Original text.
    /// </summary>
    internal string Text { get; set; } = "";

    /// <summary>
    /// Text split into words.
    /// </summary>
    internal List<Word> Words => this.Text.Words();

    /// <summary>
    /// The number of words in the text chunk.
    /// </summary>
    internal int WordCount => this.Words.Count;

    internal string ContinuationMarker = "";

    /// <summary>
    /// The OOC starting tag to insert into the Complete Text value before Text.
    /// </summary>
    internal string OutOfCharacterStartTag { get; set; } = "";

    /// <summary>
    /// The OOC ending tag to insert into the Complete Text value after Text.
    /// </summary>
    internal string OutOfCharacterEndTag { get; set; } = "";

    /// <summary>
    /// The continuation marker to append to the end of the Complete Text value.
    /// </summary>
    /// <summary>
    /// The index where this chunk starts within the original text.
    /// </summary>
    internal int StartIndex { get; set; } = -1;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="text">The text that forms the chunk.</param>
    internal TextChunk( string text ) { this.Text = text; }
}

internal sealed class ThesaurusEntry : WordEntry
{
    #region Synonyms
    /// <summary>
    /// A <see cref="IReadOnlyList{T}"/> of <see cref="string"/> containing all synonyms.
    /// </summary>
    public IReadOnlyList<string> Synonyms { get => _syn; }
    private List<string> _syn = new();

    /// <summary>
    /// Adds a synonym to the list.
    /// </summary>
    /// <param name="word"><see cref="string"/> synonym.</param>
    public void AddSynonym(string word) => _syn.Add(word);

    /// <summary>
    /// Adds a collection of synonyms to the list.
    /// </summary>
    /// <param name="words">An <see cref="IEnumerable{T}"/> of synonyms</param>
    public void AddSynonyms(IEnumerable<string> words) => _syn.AddRange(words);

    /// <summary>
    /// Returns all synonyms joined into a comma-separated <see cref="string"/>
    /// </summary>
    public string SynonymString => string.Join(", ", this.Synonyms ?? Array.Empty<string>());
    #endregion

    #region Related
    /// <summary>
    /// A <see cref="IReadOnlyList{T}"/> of <see cref="string"/> containing all related words.
    /// </summary>
    public IReadOnlyList<string> Related { get => _rel; }
    private List<string> _rel = new();

    /// <summary>
    /// Adds a related word to the list.
    /// </summary>
    /// <param name="word"><see cref="string"/> synonym.</param>
    public void AddRelatedWord(string word) => _rel.Add(word);

    /// <summary>
    /// Adds a collection of related words to the list.
    /// </summary>
    /// <param name="words">An <see cref="IEnumerable{T}"/> of related words</param>
    public void AddRelatedWords(IEnumerable<string> words) => _rel.AddRange(words);

    /// <summary>
    /// Returns all related words joined into a comma-separated <see cref="string"/>
    /// </summary>
    public string RelatedString => string.Join(", ", this.Related ?? Array.Empty<string>() );
    #endregion

    #region Near Antonyms
    /// <summary>
    /// A <see cref="IReadOnlyList{T}"/> of <see cref="string"/> containing all near antonyms.
    /// </summary>
    public IReadOnlyList<string> NearAntonyms { get => _near; }
    private List<string> _near = new();

    /// <summary>
    /// Adds a near antonyms to the list.
    /// </summary>
    /// <param name="word"><see cref="string"/> near antonym.</param>
    public void AddNearAntonym(string word) => _near.Add(word);

    /// <summary>
    /// Adds a collection of near antonyms to the list.
    /// </summary>
    /// <param name="words">An <see cref="IEnumerable{T}"/> of near antonyms</param>
    public void AddNearAntonyms(IEnumerable<string> words) => _near.AddRange(words);

    /// <summary>
    /// Returns all near antonyms joined into a comma-separated <see cref="string"/>
    /// </summary>
    public string NearAntonymString => string.Join(", ", this.NearAntonyms ?? Array.Empty<string>() );
    #endregion

    #region Antonyms
    /// <summary>
    /// A <see cref="IReadOnlyList{T}"/> of <see cref="string"/> containing all antonyms.
    /// </summary>
    public IReadOnlyList<string> Antonyms { get => _ant; }
    private List<string> _ant = new();

    /// <summary>
    /// Adds a antonym to the list.
    /// </summary>
    /// <param name="word"><see cref="string"/> antonym.</param>
    public void AddAntonym(string word) => _ant.Add(word);

    /// <summary>
    /// Adds a collection of antonyms to the list.
    /// </summary>
    /// <param name="words">An <see cref="IEnumerable{T}"/> of antonyms</param>
    public void AddAntonyms(IEnumerable<string> words) => _ant.AddRange(words);

    /// <summary>
    /// Returns all antonyms joined into a comma-separated <see cref="string"/>
    /// </summary>
    public string AntonymString => string.Join(", ", this.Antonyms ?? Array.Empty<string>() );
    #endregion
}

[StructLayout( LayoutKind.Sequential )]
internal struct Rect
{
    /// <summary>
    /// <see cref="int"/> X coordinate of upper left corner
    /// </summary>
    public int Left;

    /// <summary>
    /// <see cref="int"/> Y Coordinate of upper left corner
    /// </summary>
    public int Top;

    /// <summary>
    /// <see cref="int"/> X Coordinate of bottom right corner
    /// </summary>
    public int Right;

    /// <summary>
    /// <see cref="int"/> Y Coordinate of bottom right corner
    /// </summary>
    public int Bottom;

    /// <summary>
    /// Returns the <see cref="Vector2"/> coordinates of the
    /// upper left corner of the rectangle
    /// </summary>
    public Vector2 Position => new( Left, Top );

    /// <summary>
    /// Returns the size of the rectangle as a <see cref="Vector2"/>
    /// where X is width and Y is height.
    /// </summary>
    public Vector2 Size => new( Right - Left, Bottom - Top );

    /// <summary>
    /// Determines whether or not a <see cref="Vector2"/> coordinate is within
    /// the bounds of this rect
    /// </summary>
    /// <param name="v">The <see cref="Vector2"/> coordinate to test</param>
    /// <returns><see langword="true"/> if the coordinate is within the bounds.</returns>
    internal bool Contains( Vector2 v )
    {
        if ( v.X < this.Left || v.Y < this.Top || v.X > this.Right || v.Y > this.Bottom )
            return false;
        return true;
    }
}

internal sealed class WebManifest
{
    /// <summary>
    /// A <see cref="bool"/> value that indicates whether the manifest was
    /// successfully loaded or not.
    /// </summary>
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

    /// <summary>
    /// The index where the word ends. This can be different
    /// from EndIndex when the text ends with punctuation.
    /// </summary>
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

    /// <summary>
    /// This is a list of suggest word replacements for spelling errors.
    /// </summary>
    internal List<string>? Suggestions;

    public Word() { }

    /// <summary>
    /// Attempts to collect the text from start index to end index in a given string.
    /// NOTE: If a different string is used to get the word than was used to create it then
    /// create it then incorrect data may be returned.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to get the text from.</param>
    /// <returns><see cref="string"/> containing the matching text.</returns>
    internal string GetString(string s) => GetString(s, 0);

    /// <summary>
    /// Attempts to collect the text from start index to end index in a given string.
    /// NOTE: If a different string is used to get the word than was used to create it then
    /// incorrect data may be returned.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to get the text from.</param>
    /// <param name="offset">The <see cref="int"/> offset position to begin looking for the text.</param>
    /// <returns><see cref="string"/> containing the matching text.</returns>
    internal string GetString(string s, int offset) => StartIndex + offset >= 0 && StartIndex < EndIndex && EndIndex + offset <= s.Length ? s[(StartIndex + offset)..(EndIndex + offset)] : "";

    /// <summary>
    /// Attempts to collect the word from word index to word end index in a given string.
    /// NOTE: If a different string is used to get the word than was used to create it then
    /// incorrect data may be returned.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to get the text from.</param>
    /// <returns><see cref="string"/> containing the matching text.</returns>
    internal string GetWordString(string s) => GetWordString(s, 0);

    /// <summary>
    /// Attempts to collect the text from word index to word end index in a given string.
    /// NOTE: If a different string is used to get the word than was used to create it then
    /// incorrect data may be returned.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to get the text from.</param>
    /// <param name="offset">The <see cref="int"/> offset position to begin looking for the word.</param>
    /// <returns><see cref="string"/> containing the matching text.</returns>
    internal string GetWordString(string s, int offset) => WordIndex + offset >= 0 && WordLength > 0 && WordIndex + WordLength + offset <= s.Length ? s[(WordIndex + offset)..(WordIndex + WordLength + offset)] : "";

    /// <summary>
    /// Offset the starting and ending indices for the whole text and word.
    /// </summary>
    /// <param name="offset"><see cref="int"/> value to move the indices.</param>
    /// <exception cref="IndexOutOfRangeException"/>
    internal void Offset(int offset)
    {
        if ( this.StartIndex + offset < 0 )
            throw new IndexOutOfRangeException();
        if ( this.WordEndIndex + offset < 0 )
            throw new IndexOutOfRangeException();
        if ( this.EndIndex + offset < 0 )
            throw new IndexOutOfRangeException();

        StartIndex += offset;
        WordIndex += offset;
        EndIndex += offset;
    }

    /// <summary>
    /// Asynchronously gets word suggestions for the given word.
    /// </summary>
    /// <param name="wordText">Text to generate suggestions based on.</param>
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
    private List<WordEntry> _entries = new();

    /// <summary>
    /// An array of all word variant entries.
    /// </summary>
    public IReadOnlyList<WordEntry> Entries { get => _entries; }

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
        this.Query = query;
        ID = ++_nextid;
    }
}