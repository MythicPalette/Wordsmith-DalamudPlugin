using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Logging;

namespace Wordsmith.Helpers
{
    public class ChatHelper
    {
        /// <summary>
        /// Takes inputs and returns it as a collection of strings that are ready to be sent, all under 500 bytes.
        /// </summary>
        /// <param name="header">The header to place at the front of each string (i.e. /tell Player Name@World)</param>
        /// <param name="text">The text to be conferted into strings.</param>
        /// <returns>Returns an array of strings, all under 500 bytes to prepare for sending.</returns>
        public static string[] FFXIVify(string header, string text, bool OOC)
        {
            UTF8Encoding encoder = new();

            // Get the number of bytes taken by the header.
            // Note that we remove 5 bytes right off of the top as a buffer zone.
            // We then cut the bytes out required for the header and for the continuation marker.
            int availableBytes = 495 - encoder.GetByteCount($"{header} ") - encoder.GetByteCount(Wordsmith.Configuration.ContinuationMarker);

            // If the user is typing into OOC, remove 6 more bytes from the available bytes for the (( )) tags.
            if (OOC)
                availableBytes -= 6;

            // Create a list to hold all of our strings.
            List<string> results = new();

            // Break the string into smaller sizes.
            int offset = 0;
            while (offset < text.Length)
            {
                // Get the current possible string.
                string str = SubstringByByteCount(text, offset, availableBytes);

                // Add the length of the string to the offset.
                offset += str.Length;


                // Add the string to the list with the header and, if offset is not at
                // the end of the string yet, add the continuation marker for the player.
                //results.Add($"{header} {(OOC ? "(( " : "")}{str}{(OOC ? " ))" : "")}{(offset < text.Length ? " (c)" : "")}");
                results.Add($"{header} {(OOC ? "(( " : "")}{str}{(OOC ? " ))" : "")}");
            }
            for (int i = 0; i < (Wordsmith.Configuration.MarkLastChunk ? results.Count : results.Count - 1); ++i)
                results[i] += Wordsmith.Configuration.ContinuationMarker.Replace("#c", (i+1).ToString()).Replace("#m", results.Count.ToString());

            return results.ToArray();
        }

        /// <summary>
        /// Gets a substring based on maximum number of bytes allowed.
        /// </summary>
        /// <param name="text">Text to get the substring from</param>
        /// <param name="offset">The starting index of the substring.</param>
        /// <param name="byteLimit">The maximum byte length of the return.</param>
        /// <exception cref="IndexOutOfRangeException">Offset is out of range of text.</exception>
        /// <returns>A string that is under the byte limit.</returns>
        protected static string SubstringByByteCount(string text, int offset, int byteLimit)
        {
            // If the offset is out of index, throw an out of range exception
            if (offset >= text.Length)
                throw new IndexOutOfRangeException();

            // Designate a text encoder so we don't reinitialize a new one every time.
            UTF8Encoding encoder = new UTF8Encoding();

            // Iterate over all of the characters after the offset. i=offset+1 so that the
            // length is a minimum of 1 on the first comparison. Last space is where the string
            // should be broken at for readability.
            int lastSpace = -1;
            int lastSentence = -1;
            for (int length=1; length+offset<text.Length; ++length)
            {
                // If the current length would be over the byte limit
                if (encoder.GetByteCount(text.Substring(offset, length)) > byteLimit)
                {
                    // reduce the length by one.
                    --length;

                    // If we never found a space, we'll have to split the string at length regardless.
                    if (lastSpace == -1)
                        lastSpace = length;

                    if (Wordsmith.Configuration.BreakOnSentence && lastSentence > 0)
                        return text.Substring(offset, lastSentence);
                    else
                        // get the substring starting from offset. If the character at offset+length is a space,
                        // split there. If not, go back to the last space found.
                        return text.Substring(offset, (text[offset+length] == ' ' ? length : lastSpace));
                }

                // Check if the current character is a space.
                if (text[offset + length] == ' ')
                {
                    // If it is, take note of it.
                    lastSpace = length;

                    // If the character is a split point 
                    if (Wordsmith.Configuration.SplitPointDefinitions.Contains(text[offset + length - 1]))
                        lastSentence = length;

                    // If there are more characters previous
                    else if (offset+length -2 >= 0)
                    {
                        // Check if we have a case of encapsulation like (Hello.)
                        if(Wordsmith.Configuration.EncapsulationCharacters.Contains(text[offset+length-1]))
                        {
                            // If the character is a split point 
                            if (Wordsmith.Configuration.SplitPointDefinitions.Contains(text[offset + length - 2]))
                                lastSentence = length;
                        }
                    }
                }
            }

            // If we make it here, the remaining string from offset to end of string is all
            // all within the given byte limit so return the remaining substring.
            return text.Substring(offset);
        }
    }
}
