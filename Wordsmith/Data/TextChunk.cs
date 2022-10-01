namespace Wordsmith.Data;
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
    internal Word[] Words => this.Text.Words();

    /// <summary>
    /// The number of words in the text chunk.
    /// </summary>
    internal int WordCount => this.Words.Count();

    /// <summary>
    /// Assembles the complete chunk with header, OOC tags, continuation markers, and user-defined text.
    /// </summary>
    internal string CompleteText => $"{(Header.Length > 0 ? $"{Header} " : "")}{OutOfCharacterStartTag}{Text.Replace( Global.SPACED_WRAP_MARKER, " ").Replace(Global.NOSPACE_WRAP_MARKER, "").Replace("\n", "").Trim()}{OutOfCharacterEndTag}{(ContinuationMarker.Length > 0 ? $" {ContinuationMarker}" : "")}";
    
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
    internal TextChunk(string text) => this.Text = text;

    public override string ToString() => $"{{ StartIndex: {this.StartIndex}, Header: \"{this.Header}\", Text: \"{this.Text}\", Words: {this.Words}, Marker: {this.ContinuationMarker}, OOC Start: \"{this.OutOfCharacterStartTag}\", OOC End: \"{this.OutOfCharacterEndTag}\" }}";
}
