namespace Wordsmith.Enums;

internal enum ChatType
{
    None,
    Emote,
    Reply,
    Say,
    Party,
    FC,
    Shout,
    Yell,
    Tell,
    Echo,
    Linkshell,
    CrossWorldLinkshell
}

internal static class ChatTypeExtensions
{
    internal static string GetShortHeader(this ChatType c)
    {
        switch (c)
        {
            case ChatType.Emote:
                return "/em";
            case ChatType.Reply:
                return "/r";
            case ChatType.Say:
                return "/s";
            case ChatType.Party:
                return "/p";
            case ChatType.FC:
                return "/fc";
            case ChatType.Shout:
                return "/sh";
            case ChatType.Yell:
                return "/y";
            case ChatType.Tell:
                return "/t";
            case ChatType.Echo:
                return "/e";
            case ChatType.Linkshell:
                return "/linkshell";
            case ChatType.CrossWorldLinkshell:
                return "/cwlinkshell";
            default:
                return "";
        }
    }

    internal static string GetLongHeader(this ChatType c)
    {
        switch (c)
        {
            case ChatType.Emote:
                return "/emote";
            case ChatType.Reply:
                return "/reply";
            case ChatType.Say:
                return "/say";
            case ChatType.Party:
                return "/party";
            case ChatType.FC:
                return "/freecompany";
            case ChatType.Shout:
                return "/shout";
            case ChatType.Yell:
                return "/yell";
            case ChatType.Tell:
                return "/tell";
            case ChatType.Echo:
                return "/echo";
            case ChatType.Linkshell:
                return "/linkshell";
            case ChatType.CrossWorldLinkshell:
                return "/cwlinkshell";
            default:
                return "";
        }
    }

    internal static string GetPattern(this ChatType c)
    {
        // If the chat type is none, return nothing.
        if ( c == ChatType.None )
            return "";

        // Create the pattern.
        string pattern = $"^\\s*(?:(?<short>{c.GetShortHeader()})|(?<long>{c.GetLongHeader()}))";

        // Tell pattern also requires user name@world
        if ( c == ChatType.Tell )
            pattern += "\\s+(?<target>[\\w']+ [\\w']+@[a-zA-Z]+)";

        // If the chat type is linkshell, also check for a number.
        if ( c >= ChatType.Linkshell )
            pattern += "(?<channel>\\d)";

        return pattern;
    }
}
