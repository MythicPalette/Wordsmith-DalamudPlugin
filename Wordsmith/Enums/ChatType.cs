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
        return c switch
        {
            ChatType.Emote => "/em",
            ChatType.Reply => "/r",
            ChatType.Say => "/s",
            ChatType.Party => "/p",
            ChatType.FC => "/fc",
            ChatType.Shout => "/sh",
            ChatType.Yell => "/y",
            ChatType.Tell => "/t",
            ChatType.Echo => "/e",
            ChatType.Linkshell => "/linkshell",
            ChatType.CrossWorldLinkshell => "/cwlinkshell",
            _ => "",
        };
    }

    internal static string GetLongHeader(this ChatType c)
    {
        return c switch
        {
            ChatType.Emote => "/emote",
            ChatType.Reply => "/reply",
            ChatType.Say => "/say",
            ChatType.Party => "/party",
            ChatType.FC => "/freecompany",
            ChatType.Shout => "/shout",
            ChatType.Yell => "/yell",
            ChatType.Tell => "/tell",
            ChatType.Echo => "/echo",
            ChatType.Linkshell => "/linkshell",
            ChatType.CrossWorldLinkshell => "/cwlinkshell",
            _ => "",
        };
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
