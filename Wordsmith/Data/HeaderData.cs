using Wordsmith.Enums;

namespace Wordsmith.Data;

internal struct HeaderData
{
    public ChatType ChatType { get; set; } = ChatType.None;
    public int Linkshell { get; set; } = 0;
    public bool CrossWorld { get; set; } = false;
    public string TellTarget { get; set; } = "";
    public string Headstring { get; set; } = "";
    public int Length { get => Headstring.Length; }
    public bool Valid { get => this.Length > 0; }

    public HeaderData(string headstring)
    {
        // If it doesn't start with a slash, we don't even try.
        if ( !headstring.StartsWith( '/' ) )
            return;

        // Find a matching header by default.
        for ( int i = 0; i < Enum.GetValues( typeof( ChatType ) ).Length; ++i )
        {
            string hstring = "";
            // If the string starts with the header for the chat type then mark it
            if ( headstring.StartsWith( $"{((ChatType)i).GetShortHeader()} " ) )
                hstring = $"{((ChatType)i).GetShortHeader()} ";
            else if ( headstring.StartsWith( $"{((ChatType)i).GetShortHeader()}\n" ) )
                hstring = $"{((ChatType)i).GetShortHeader()}\n";
            else if ( headstring.StartsWith( $"{((ChatType)i).GetLongHeader()} " ) )
                hstring = $"{((ChatType)i).GetLongHeader()} ";
            else if ( headstring.StartsWith( $"{((ChatType)i).GetLongHeader()}\n" ) )
                hstring = $"{((ChatType)i).GetLongHeader()}\n";

            // if the header type is linkshell or CrossWorldLinkshell
            else if ( (ChatType)i == ChatType.Linkshell || (ChatType)i == ChatType.CrossWorldLinkshell )
            {
                // Linkshells must be checked with numbers appended to them.
                for ( int x = 1; x <= 8; ++x )
                {
                    // If the number was found, mark the header type and the number.
                    if ( headstring.StartsWith( $"{((ChatType)i).GetShortHeader()}{x} " ) )
                    {
                        this.ChatType = ChatType.Linkshell;
                        this.Linkshell = x-1;
                        this.Headstring = $"{((ChatType)i).GetShortHeader()}{x} ";
                        return;
                    }
                }

                // This continue prevents code from running after failing to identify the linkshell.
                continue;
            }
            // No matches and not checking linkshells, skip the following code and loop again
            else
                continue;

            // Neither of these two types should be allowed passed this point.
            if ( i == (int)ChatType.Linkshell )
                continue;
            if ( i == (int)ChatType.CrossWorldLinkshell )
                continue;

            // If there is a header to fit with the chat type.
            if ( ((ChatType)i).GetShortHeader().Length > 0 )
            {
                // Set the chat type
                this.ChatType = (ChatType)i;

                // Set the headstring.
                this.Headstring = hstring;

                // Break from the loop
                break;
            }
        }

        // No match was found.
        if ( this.ChatType == ChatType.None )
        {
            // For each alias
            foreach ( (int id, string alias, object? data) in Wordsmith.Configuration.HeaderAliases )
            {
                // If a matching alias is found
                if ( headstring.StartsWith( $"/{alias} " ) || headstring.StartsWith($"/{alias}\n") )
                {
                    // If the ID for the alias is Tell
                    if ( id == (int)ChatType.Tell && data == null )
                    {
                        this.Headstring = $"/{alias} ";
                        this.ChatType = ChatType.Tell;
                    }
                    else if ( id == (int)ChatType.Tell && data is string dataString )
                    {
                        this.Headstring = $"/{alias} ";
                        this.ChatType = ChatType.Tell;
                        this.TellTarget = dataString;
                        return;
                    }

                    // If it isn't /Tell and the ChatType is within normal range
                    // simply assign the chat type.
                    else if ( id < (int)ChatType.Linkshell )
                    {
                        // Assign the chat type and break from the loop.
                        this.ChatType = (ChatType)id;
                        this.Headstring = $"/{alias} ";
                        return;
                    }

                    else
                    {
                        // Set the chat type to Linkshell
                        this.ChatType = ChatType.Linkshell;

                        // Get the linkshell number.
                        this.Linkshell = (id - (int)ChatType.Linkshell) % 8;

                        // Determine if the linkshell is crossworld
                        this.CrossWorld = (id - (int)ChatType.Linkshell) >= 8;

                        // Set the HeadString property.
                        this.Headstring = $"/{alias} ";
                    }
                    break;
                }
            }
        }

        if ( this.ChatType == ChatType.Tell )
        {
            string target = headstring.GetTarget() ?? "";
            // If a target was found
            if ( target != "" )
            {
                this.Headstring += $" {target}";
                this.ChatType = ChatType.Tell;
                this.TellTarget = target;
            }
            else // Erase the headstring to invalidate the text.
                this.Headstring = "";
        }
    }
}
