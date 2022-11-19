namespace Wordsmith.Helpers;

internal class ChatHelper: IReflected
{
    private const int _safety = 10;
    /// <summary>
    /// Takes inputs and returns it as a collection of strings that are ready to be sent, all under 500 bytes.
    /// </summary>
    /// <param name="header">The header to place at the front of each string (i.e. /tell Player Name@World)</param>
    /// <param name="text">The text to be conferted into strings.</param>
    /// <returns>Returns an array of strings, all under 500 bytes to prepare for sending.</returns>
    internal static List<TextChunk>? FFXIVify(HeaderData header, string text, bool OOC)
    {
        try
        {
            UTF8Encoding encoder = new();

            // Get the number of bytes taken by the header.
            // We then cut the bytes out required for the safety zone, header, continuation marker, and OOC tags.
            int availableBytes = 480 - _safety - encoder.GetByteCount($"{header} ") - encoder.GetByteCount(Wordsmith.Configuration.ContinuationMarker);

            // If the user has enabled OOC then subtract the byte length of the opening and closing tags
            // from the available byte length.
            if ( OOC )
            {
                int oLen = encoder.GetByteCount( Wordsmith.Configuration.OocOpeningTag );
                int cLen = encoder.GetByteCount( Wordsmith.Configuration.OocClosingTag );
                availableBytes -= oLen + cLen;
            }

            // Create a list to hold all of our strings.
            List<TextChunk> results = new();

            // Break the string into smaller sizes.
            // offset will be adjusted to the end of the previous string with each iteration.
            int offset = 0;
            while ( offset < text.Length )
            {
                // Get the current possible string.
                string? substring = SubstringByByteCount(text, offset, availableBytes);

                // If the string comes back null, throw an error.
                if ( substring == null )
                    throw new Exception( "substring is null" );

                string str = substring;

                // Add the string to the list with the header and, if offset is not at
                // the end of the string yet, add the continuation marker for the player.
                if ( str.Trim().Length > 0 )
                    results.Add( new( str.Trim() )
                    {
                        // The StartIndex is adjusted here because if there was white space
                        // trimmed from the string, we want to eliminate it from the chunk
                        // text.
                        StartIndex = offset + (str.Length - str.TrimStart().Length),
                        Header = header.ToString(),
                        OutOfCharacterStartTag = OOC ? Wordsmith.Configuration.OocOpeningTag : "",
                        OutOfCharacterEndTag = OOC ? Wordsmith.Configuration.OocClosingTag : ""
                    } );

                // Add the length of the string to the offset.
                offset += str.Length;
            }

            // If there is more than one result we want to do continuation markers
            if ( results.Count > 1 )
            {
                // Iterate through the chunks and append the continuation marker. We have to do this
                // in a separate loop from when we created the chunks in case the user adds the #m tag
                // to their continuation marker and we need to know the total number of chunks.
                for ( int i = 0; i < (Wordsmith.Configuration.ContinuationMarkerOnLast ? results.Count : results.Count - 1); ++i )
                    results[i].ContinuationMarker = Wordsmith.Configuration.ContinuationMarker.Replace( "#c", (i + 1).ToString() ).Replace( "#m", results.Count.ToString() );
            }

            // Return the results.
            return results;
        }
        
        catch ( Exception e )
        {
            Dictionary<string, object> dump = new();
            dump["Type"] = typeof( ChatHelper );
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ChatHelper Exception##ErrorWindow{DateTime.Now}" );
        }
        return null;
    }

    /// <summary>
    /// Gets a substring based on maximum number of bytes allowed.
    /// </summary>
    /// <param name="text">Text to get the substring from</param>
    /// <param name="startIndex">The starting index of the substring.</param>
    /// <param name="byteLimit">The maximum byte length of the return.</param>
    /// <exception cref="IndexOutOfRangeException">Offset is out of range of text.</exception>
    /// <returns>A string that is under the byte limit.</returns>
    protected static string? SubstringByByteCount( string text, in int startIndex, in int byteLimit )
    {
        try
        {
            // If the offset is out of index, throw an out of range exception
            if ( startIndex >= text.Length )
                throw new IndexOutOfRangeException();

            // Designate a text encoder so we don't reinitialize a new one every time.
            UTF8Encoding encoder = new UTF8Encoding();

            // Create a variable to hold the last known space and last known sentence marker
            int lastSpace = -1;
            int lastSentence = -1;

            // Start with a character length of 1 and try increasing lengths.
            for ( int length = 1; length + startIndex < text.Length; ++length )
            {
                // If the current length would be over the byte limit
                if ( encoder.GetByteCount( text.Substring( startIndex, length ) ) > byteLimit )
                {
                    // reduce the length by one as we've officially crossed the maximum byte count.
                    --length;

                    // If we never found a space, we'll have to split the string at length regardless.
                    if ( lastSpace == -1 )
                        lastSpace = length;

                    if ( Wordsmith.Configuration.SplitTextOnSentence && lastSentence > 0 && lastSentence > startIndex )
                        return text.Substring( startIndex, lastSentence );
                    else
                        // get the substring starting from offset. If the character at offset+length is a space,
                        // split there. If not, go back to the last space found.
                        return text.Substring( startIndex, (text[startIndex + length] == ' ' ? length : lastSpace) );
                }

                // If the current character is a new line.
                else if ( text[startIndex + length] == '\n' )
                    return text.Substring( startIndex, length );

                // Check if the current character is a space.
                if ( text[startIndex + length] == ' ' )
                {
                    // If it is, take note of it.
                    lastSpace = length;

                    // If the character is a split point 
                    if ( Wordsmith.Configuration.SentenceTerminators.Contains( text[startIndex + length - 1] ) )
                        lastSentence = length;

                    // If there are more characters previous
                    else if ( startIndex + length - 2 >= 0 )
                    {
                        // Check if we have a case of encapsulation like (Hello.)
                        if ( Wordsmith.Configuration.EncapsulationTerminators.Contains( text[startIndex + length - 1] ) )
                        {
                            // If the character is a split point 
                            if ( Wordsmith.Configuration.SentenceTerminators.Contains( text[startIndex + length - 2] ) )
                                lastSentence = length;
                        }
                    }
                }
            }

            // If we make it here, the remaining string from offset to end of string is all
            // all within the given byte limit so return the remaining substring.
            return text[startIndex..^0];
        }
        catch ( Exception e )
        {
            Dictionary<string, object> dump = new();
            dump["Type"] = typeof( ChatHelper );
            dump["Exception"] = new Dictionary<string, string>()
            {
                { "Error", e.ToString() },
                { "Message", e.Message }
            };
            WordsmithUI.ShowErrorWindow( dump, $"ChatHelper Exception##ErrorWindow{DateTime.Now}" );
        }
        return text;
    }
}
