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
}
