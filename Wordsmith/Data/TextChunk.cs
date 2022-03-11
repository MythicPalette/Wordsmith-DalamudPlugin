namespace Wordsmith.Data;
internal class TextChunk
{
    internal string Header = "";

    internal string Text = "";

    internal string[] Words => this.Text.Split( ' ' );
    internal int WordCount => this.Words.Count();

    internal string CompleteText => $"{(Header.Length > 0 ? $"{Header} " : "")}{OutOfCharacterStartTag}{Text}{OutOfCharacterEndTag}{(ContinuationMarker.Length > 0 ? $" {ContinuationMarker}" : "")}";
    // Continuation marker
    internal string ContinuationMarker = "";

    // OOC Tags
    internal string OutOfCharacterStartTag = "";
    internal string OutOfCharacterEndTag = "";

    // Byte count
    internal TextChunk(string text) => this.Text = text;
}
