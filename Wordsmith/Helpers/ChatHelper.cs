using System;

namespace Wordsmith.Helpers;

internal sealed class ChatHelper
{
    /// <summary>
    /// Buffer zone to protect the size of the text.
    /// </summary>
    private const int _safety = 10;
    /// <summary>
    /// Takes inputs and returns it as a collection of strings that are ready to be sent, all under 500 bytes.
    /// </summary>
    /// <param name="header">The header to place at the front of each string (i.e. /tell Player Name@World)</param>
    /// <param name="text">The text to be conferted into strings.</param>
    /// <returns>Returns an array of strings, all under 500 bytes to prepare for sending.</returns>
    internal static List<TextChunk>? FFXIVify(HeaderData header, string text, bool OOC)
    {
        UTF8Encoding encoder = new();

        // Get the number of bytes taken by the header.
        // We then cut the bytes out required for the safety zone, header, continuation marker, and OOC tags.
        int iMaxByteWidth = 480 - _safety - encoder.GetByteCount($"{header} ") - encoder.GetByteCount($"{Wordsmith.Configuration.ContinuationMarker}");

        // If the user has enabled OOC then subtract the byte length of the opening and closing tags
        // from the available byte length.
        if ( OOC )
        {
            int oLen = encoder.GetByteCount( Wordsmith.Configuration.OocOpeningTag );
            int cLen = encoder.GetByteCount( Wordsmith.Configuration.OocClosingTag );
            iMaxByteWidth -= oLen + cLen;
        }

        // Create a new dictionary for all of the chunk markers.
        Dictionary<RepeatMode, List<ChunkMarker>> dMarkers = new();
        foreach ( RepeatMode opt in Enum.GetValues( typeof( RepeatMode ) ) )
            dMarkers[opt] = new();

        // Sort all of the chunk markers by repeat option.
        foreach ( ChunkMarker cm in Wordsmith.Configuration.ChunkMarkers )
            dMarkers[cm.RepeatMode].Add(cm);

        // Subtract the "always on" markers from the byte width. Due to the complexity and taxing
        // nature of retroactively calculating in the OnlyOnLast for just the last chunk it will
        // be considered "always on" in this calculation.
        iMaxByteWidth -= GetMarkerByteLength( dMarkers[RepeatMode.All] ) +
                GetMarkerByteLength( dMarkers[RepeatMode.AllExceptFirst] ) +
                GetMarkerByteLength( dMarkers[RepeatMode.AllExceptLast] ) +
                GetMarkerByteLength( dMarkers[RepeatMode.OnlyOnLast] );

        // Create a list to hold all of our chunks.
        List<TextChunk> results = new();

        // Break the string into smaller sizes.
        // offset will be adjusted to the end of the previous string with each iteration.
        int offset = 0;
        int index = 0;
        while ( offset < text.Length )
        {
            // Increase the offset when the starting character is not usable.
            if ( " \r\n".Contains( text[offset] ) )
            {
                offset++;
                continue;
            }

            // Get all of the default marker width
            int iMarkerWidth = 0;

            // If this is the first chunk then include all first chunk markers.
            if ( results.Count == 0 )
                iMarkerWidth += GetMarkerByteLength( dMarkers[RepeatMode.OnlyOnFirst] ) - GetMarkerByteLength( dMarkers[RepeatMode.AllExceptFirst] );

            // Get a list of nth markers that apply to this marker and go from there.
            iMarkerWidth += GetMarkerByteLength( dMarkers[RepeatMode.EveryNth].Where( x =>
                (index == 0 && x.StartPosition == 1 ) ||
                (index > 0 && index % x.Nth == 0)).ToList() );

            if ( iMaxByteWidth - iMarkerWidth < 1 )
                throw new Exception( "Too many markers. Unable to fit text body." );

            // Get the current possible string.
            string? substring = GetSubstringByByteCount(text, offset, iMaxByteWidth - iMarkerWidth);

            // If the string comes back null, throw an error.
            if ( substring == null )
                throw new Exception( "substring is null" );

            if ( substring.Length == 0 )
                throw new Exception( "Failed to retrieve a valid substring." );

            string str = substring;

            // Add the string to the list with the header and, if offset is not at
            // the end of the string yet, add the continuation marker for the player.
            if ( str != "\n" && str.Trim().Length > 0 )
                results.Add( new( str.Trim() )
                {
                    // The StartIndex is adjusted here because if there was white space
                    // trimmed from the string, we want to eliminate it from the chunk
                    // text.
                    StartIndex = offset + (str.Length - str.TrimStart().Length),
                    Header = header.ToString(),
                    OutOfCharacterStartTag = OOC ? Wordsmith.Configuration.OocOpeningTag : "",
                    OutOfCharacterEndTag = OOC ? Wordsmith.Configuration.OocClosingTag : "",
                    ContinuationMarker = Wordsmith.Configuration.ContinuationMarker
                } );

            // Add the length of the string to the offset.
            offset += str.Length;
            index++;
        }

        // Return the results.
        return results;
    }

    /// <summary>
    /// Gets a substring based on maximum number of bytes allowed.
    /// </summary>
    /// <param name="text">Text to get the substring from</param>
    /// <param name="startIndex">The starting index of the substring.</param>
    /// <param name="byteLimit">The maximum byte length of the return.</param>
    /// <exception cref="IndexOutOfRangeException">Offset is out of range of text.</exception>
    /// <returns>A string that is under the byte limit.</returns>
    private static string? GetSubstringByByteCount( string text, in int startIndex, in int byteLimit )
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
            string substring = text.Substring( startIndex, length );

            // If the current character is a new line.
            if ( text[startIndex + length] == '\n' )
                return substring;

            // Check if the current character is a space.
            if ( text[startIndex + length] == ' ' )
            {
                // If it is, lastSpace is at the current length
                lastSpace = length;

                // If the previous character is a sentence terminator mark the last sentence.
                if ( Wordsmith.Configuration.SentenceTerminators.Contains( text[startIndex + length - 1] ) )
                    lastSentence = length;
            }

            // If the current length would be over the byte limit
            if ( encoder.GetByteCount( substring ) > byteLimit )
            {
                // reduce the length by one as we've officially crossed the maximum byte count.
                --length;

                // If we never found a space, we'll have to split the string at length regardless.
                if ( lastSpace == -1 )
                    lastSpace = length;

                if ( Wordsmith.Configuration.SplitTextOnSentence && lastSentence > 0 )
                    return text.Substring( startIndex, lastSentence );
                else
                    // get the substring starting from offset. If the character at offset+length is a space,
                    // split there. If not, go back to the last space found.
                    return text.Substring( startIndex, (text[startIndex + length] == ' ' ? length : lastSpace) );
            }
        }

        // If we make it here, the remaining string from offset to end of string is all
        // all within the given byte limit so return the remaining substring.
        return text[startIndex..^0];
    }

    private static int GetMarkerByteLength(List<ChunkMarker> lMarkers) => new UTF8Encoding().GetByteCount( $" {(string.Join(' ', lMarkers.Select(x => x.Text)))}" );
}
