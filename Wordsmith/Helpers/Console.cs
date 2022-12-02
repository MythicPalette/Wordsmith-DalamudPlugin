using System.Text.RegularExpressions;
using Wordsmith.Gui;

namespace Wordsmith.Helpers;

internal sealed class Console
{
    internal static bool ProcessCommand(ScratchPadUI pad, string s)
    {
        Match m = Regex.Match(s, @"(?<=^devx(?:\s*))(?<option>\S+)(?:=)(?<value>\S+)$");
        if ( !m.Success )
            return false;

        // Enable debug
        switch (m.Groups["option"].Value.ToLower())
        {
            case "dbg":
                Wordsmith.Configuration.EnableDebug = m.Groups["value"].Value == "on";
                Wordsmith.Configuration.Save();
                break;

            case "nquery":
                Wordsmith.Configuration.NumericQuery = m.Groups["value"].Value;
                Wordsmith.Configuration.Save();
                break;

            case "dquery":
                Wordsmith.Configuration.DateQuery = m.Groups["value"].Value;
                Wordsmith.Configuration.Save();
                break;

            case "wquery":
                Wordsmith.Configuration.WordQuery = m.Groups["value"].Value;
                Wordsmith.Configuration.Save();
                break;

            case "tquery":
                Wordsmith.Configuration.TimeQuery = m.Groups["value"].Value;
                Wordsmith.Configuration.Save();
                break;

            case "config":
                if ( m.Groups["value"].Value.ToLower() == "reset" )
                    Wordsmith.Configuration.ResetToDefault();
                break;

            case "dump":
                if ( m.Groups["value"].Value.ToLower() == "all" )
                    WordsmithUI.ShowErrorWindow( pad.Dump(), $"Scratch Pad #{pad.ID} Dump" );
                break;

            case "addpads":
                int count;
                if ( int.TryParse( m.Groups["value"].Value, out count ) )
                    for ( int i = 0; i < count; i++ )
                        WordsmithUI.ShowScratchPad();
                break;

            default:
                return false;
        }
        return true;
    }
}
