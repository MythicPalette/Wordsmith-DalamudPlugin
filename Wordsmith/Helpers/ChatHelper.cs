
using Wordsmith.Data;

namespace Wordsmith.Helpers;

internal class ChatHelper
{
    /// <summary>
    /// Takes inputs and returns it as a collection of strings that are ready to be sent, all under 500 bytes.
    /// </summary>
    /// <param name="header">The header to place at the front of each string (i.e. /tell Player Name@World)</param>
    /// <param name="text">The text to be conferted into strings.</param>
    /// <returns>Returns an array of strings, all under 500 bytes to prepare for sending.</returns>
    internal static List<TextChunk> FFXIVify(string header, string text, bool OOC)
    {
        UTF8Encoding encoder = new();

        // Get the number of bytes taken by the header.
        // Note that we remove 10 bytes right off of the top as a safety zone.
        // We then cut the bytes out required for the header, continuation marker, and OOC tags.
        int availableBytes = 490 - encoder.GetByteCount($"{header} ") - encoder.GetByteCount(Wordsmith.Configuration.ContinuationMarker);

        // Cut the length of the OOC from the available bytes if OOC is on.
        availableBytes -= (OOC ? encoder.GetByteCount(Wordsmith.Configuration.OocOpeningTag + Wordsmith.Configuration.OocClosingTag) : 0);

        // Create a list to hold all of our strings.
        List<TextChunk> results = new();

        // Break the string into smaller sizes.
        int offset = 0;
        while (offset < text.Length)
        {
            // Get the current possible string.
            string str = SubstringByByteCount(text, offset, availableBytes);

            // Add the string to the list with the header and, if offset is not at
            // the end of the string yet, add the continuation marker for the player.
            if ( str.Trim().Length > 0 )
                results.Add( new(
                        str.Trim() )
                {
                    StartIndex = offset + (str.Length - str.TrimStart().Length),
                    Header = header,
                    OutOfCharacterStartTag = OOC ? Wordsmith.Configuration.OocOpeningTag : "",
                    OutOfCharacterEndTag = OOC ? Wordsmith.Configuration.OocClosingTag : ""
                } );

            // Add the length of the string to the offset.
            offset += str.Length;
        }

        // If there is more than one result we want to do continuation markers
        if (results.Count > 1)
        {
            // Iterate through the chunks and append the continuation marker. We have to do this
            // in a separate loop from when we created the chunks in case the user adds the #m tag
            // to their continuation marker and we need to know the total number of chunks.
            for ( int i = 0; i < (Wordsmith.Configuration.MarkLastChunk ? results.Count : results.Count - 1); ++i )
                results[i].ContinuationMarker = Wordsmith.Configuration.ContinuationMarker.Replace( "#c", (i + 1).ToString() ).Replace( "#m", results.Count.ToString() );
        }

        // Return the results.
        return results;
    }

    /// <summary>
    /// Gets a substring based on maximum number of bytes allowed.
    /// </summary>
    /// <param name="text">Text to get the substring from</param>
    /// <param name="offset">The starting index of the substring.</param>
    /// <param name="byteLimit">The maximum byte length of the return.</param>
    /// <exception cref="IndexOutOfRangeException">Offset is out of range of text.</exception>
    /// <returns>A string that is under the byte limit.</returns>
    protected static string SubstringByByteCount(string text, in int startIndex, in int byteLimit)
    {
        // If the offset is out of index, throw an out of range exception
        if ( startIndex >= text.Length)
            throw new IndexOutOfRangeException();

        // Designate a text encoder so we don't reinitialize a new one every time.
        UTF8Encoding encoder = new UTF8Encoding();

        // Create a variable to hold the last known space and last known sentence marker
        int lastSpace = -1;
        int lastSentence = -1;

        // Start with a character length of 1 and try increasing lengths.
        for (int length=1; length+ startIndex < text.Length; ++length)
        {
            // If the current length would be over the byte limit
            if (encoder.GetByteCount(text.Substring( startIndex, length)) > byteLimit)
            {
                // reduce the length by one as we've officially crossed the maximum byte count.
                --length;

                // If we never found a space, we'll have to split the string at length regardless.
                if (lastSpace == -1)
                    lastSpace = length;

                if (Wordsmith.Configuration.BreakOnSentence && lastSentence > 0 && lastSentence > startIndex )
                    return text.Substring( startIndex, lastSentence);
                else
                    // get the substring starting from offset. If the character at offset+length is a space,
                    // split there. If not, go back to the last space found.
                    return text.Substring( startIndex, (text[startIndex + length] == ' ' ? length : lastSpace));
            }

            // If the current character is a new line.
            else if (text[startIndex + length] == '\n')
                return text.Substring( startIndex, length);

            // Check if the current character is a space.
            if (text[startIndex + length] == ' ')
            {
                // If it is, take note of it.
                lastSpace = length;

                // If the character is a split point 
                if (Wordsmith.Configuration.SplitPointDefinitions.Contains(text[startIndex + length - 1]))
                    lastSentence = length;

                // If there are more characters previous
                else if ( startIndex + length -2 >= 0)
                {
                    // Check if we have a case of encapsulation like (Hello.)
                    if(Wordsmith.Configuration.EncapsulationCharacters.Contains(text[startIndex + length-1]))
                    {
                        // If the character is a split point 
                        if (Wordsmith.Configuration.SplitPointDefinitions.Contains(text[startIndex + length - 2]))
                            lastSentence = length;
                    }
                }
            }
        }

        // If we make it here, the remaining string from offset to end of string is all
        // all within the given byte limit so return the remaining substring.
        return text[startIndex..^0];
    }
}
