using Wordsmith.Gui;
using Dalamud.Bindings.ImGui;

namespace Wordsmith.Helpers;

internal sealed class Console
{
    internal static int iSpellcheckMode = 0;
    internal static Dictionary<ScratchPadUI, List<string>> Log = new();

    internal static bool ProcessCommand(ScratchPadUI pad, string s)
    {
        Match m = Regex.Match(s, @"(?<=^devx(?:\s*))(?<option>\S+)(?:\s*=\s*)(?<value>\S+)$");
        if ( !m.Success )
            return false;

        // Enable debug
        switch (m.Groups["option"].Value.ToLower())
        {
            case "config":
                if ( m.Groups["value"].Value.ToLower() == "reset" )
                    Wordsmith.ResetConfig();
                break;

            case "dump":
                if ( m.Groups["value"].Value.ToLower() == "all" )
                    WordsmithUI.ShowErrorWindow( pad.Dump() );
                else if ( m.Groups["value"].Value.ToLower() == "config" )
                    WordsmithUI.ShowErrorWindow(Wordsmith.Configuration.Dump());
                break;

            case "addpads":
                int count;
                if ( int.TryParse( m.Groups["value"].Value, out count ) )
                    for ( int i = 0; i < count; i++ )
                        WordsmithUI.ShowScratchPad();
                break;

            case "guid":
                if ( m.Groups["value"].Value.ToLower() == "new" )
                    ImGui.SetClipboardText(Guid.NewGuid().ToString().ToUpper());
                break;

            case "mark":
                int counter = 1;
                RepeatMode[] aRepeat = Enum.GetValues(typeof(RepeatMode)).Cast<RepeatMode>().ToArray();
                MarkerPosition[] aPos = Enum.GetValues(typeof(MarkerPosition)).Cast<MarkerPosition>().ToArray();
                DisplayMode[] aDisplay = Enum.GetValues(typeof(DisplayMode)).Cast<DisplayMode>().ToArray();
                List<DisplayMode> lDisplay = new();
                for ( int x = 0; x < 3; x++ )
                    for ( int y = 3; y < 6; y++ )
                        lDisplay.Add( aDisplay[x] | aDisplay[y] );

                bool all = m.Groups["value"].Value.ToLower() == "all";
                if ( m.Groups["value"].Value.ToLower() == "mp" || all)
                {
                    foreach ( MarkerPosition mp in aPos )
                    {
                        Wordsmith.Configuration.ChunkMarkers.Add( new( $"({counter:X2})", mp, aRepeat[counter% aRepeat.Length], lDisplay[counter% lDisplay.Count], (uint)counter % 3, (uint)counter % 4 ) );
                        counter++;
                    }
                }
                if ( m.Groups["value"].Value.ToLower() == "rm" || all )
                {
                    foreach ( RepeatMode rm in aRepeat )
                    {
                        Wordsmith.Configuration.ChunkMarkers.Add( new( $"({counter:X2})", aPos[counter % aPos.Length], rm, lDisplay[counter % lDisplay.Count], (uint)counter % 3, (uint)counter % 4 ) );
                        counter++;
                    }
                }
                if ( m.Groups["value"].Value.ToLower() == "dm" || all )
                {
                    foreach ( DisplayMode dm in lDisplay )
                    {
                        Wordsmith.Configuration.ChunkMarkers.Add( new( $"({counter:X2})", aPos[counter % aPos.Length], aRepeat[counter % aRepeat.Length], dm, (uint)counter % 3, (uint)counter % 4 ) );
                        counter++;
                    }
                }
                if ( m.Groups["value"].Value.ToLower() == "clear" )
                    Wordsmith.Configuration.ChunkMarkers.Clear();
                break;

            case "spellcheck":
                if ( m.Groups["value"].Value.ToLower() == "limited" )
                    iSpellcheckMode = 0;
                else if ( m.Groups["value"].Value.ToLower() == "unlimited" )
                    iSpellcheckMode = 2;
                else if ( m.Groups["value"].Value.ToLower() == "onedit" )
                    iSpellcheckMode = 1;
                break;

            case "ex":
                if ( m.Groups["value"].Value.ToLower() == "ffxivify" )
                    pad.FFXIVify();
                break;

            default:
                return false;
        }

        // Add the the command to the log.
        if ( !Log.ContainsKey( pad ) )
            Log[pad] = new() { $"{m.Groups["option"].Value} = {m.Groups["value"].Value}" };
        else
            Log[pad].Add( s );

        while ( Log[pad].Count > 15 )
            Log[pad].RemoveAt( 0 );

        return true;
    }
}