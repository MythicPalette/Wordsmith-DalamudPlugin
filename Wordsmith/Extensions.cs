using System.Collections;
using System.Reflection;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;

namespace Wordsmith;

internal static class Extensions
{
    private const string SPACED_WRAP_MARKER = "\r\r";
    private const string NOSPACE_WRAP_MARKER = "\r";

    internal static float Scale( this int i ) => i * ImGuiHelpers.GlobalScale;

    #region Collections
    internal static Dictionary<int, Vector4> Clone( this Dictionary<int, Vector4> dict )
    {
        Dictionary<int, Vector4> result = new();
        foreach ( int key in dict.Keys )
            result[key] = new Vector4( dict[key].X, dict[key].Y, dict[key].Z, dict[key].W );
        return result;
    }
    #endregion

    #region Object
    /// <summary>
    /// Gets the properties and fields of the <see cref="object"/> and creates a list
    /// of them with their type and value
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to get the properties from.</param>
    /// <param name="excludes">A list of properties to exclude by name.</param>
    /// <returns><see cref="IReadOnlyList{T}"/> of <see cref="Tuple{T1, T2, T3}"/> with <see cref="int"/> type, <see cref="string"/> name, <see cref="string"/> value</returns>
    internal static IReadOnlyList<(int Type, string Name, string Value)> GetProperties( this object obj, params string[]? excludes )
    {
        // Get the object's type
        Type t = obj.GetType();

        // Create the resulting list
        List<(int Type, string Name, string Value)> result = new();

        // Get properties of the object and skip any that are in the exclude list.
        foreach ( PropertyInfo p in t.GetProperties().Where( x => !excludes?.Contains( x.Name ) ?? true ) )
            result.Add( new( 0, p.Name, GetValueString( p.GetValue( obj ) ) ) );

        // Get fields of the object and skip any that are in the exclude list.
        foreach ( FieldInfo f in t.GetFields( BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public ).Where( x => !excludes?.Contains( x.Name ) ?? true ) )
            result.Add( new( 1, f.Name, GetValueString( f.GetValue( obj ) ) ) );

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private static string GetValueString( object? obj )
    {
        // If the object is null return an emtpy string.
        if ( obj is null )
            return string.Empty;

        // If the object is a list
        else if ( obj.IsGenericList() )
        {
            // Cast it to an array list
            ArrayList? arrayList = ReflectListObjectToList( obj );

            // If the cast failed then return an empty string
            if ( arrayList is null )
                return string.Empty;

            // Start the resulting string with necessary JSON wrapper
            string result = "{ \"";

            // Convert each item in the list to string
            for ( int i = 0; i < arrayList.Count; ++i )
            {
                if ( i > 0 )
                    result += ", \"";
                result += arrayList[i]?.ToString() ?? string.Empty;
            }

            // End the JSON wrapper
            result += "\" }";

            // Return the result
            return result;
        }

        // If the object is a dictionary
        else if ( obj.IsGenericDictionary() )
        {
            // Cast the dictionary to a list of string
            List<string>? pairs = ReflectDictionaryObjectToList( obj );

            // If the cast fails then return an empty string
            if ( pairs is null )
                return string.Empty;


            // Start the resulting string with necessary JSON wrapper
            string result = "{ \"";

            // Convert each item in the list to string
            for ( int i = 0; i < (pairs?.Count ?? 0); ++i )
            {
                if ( i > 0 )
                    result += ", \"";
                result += pairs![i] ?? string.Empty;
            }

            // End the JSON wrapper
            result += "\" }";

            // Return the result
            return result;
        }

        // If the object is an array
        else if ( obj.GetType().IsArray )
        {
            // Convert it to an array of objects
            if ( obj is object[] objects )
            {
                // Convert all of the objects to strings inside of an array wrapper
                string result = "[ \"";
                for ( int i = 0; i < objects.Length; ++i )
                {
                    if ( i > 0 )
                        result += ", \"";
                    result += objects[i].ToString();
                }
                result += "\" ]";
                return result;
            }
            // In the case that we can't operate on it as an array of object then return
            // just the string "Enumerable" as a last resort.
            return "Enumerable";
        }
        // Lastly, in this case the object seems to not be a collection type so simply
        // return it in its string form or an empty string if that turns out to be null.
        return obj.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Detects if an <see cref="object"/> is a generic list.
    /// </summary>
    /// <param name="o">the <see cref="object"/> to test.</param>
    /// <returns><see langword="true"/> if the object is a list; otherwise <see langword="false"/></returns>
    internal static bool IsGenericList( this object o )
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof( List<> )));
    }

    /// <summary>
    /// Detects if an <see cref="object"/> is a generic Dictionary.
    /// </summary>
    /// <param name="o">the <see cref="object"/> to test.</param>
    /// <returns><see langword="true"/> if the object is a Dictionary; otherwise <see langword="false"/></returns>
    internal static bool IsGenericDictionary( this object o )
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof( Dictionary<,> )));
    }

    /// <summary>
    /// Detects if an <see cref="object"/> is an Enumerable.
    /// </summary>
    /// <param name="o">the <see cref="object"/> to test.</param>
    /// <returns><see langword="true"/> if the object is an Enumerable; otherwise <see langword="false"/></returns>
    internal static bool IsGenericEnumerable( this object o )
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof( IEnumerable<> )));
    }

    /// <summary>
    /// Cast a reflected list as <see cref="object"/> to an ArrayList.
    /// </summary>
    /// <param name="o">The <see cref="object"/> to cast.</param>
    /// <returns>An <see cref="ArrayList"/> if the object can be cast; otherwise <see langword="null"/></returns>
    private static ArrayList? ReflectListObjectToList( object o )
    {
        Type t = o.GetType();
        MethodInfo? mi = t.GetMethod("ToArray");
        Array? array = (Array?)mi?.Invoke( o, null );
        return array is null ? null : new( array );
    }

    /// <summary>
    /// Cast a reflected Dictionary as <see cref="object"/> to a list of string.
    /// </summary>
    /// <param name="o">The <see cref="object"/> to cast.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="string"/> if the object can be cast; otherwise <see langword="null"/></returns>
    private static List<string>? ReflectDictionaryObjectToList( object o )
    {
        Type t = o.GetType();

        // Get the keys
        var keys = t.GetProperty("Keys")?.GetValue(o);

        // Return null if unable to get keys.
        if ( keys is null )
            return null;

        // Convert the keys to an array.
        Type keyCollectionType = keys.GetType();
        MethodInfo? keyToArray = keyCollectionType.GetMethod("ToArray");
        Array? keyArray = (Array?)keyToArray?.Invoke( keys, null );

        // Return null if the array is null or empty
        if ( keyArray is null || keyArray.Length == 0 )
            return null;

        // Get the values
        var values = t.GetProperty("Values")?.GetValue(o);

        // Return null if unable to get values.
        if ( values is null )
            return null;

        // Convert the values to an array.
        Type valueCollectionType = values.GetType();
        MethodInfo? valueToArray = valueCollectionType.GetMethod("ToArray");
        Array? valueArray = (Array?)valueToArray?.Invoke( values, null );


        List<string> result = new();
        for ( int i = 0; i < keyArray.Length; ++i )
        {
            result.Add( $"{keyArray.GetValue( i )}: {valueArray?.GetValue( i )}" );
        }
        return result;
    }

    /// <summary>
    /// Copies all accessible field and property names and values to a dictionary.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to dump</param>
    /// <returns><see cref="Dictionary{TKey, TValue}"/> of <see cref="string"/>, <see cref="object"/> containing all data.</returns>
    internal static Dictionary<string, object> Dump( this object obj )
    {
        // Get the list of results
        IReadOnlyList<(int Type, string Name, string Value) > data = obj.GetProperties();

        Dictionary<string, object> result = new()
        {
            ["Version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            ["Type"] = obj.GetType().ToString()
        };

        // Get Properties
        foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 0 ) )
            result[Name] = Value;

        // Get Fields
        foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 1 ) )
            result[Name] = Value;

        return result;
    }
    #endregion

    #region String
    /// <summary>
    /// Capitalizes the first letter in a <see cref="string"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to capitalize the first letter of.</param>
    /// <returns>A <see cref="string"/> with the first letter capitalized.</returns>
    internal static string CaplitalizeFirst( this string s )
    {
        // If the length is one, just change the char and send it back.
        if ( s.Length == 1 )
            return char.ToUpper( s[0] ).ToString();

        // If the length is greater than 1, capitalize the first char and
        // get the remaining substring to lower.
        else if ( s.Length > 1 )
            return char.ToUpper( s[0] ).ToString() + s[1..].ToLower();

        // If we reach this return, the string is empty, return as-is.
        return s;
    }

    /// <summary>
    /// Spaces a <see cref="string"/> by capital letters. Useful for adding spaces to PascalCasing
    /// </summary>
    /// <param name="s"><see cref="string"/> to space.</param>
    /// <returns>A properly spaced <see cref="string"/></returns>
    internal static string SpaceByCaps( this string s )
    {
        // If there aren't at least two characters then return.
        if ( s.Length < 2 )
            return s;

        string result = s[0].ToString();

        string caps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        // Iterate through each character not in the first index
        for ( int i = 1; i < s.Length; ++i )// char c in s[1..^0])
        {
            // If the character is a capital letter and the letter before
            // it is not then add a space.
            if ( caps.Contains( s[i] ) && !caps.Contains( s[i - 1] ) )
                result += " ";

            // Add the character to the result.
            result += s[i];
        }

        return result;
    }

    /// <summary>
    /// Removes all double spaces from a <see cref="string"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to remove double spaces from.</param>
    /// <returns><see cref="string"/> with double-spacing fixed.</returns>
    internal static string FixSpacing( this string s )
    {
        // Start by initially running the replace command.
        do
        {
            // Replace double spaces.
            s = s.Replace( "  ", " " );

            // Loop because 3 spaces together will only get knocked down
            // to 2 spaces and it won't check again so we need to. With
            // each pass, any area with more than one space will become
            // less spaced until only one remains.
        } while ( s.Contains( "  " ) );

        // Return the correctly spaced string.
        return s;
    }

    /// <summary>
    /// Removes all double spaces from a <see cref="string"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to remove double spaces from.</param>
    /// <param name="cursorPos">A reference to a text cursor to be manipulated.</param>
    /// <returns><see cref="string"/> with double-spacing fixed.</returns>
    internal static string FixSpacing( this string s, ref int cursorPos )
    {
        int idx;
        do
        {
            // Get the position of the first double space.
            idx = s.IndexOf( "  " );
            if ( idx == cursorPos - 1 )
            {
                idx = s[cursorPos..^0].IndexOf( "  " );
                if ( idx > -1 )
                    idx += cursorPos;
            }

            // If the index is greater than -1;
            if ( idx > -1 )
            {
                // If the index is 0 just remove the space from the front of the line.
                if ( idx == 0 )
                    s = s[1..^0];

                // Remove the space from inside the string.
                else
                    s = s[0..idx] + s[(idx + 1)..^0];

                // If the removed space is at a lower index than the cursor
                // move the cursor back a space to account for the position change.
                if ( idx <= cursorPos )
                    cursorPos -= 1;
            }
        } while ( idx > -1 );

        return s;
    }
    
    /// <summary>
    /// Replaces any placeholders in the text with the given values.
    /// </summary>
    /// <param name="s">the <see cref="string"/> to replace text in.</param>
    /// <param name="c">the value to replace #c with.</param>
    /// <param name="m">the value to replace #m with.</param>
    /// <remarks>
    /// #c expects the current number in the collection (one-based)
    /// #m expects the total number of items in the collection
    /// </remarks>
    /// <returns><see cref="string"/> with the placeholders replaced.</returns>
    internal static string ReplacePlaceholders(this string s, int c, int m)
    {
        return s.Replace( "#C", $"{c}" )
            .Replace( "#c", $"{c}" )
            .Replace( "#M", $"{m}" )
            .Replace( "#m", $"{m}" )
            .Replace( "#R", $"{m - c}" )
            .Replace( "#r", $"{m - c}" );
    }

    /// <summary>
    /// Unwraps the text <see cref="string"/> using the spaced and no space markers.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to be unwrapped.</param>
    /// <returns><see cref="string"/> with all wrap markers replaced.</returns>
    internal static string Unwrap( this string s )
    {
        // Replace the markers with the correct information
        s = s.Trim().Replace( SPACED_WRAP_MARKER + "\n", " " ).Replace( NOSPACE_WRAP_MARKER + "\n", "" );

        // Before sending the string back, replace all stray return carriage characters with nothing
        // prevent the text from being poisoned by broken markers.
        return s.Replace( "\r", "" );
    }

    /// <summary>
    /// Wraps the <see cref="string"/> based on the given width.
    /// </summary>
    /// <param name="text">The <see cref="string"/> to be wrapped.</param>
    /// <param name="width">The width of the space the <see cref="string"/> is to be wrapped in.</param>
    /// <returns></returns>
    internal static string Wrap( this string text, float width )
    {
        int _ = 0;
        return text.Wrap( width, ref _ );
    }
    /// <summary>
    /// Wraps the <see cref="string"/> based on the given width and offsets the given cursor position
    /// </summary>
    /// <param name="text">The <see cref="string"/> to be wrapped.</param>
    /// <returns></returns>
    internal static string Wrap( this string text, float width, ref int cursorPos )
    {
        // If the string is empty then just return it.
        if ( text.Length == 0 )
            return text;

        // Trim any return carriages off the end. This can happen if the user
        // backspaces a new line character off of the end.
        text = text.TrimEnd( '\r' );

        // Replace all wrap markers with spaces and adjust cursor offset. Do this before
        // all non-spaced wrap markers because the Spaced marker contains the nonspaced marker
        while ( text.Contains( SPACED_WRAP_MARKER + '\n' ) )
        {
            int idx = text.IndexOf(SPACED_WRAP_MARKER + '\n');
            text = text[0..idx] + " " + text[(idx + (SPACED_WRAP_MARKER + '\n').Length)..^0];

            // We adjust the cursor position by one less than the wrap marker
            // length to account for the space that replaces it.
            if ( cursorPos > idx )
                cursorPos -= SPACED_WRAP_MARKER.Length;

        }

        // Replace all non-spaced wrap markers with an empty zone.
        while ( text.Contains( NOSPACE_WRAP_MARKER + '\n' ) )
        {
            int idx = text.IndexOf(NOSPACE_WRAP_MARKER + '\n');
            text = text[0..idx] + text[(idx + (NOSPACE_WRAP_MARKER + '\n').Length)..^0];

            if ( cursorPos > idx )
                cursorPos -= (NOSPACE_WRAP_MARKER + '\n').Length;
        }

        // Replace all remaining carriage return characters with nothing.
        while ( text.Contains( '\r' ) )
        {
            // Get the index of the character
            int idx = text.IndexOf('\r');

            // Splice the string around the character.
            text = text[0..idx] + text[(idx + 1)..^0];

            // If the cursor is behind the edit, move the cursor with the edited text.
            if ( cursorPos > idx )
                cursorPos -= 1;
        }

        // Replace double spaces if configured to do so.
        if ( Wordsmith.Configuration.ReplaceDoubleSpaces )
            text = text.FixSpacing( ref cursorPos );

        // Get the maximum allowed character width.
        //float width = this._lastWidth - (35 * ImGuiHelpers.GlobalScale);

        // Iterate through each character.
        int lastSpace = 0;
        int offset = 0;
        for ( int i = 1; i < text.Length; ++i )
        {
            // If the current character is a space, mark it as a wrap point.
            if ( text[i] == ' ' )
                lastSpace = i;

            // If the size of the text is wider than the available size
            float txtWidth = ImGui.CalcTextSize(text[offset..i ]).X;
            if ( txtWidth + 10 * ImGuiHelpers.GlobalScale > width )
            {
                // Replace the last previous space with a new line
                StringBuilder sb = new(text);

                if ( lastSpace > offset )
                {
                    sb.Remove( lastSpace, 1 );
                    sb.Insert( lastSpace, SPACED_WRAP_MARKER + '\n' );
                    offset = lastSpace + SPACED_WRAP_MARKER.Length;
                    i += SPACED_WRAP_MARKER.Length;

                    // Adjust cursor position for the marker but not
                    // the new line as the new line is replacing the space.
                    if ( lastSpace < cursorPos )
                        cursorPos += SPACED_WRAP_MARKER.Length;
                }
                else
                {
                    sb.Insert( i, NOSPACE_WRAP_MARKER + '\n' );
                    offset = i + NOSPACE_WRAP_MARKER.Length;
                    i += NOSPACE_WRAP_MARKER.Length;

                    // Adjust cursor position for the marker and the
                    // new line since both are inserted.
                    if ( cursorPos > i - NOSPACE_WRAP_MARKER.Length )
                        cursorPos += NOSPACE_WRAP_MARKER.Length + 1;
                }
                text = sb.ToString();
            }
        }
        return text;
    }

    /// <summary>
    /// Takes a <see cref="string"/> and attempts to collect all of the words inside of it.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to parse.</param>
    /// <returns><see cref="Word"/> array containing all words in the <see cref="string"/>.</returns>
    internal static List<Word> Words( this string s )
    {
        if ( s.Length == 0 )
            return new();

        List<Word> words = new();

        // The start of the current word
        int start = 0;

        // The end of the current word
        int len = 1;

        while ( start + len <= s.Length )
        {
            // Scoot the starting point until we've skipped all spaces, return carriage, and newline characters.
            while ( start < s.Length && " \r\n".Contains( s[start] ) )
                ++start;

            // If the start has gone all the way to the end, leave the loop.
            if ( start == s.Length )
                break;

            // If the word finishes the string or it contains a whitespace character
            if ( start + len == s.Length || " \r\n".Contains( s[start + len] ) )
            {
                // Where the word starts compared to included punctuation
                int wordoffset = 0;

                // The length of the word offset.
                int wordlenoffset = 0;

                // If the word starting index is a punctuation character then we scoot the word offset forward up to the entire
                // length of the current string.
                while ( start + wordoffset < s.Length && Wordsmith.Configuration.PunctuationCleaningList.Contains( s[start + wordoffset] ) && wordoffset <= len )
                    ++wordoffset;

                // Default to false hyphen termination.
                bool hyphen = false;

                // If the word ends with a punctuation character then we scoot the word offset left up to the point that
                // the offset puts us at -1 word length. This will happen when the word has no letters.
                while ( start + len - wordlenoffset - 1 > -1 && $"-{Wordsmith.Configuration.PunctuationCleaningList}".Contains( s[start + len - wordlenoffset - 1] ) && wordoffset <= len )
                {
                    // If the character is a hyphen, flag it as true.
                    if ( s[start + len - wordlenoffset - 1] == '-' )
                        hyphen = true;

                    // If the character is not a hyphen, flag it as false.
                    else
                        hyphen = false;

                    ++wordlenoffset;
                }

                // Add the start offset to the len offset to account for the lost length at the start.
                wordlenoffset += wordoffset;

                // When we create the word we add the offset to ensure that we
                // adjust the position as needed.
                Word w = new()
                {
                    StartIndex = start,
                    EndIndex = start + len,
                    WordIndex = len-wordlenoffset > 0 ? start+wordoffset : 0,
                    WordLength = len-wordlenoffset > 0 ? len-wordlenoffset : 0,
                    HyphenTerminated = hyphen
                };

                words.Add( w );
                start += len;
                len = 1;

                continue;
            }
            ++len;
        }
        return words;
    }

    /// <summary>
    /// Parses a chat target from the <see cref="string"/>.
    /// </summary>
    /// <param name="s">the <see cref="string"/> to parse</param>
    /// <returns>A <see cref="string"/> containing the target if able; otherwise <see langword="null"/></returns>
    internal static string? GetTarget( this string s )
    {
        // Check for a placeholder
        Match match = Regex.Match( s, @"^(?:/tell|/t) (?<target><\w+>)" );
        if ( match.Success )
            return match.Groups["target"].Value;

        // Check for user name@world
        match = Regex.Match( s, @"^(?:/tell|/t) (?<target>[A-Z][a-zA-Z']{1,14} [A-Z][a-zA-Z']{1,14}@[A-Z][a-z]{3,13})$" );
        if ( match.Success )
                return match.Groups["target"].Value;
        
        return null;
    }

    /// <summary>
    /// Determines if the <see cref="string"/> is a valid target for /Tell
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    internal static bool isTarget( this string s )
    {
        // No valid target has less than 3 characters.
        if ( s.Length < 3 )
            return false;

        // If the target is a valid User Name@World
        if ( Regex.Match( s, @"^[A-Z][a-zA-Z']{1,14} [A-Z][a-zA-Z']{1,14}@[A-Z][a-z]{3,13}$" ).Success )
            return true;

        // If the target is a placeholder.
        if ( Regex.Match(s, @"^\<\w+\>$").Success )
            return true;

        // Return false when matches fail.
        return false;
    }
    #endregion
}

/// <summary>
/// A class for ImGuiNET wrappers and extensions.
/// </summary>
internal static class ImGuiExt
{
    /// <summary>
    /// Wraps <see cref="ImGui.SetTooltip(string)"/> in an <see langword="if"/> <see cref="ImGui.IsItemHovered()"/> check
    /// </summary>
    /// <param name="tooltip"><see cref="string"/> tooltip message.</param>
    internal static void SetHoveredTooltip( string tooltip )
    {
        if ( ImGui.IsItemHovered() )
            ImGui.SetTooltip( tooltip );
    }
}
