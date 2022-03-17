namespace Wordsmith.Data;

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

    internal int WordEndIndex => this.WordIndex + this.WordLength;

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

    public string GetString( string s ) => this.GetString( s, 0 );
    public string GetString( string s, int offset ) => this.StartIndex + offset >= 0 && this.StartIndex < this.EndIndex && this.EndIndex + offset <= s.Unwrap().Length ? s.Unwrap()[(this.StartIndex + offset)..(this.EndIndex + offset)] : "";
    public string GetWordString( string s ) => this.GetWordString( s, 0 );
    public string GetWordString( string s, int offset ) => this.WordIndex + offset >= 0 && this.WordLength > 0 && this.WordIndex + this.WordLength + offset <= s.Unwrap().Length ? s.Unwrap()[(this.WordIndex + offset)..(this.WordIndex + this.WordLength + offset)] : "";
    public void Offset(int value)
    {
        this.StartIndex += value;
        this.WordIndex += value;
        this.EndIndex += value;
    }
}
