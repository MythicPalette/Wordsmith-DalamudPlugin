using System.Data;
using System.Text.RegularExpressions;
using Wordsmith.Enums;

namespace Wordsmith;

internal class HeaderData
{
    internal event EventHandler? DataChanged;

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
            DataChanged?.Invoke(this, EventArgs.Empty);
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
            DataChanged?.Invoke(this, EventArgs.Empty);
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
            DataChanged?.Invoke(this, EventArgs.Empty);
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
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Headstring
    {
        get
        {
            string rtn;

            if (ChatType == ChatType.None)
                rtn = ChatType.GetShortHeader();

            else
            {
                // Get the slash command.
                rtn = ChatType.GetShortHeader();

                // If /tell get the target or placeholder.
                if (ChatType == ChatType.Tell)
                    rtn += $" {TellTarget} ";

                // Grab the linkshell command.
                else if (ChatType == ChatType.Linkshell)
                    rtn = $"/{(CrossWorld ? "cw" : "")}linkshell{Linkshell + 1}";
            }
            return rtn;
        }
    }
    public int Length { get => Headstring.Length; }
    public bool Valid { get; private set; }

    public HeaderData(bool valid)
    {
        Valid = valid;
    }

    public HeaderData(string headstring, out int len)
    {
        len = -1;

        // If it doesn't start with a slash, we don't even try.
        Match re = Regex.Match(headstring, @"^\s*(/(\w+))\s+");
        if (!re.Success)
            return;

        // Find a matching header by default.
        for (int i = 1; i < Enum.GetValues(typeof(ChatType)).Length; ++i)
        {
            Match m = Regex.Match(headstring, ((ChatType)i).GetPattern());
            // If the string starts with the header for the chat type then mark it
            if (m.Success)
            {
                ChatType = (ChatType)i;
                if (ChatType == ChatType.Tell)
                    TellTarget = m.Groups["target"].Value;

                len = m.Groups[0].Value.Length;
            }
            // if the header type is linkshell or CrossWorldLinkshell
            else if ((ChatType)i == ChatType.Linkshell || (ChatType)i == ChatType.CrossWorldLinkshell)
            {
                // Linkshells must be checked with numbers appended to them.
                for (int x = 1; x <= 8; ++x)
                {
                    // If the number was found, mark the header type and the number.
                    if (headstring.StartsWith($"{((ChatType)i).GetShortHeader()}{x} "))
                    {
                        ChatType = ChatType.Linkshell;
                        Linkshell = x - 1;
                        return;
                    }
                }

                // This continue prevents code from running after failing to identify the linkshell.
                continue;
            }
            // No matches and not checking linkshells, skip the following code and loop again
            else
                continue;

            Valid = true;

            // Neither of these two types should be allowed passed this point.
            if (i == (int)ChatType.Linkshell)
                continue;
            if (i == (int)ChatType.CrossWorldLinkshell)
                continue;

            // If there is a header to fit with the chat type.
            if (((ChatType)i).GetShortHeader().Length > 0)
            {
                // Set the chat type
                ChatType = (ChatType)i;

                // Break from the loop
                break;
            }
        }

        // No match was found.
        if (ChatType == ChatType.None)
        {
            // For each alias
            foreach ((int id, string alias, object? data) in Wordsmith.Configuration.HeaderAliases)
            {
                // If a matching alias is found
                if (headstring.StartsWith($"/{alias} ") || headstring.StartsWith($"/{alias}\n"))
                {
                    // If the ID for the alias is Tell
                    if (id == (int)ChatType.Tell && data == null)
                    {
                        ChatType = ChatType.Tell;
                    }
                    else if (id == (int)ChatType.Tell && data is string dataString)
                    {
                        ChatType = ChatType.Tell;
                        TellTarget = dataString;
                        return;
                    }

                    // If it isn't /Tell and the ChatType is within normal range
                    // simply assign the chat type.
                    else if (id < (int)ChatType.Linkshell)
                    {
                        // Assign the chat type and break from the loop.
                        ChatType = (ChatType)id;
                        return;
                    }

                    else
                    {
                        // Set the chat type to Linkshell
                        ChatType = ChatType.Linkshell;

                        // Get the linkshell number.
                        Linkshell = (id - (int)ChatType.Linkshell) % 8;

                        // Determine if the linkshell is crossworld
                        CrossWorld = id - (int)ChatType.Linkshell >= 8;
                    }
                    break;
                }
            }
        }
    }
    public HeaderData(ChatType type, int shell = 0, bool xworld = false, string target = "")
    {
        ChatType = type;
        Linkshell = shell;
        CrossWorld = xworld;
        TellTarget = target;

    }
    public override string ToString() => Headstring;
}

internal class TextChunk
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
    internal string CompleteText => $"{(Header.Length > 0 ? $"{Header} " : "")}{OutOfCharacterStartTag}{Text.Replace(Global.SPACED_WRAP_MARKER, " ").Replace(Global.NOSPACE_WRAP_MARKER, "").Replace("\n", "").Trim()}{OutOfCharacterEndTag}{(ContinuationMarker.Length > 0 ? $" {ContinuationMarker}" : "")}";

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

public class ThesaurusEntry : WordEntry
{
    // Synonyms
    protected List<string> _syn = new List<string>();
    public string[] Synonyms { get => _syn.ToArray(); }
    public void AddSynonym(string word) => _syn.Add(word);
    public void AddSynonyms(IEnumerable<string> words) => _syn.AddRange(words);
    public string SynonymString => string.Join(", ", Synonyms ?? new string[] { });

    // Related words
    protected List<string> _rel = new List<string>();
    public string[] Related { get => _rel.ToArray(); }
    public void AddRelatedWord(string word) => _rel.Add(word);
    public void AddRelatedWords(IEnumerable<string> words) => _rel.AddRange(words);
    public string RelatedString => string.Join(", ", Related ?? new string[] { });

    // Near antonyms
    protected List<string> _near = new List<string>();
    public string[] NearAntonyms { get => _near.ToArray(); }
    public void AddNearAntonym(string word) => _near.Add(word);
    public void AddNearAntonyms(IEnumerable<string> words) => _near.AddRange(words);
    public string NearAntonymString => string.Join(", ", NearAntonyms ?? new string[] { });

    // Antonyms
    protected List<string> _ant = new List<string>();
    public string[] Antonyms { get => _ant.ToArray(); }
    public void AddAntonym(string word) => _ant.Add(word);
    public void AddAntonyms(IEnumerable<string> words) => _ant.AddRange(words);
    public string AntonymString => string.Join(", ", Antonyms ?? new string[] { });
}

internal class Rect2
{
    internal Vector2 Position;
    internal Vector2 Size;
    public Rect2(float x, float y, float w, float h)
    {
        Position = new(x, y);
        Size = new(w, h);
    }
    public Rect2(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    internal bool Contains(Vector2 v) => v.X > Position.X && v.X < Position.X + Size.X && v.Y > Position.Y && v.Y < Position.Y + Size.Y;
}

internal class WebManifest
{
    internal bool IsLoaded { get; set; } = false;
    public string[] Dictionaries = Array.Empty<string>();
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

    public string GetString(string s) => GetString(s, 0);
    public string GetString(string s, int offset) => StartIndex + offset >= 0 && StartIndex < EndIndex && EndIndex + offset <= s.Unwrap().Length ? s.Unwrap()[(StartIndex + offset)..(EndIndex + offset)] : "";
    public string GetWordString(string s) => GetWordString(s, 0);
    public string GetWordString(string s, int offset) => WordIndex + offset >= 0 && WordLength > 0 && WordIndex + WordLength + offset <= s.Unwrap().Length ? s.Unwrap()[(WordIndex + offset)..(WordIndex + WordLength + offset)] : "";
    public void Offset(int value)
    {
        StartIndex += value;
        WordIndex += value;
        EndIndex += value;
    }
}

public class WordEntry
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
    protected string _type = "";

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
    /// Default constructor that just assigns an ID.
    /// </summary>
    public WordEntry() { ID = ++_nextid; }
}

public class WordSearchResult
{
    /// <summary>
    /// Static value that holds the next available ID
    /// </summary>
    protected static long _nextid = 0;

    /// <summary>
    /// Holds the ID of this instance.
    /// </summary>
    public readonly long ID;

    /// <summary>
    /// The original string used for the search.
    /// </summary>
    public string Query { get; set; }

    /// <summary>
    /// Returns true if an exception happened while searching for the word.
    /// </summary>
    public bool SearchError => Exception != null;

    /// <summary>
    /// Is null unless an exception occured while searching in which case this will hold that value.
    /// </summary>
    public Exception? Exception;

    /// <summary>
    /// A list of all WordEntries that hold the word variant data.
    /// </summary>
    protected List<WordEntry> _entries = new List<WordEntry>();

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