using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using ImGuiNET;

namespace Wordsmith;

internal static class Extensions
{
    /// <summary>
    /// Capitalizes the first letter in a string.
    /// </summary>
    /// <param name="s">The string to capitalize the first letter of.</param>
    /// <returns>A <see cref="string"/> with the first letter capitalized.</returns>
    internal static string CaplitalizeFirst( this string s )
    {
        // If the length is one, just change the char and send it back.
        if ( s.Length == 1 )
            return char.ToUpper( s[0] ).ToString();

        // If the length is greater than 1, capitalize the first char and
        // get the remaining substring to lower.
        else if ( s.Length > 1 )
            return char.ToUpper( s[0] ).ToString() + s.Substring( 1 ).ToLower();

        // If we reach this return, the string is empty, return as-is.
        return s;
    }

    /// <summary>
    /// Spaces a string by capital letters. Useful for adding spaces to PascalCasing
    /// </summary>
    /// <param name="s">String to space.</param>
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
    /// Removes all double spaces from a string.
    /// </summary>
    /// <param name="s">The string to remove double spaces from.</param>
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
    /// Removes all double spaces from a string.
    /// </summary>
    /// <param name="s">The string to remove double spaces from.</param>
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
    /// Unwraps the text string using the spaced and no space markers.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to be unwrapped.</param>
    /// <returns><see cref="string"/> with all wrap markers replaced.</returns>
    internal static string Unwrap( this string s ) => s.Trim().Replace( Global.SPACED_WRAP_MARKER + "\n", " " ).Replace( Global.NOSPACE_WRAP_MARKER + "\n", "" );

    internal static string Wrap( this string text, float width )
    {
        int _ = 0;
        return text.Wrap( width, ref _ );
    }
    /// <summary>
    /// Wraps the text string based on the current width of the window.
    /// </summary>
    /// <param name="text">The string to be wrapped.</param>
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
        while ( text.Contains( Global.SPACED_WRAP_MARKER + '\n' ) )
        {
            int idx = text.IndexOf(Global.SPACED_WRAP_MARKER + '\n');
            text = text[0..idx] + " " + text[(idx + (Global.SPACED_WRAP_MARKER + '\n').Length)..^0];

            // We adjust the cursor position by one less than the wrap marker
            // length to account for the space that replaces it.
            if ( cursorPos > idx )
                cursorPos -= Global.SPACED_WRAP_MARKER.Length;

        }

        // Replace all non-spaced wrap markers with an empty zone.
        while ( text.Contains( Global.NOSPACE_WRAP_MARKER + '\n' ) )
        {
            int idx = text.IndexOf(Global.NOSPACE_WRAP_MARKER + '\n');
            text = text[0..idx] + text[(idx + (Global.NOSPACE_WRAP_MARKER + '\n').Length)..^0];

            if ( cursorPos > idx )
                cursorPos -= (Global.NOSPACE_WRAP_MARKER + '\n').Length;
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
                    sb.Insert( lastSpace, Global.SPACED_WRAP_MARKER + '\n' );
                    offset = lastSpace + Global.SPACED_WRAP_MARKER.Length;
                    i += Global.SPACED_WRAP_MARKER.Length;

                    // Adjust cursor position for the marker but not
                    // the new line as the new line is replacing the space.
                    if ( lastSpace < cursorPos )
                        cursorPos += Global.SPACED_WRAP_MARKER.Length;
                }
                else
                {
                    sb.Insert( i, Global.NOSPACE_WRAP_MARKER + '\n' );
                    offset = i + Global.NOSPACE_WRAP_MARKER.Length;
                    i += Global.NOSPACE_WRAP_MARKER.Length;

                    // Adjust cursor position for the marker and the
                    // new line since both are inserted.
                    if ( cursorPos > i - Global.NOSPACE_WRAP_MARKER.Length )
                        cursorPos += Global.NOSPACE_WRAP_MARKER.Length + 1;
                }
                text = sb.ToString();
            }
        }
        return text;
    }

    /// <summary>
    /// Takes a string and attempts to collect all of the words inside of it.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns><see cref="Word"/> array containing all words in the string.</returns>
    internal static Word[] Words( this string s )
    {
        s = s.Unwrap();
        if ( s.Length == 0 )
            return Array.Empty<Word>();

        List<Word> words = new();

        int start = 0;
        int len = 1;
        while ( start + len <= s.Length )
        {
            // Scoot the starting point until we've skipped all spaces, return carriage, and newline characters.
            while ( start < s.Length && " \r\n".Contains( s[start] ) )
                ++start;

            // If the start has gone all the way to the end, leave the loop.
            if ( start == s.Length )
                break;

            if ( start + len == s.Length || " \r\n".Contains( s[start + len] ) )
            {
                int wordoffset = 0;
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

                    // If the character is not a hyphen, flat it as false.
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
                    WordIndex = start+wordoffset,
                    WordLength = len-wordlenoffset,
                    HyphenTerminated = hyphen
                };

                words.Add( w );
                start += len;
                len = 1;

                continue;
            }
            ++len;
        }
        return words.ToArray();
    }

    internal static string? GetTarget( this string s )
    {
        // Check for a placeholder
        Match match = Regex.Match( s, @"^(?:/tell|/t) (?<target><\w+>)" );
        if ( match.Success )
            return match.Groups["target"].Value;

        // Check for user name@world
        match = Regex.Match( s, @"^(?:/tell|/t) ((?<target><\w+>)|(?<target>[\w']+ [\w']+@\w+))$" );
        if ( match.Success )
                return match.Groups["target"].Value;
        
        return null;
    }

    internal static bool isTarget( this string s )
    {
        // No valid target has less than 3 characters.
        if ( s.Length < 3 )
            return false;

        if ( s.StartsWith( "<" ) && s.EndsWith( ">" ) && !s.Contains( ' ' ) )
            return true;

        // Split the string up.
        if ( s.Split( " " ).Length == 2 && s.Split( ' ' )[1].Contains( '@' ) )
            return true;

        return false;
    }

    /// <summary>
    /// Returns the index of an item in an array.
    /// </summary>
    /// <typeparam name="T">Generic Type</typeparam>
    /// <param name="array">The array to find the object in.</param>
    /// <param name="obj">The object to locate within the array.</param>
    /// <returns><see cref="int"/> index of <typeparamref name="T"/> in array or -1 if not found.</returns>
    internal static int IndexOf<T>( this T[] array, T obj )
    {
        // Iterate and compare each item and return the index if
        // a match is found.
        for ( int i = 0; i < array.Length; ++i )
            if ( array[i]?.Equals( obj ) ?? false )
                return i;

        // If no match is found, return -1 to signal that it isn't in
        // the array.
        return -1;
    }

    internal static IReadOnlyList<(int Type, string Name, string Value)> GetProperties( this IReflected reflected, params string[]? excludes )
    {
        Type t = reflected.GetType();

        List<(int Type, string Name, string Value)> result = new();

        // Get properties
        foreach ( PropertyInfo p in t.GetProperties().Where( x => !excludes?.Contains( x.Name ) ?? true ) )
            result.Add( new( 0, p.Name, GetValueString( p.GetValue( reflected ) ) ) );

        // Get fields
        foreach ( FieldInfo f in t.GetFields( BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static ).Where( x => !excludes?.Contains( x.Name ) ?? true ) )
            result.Add( new( 1, f.Name, GetValueString( f.GetValue( reflected ) ) ) );

        return result;
    }

    private static string GetValueString( object? obj )
    {
        if ( obj is null )
            return "NULL";

        else if ( obj.IsGenericList() )
        {
            System.Collections.ArrayList? arrayList = ReflectListToArrayList( obj );
            if ( arrayList is null )
                return "NULL";

            string result = "{ \"";
            for ( int i = 0; i < arrayList.Count; ++i )
            {
                if ( i > 0 )
                    result += ", \"";
                result += arrayList[i]?.ToString() ?? "NULL";
            }
            result += "\" }";
            return result;
        }

        else if ( obj.IsGenericDictionary())
        {
            List<string>? pairs = ReflectDictionaryToArrayList( obj );
            if ( pairs is not null )
                return "NULL";

            string result = "{ \"";
            for ( int i = 0; i < (pairs?.Count ?? 0); ++i )
            {
                if ( i > 0 )
                    result += ", \"";
                result += pairs?[i] ?? "NULL";
            }
            result += "\" }";
            return result;
        }

        else if ( obj.GetType().IsArray )
        {
            if ( obj is object[] objects)
            {
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
            return "Enumerable";
        }
        return obj.ToString() ?? "NULL";
    }

    public static bool IsGenericList( this object o )
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof( List<> )));
    }

    public static bool IsGenericDictionary( this object o )
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof( Dictionary<,> )));
    }

    public static bool IsGenericEnumerable( this object o )
    {
        var oType = o.GetType();
        return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof( IEnumerable<> )));
    }

    private static System.Collections.ArrayList? ReflectListToArrayList( object o )
    {
        Type t = o.GetType();
        MethodInfo? mi = t.GetMethod("ToArray");
        Array? array = (Array?)mi?.Invoke( o, null );
        return array is null ? null : new(array);
    }

    private static List<string>? ReflectDictionaryToArrayList( object o)
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
        if ( keyArray is null || keyArray.Length == 0)
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
            result.Add( $"{ keyArray.GetValue( i ) }: { valueArray?.GetValue( i ) }" );
        }
        return result;
    }

    internal static Dictionary<string, object> Dump(this IReflected reflected)
    {
        // Get the list of results
        IReadOnlyList<(int Type, string Name, string Value) > data = reflected.GetProperties();

        Dictionary<string, object> result = new Dictionary<string, object>();

        result["Type"] = reflected.GetType().ToString();

        // Get Properties
        foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 0 ) )
            result[Name] = Value;

        // Get Fields
        foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 1 ) )
            result[Name] = Value;

        return result;
    }
}
