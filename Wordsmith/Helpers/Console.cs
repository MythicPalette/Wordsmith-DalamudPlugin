using Wordsmith.Gui;

namespace Wordsmith.Helpers;

internal sealed partial class Console
{
    internal static int iSpellcheckMode = 0;
    internal static Dictionary<ScratchPadUI, List<string>> Log = [];

    internal static bool ProcessCommand(ScratchPadUI pad, string s)
    {
        Match m = OptionRegex().Match( s );
        if ( !m.Success )
            return false;

        // Enable debug
        switch (m.Groups["option"].Value.ToLower())
        {
            case "config":
                if ( m.Groups["value"].Value.Equals( "reset", StringComparison.CurrentCultureIgnoreCase ) )
                    Wordsmith.ResetConfig();
                break;

            case "dump":
                if ( m.Groups["value"].Value.Equals( "all", StringComparison.CurrentCultureIgnoreCase ) )
                    WordsmithUI.ShowErrorWindow( pad.Dump() );
                else if ( m.Groups["value"].Value.Equals( "config", StringComparison.CurrentCultureIgnoreCase ) )
                    WordsmithUI.ShowErrorWindow(Wordsmith.Configuration.Dump());
                break;

            case "addpads":
                if( int.TryParse( m.Groups["value"].Value, out int count ) )
                {
                    for( int i = 0; i < count; i++ )
                        WordsmithUI.ShowScratchPad();
                }

                break;

            case "guid":
                if ( m.Groups["value"].Value.Equals( "new", StringComparison.CurrentCultureIgnoreCase ) )
                    Dalamud.Bindings.ImGui.ImGui.SetClipboardText(Guid.NewGuid().ToString().ToUpper());
                break;

            case "mark":
                int counter = 1;
                RepeatMode[] aRepeat = [.. Enum.GetValues(typeof(RepeatMode)).Cast<RepeatMode>()];
                MarkerPosition[] aPos = [.. Enum.GetValues(typeof(MarkerPosition)).Cast<MarkerPosition>()];
                DisplayMode[] aDisplay = [..Enum.GetValues(typeof(DisplayMode)).Cast < DisplayMode >()];
                List<DisplayMode> lDisplay = [];
                for ( int x = 0; x < 3; x++ )
                {
                    for ( int y = 3; y < 6; y++ )
                        lDisplay.Add( aDisplay[x] | aDisplay[y] );
                }

                bool all = m.Groups["value"].Value.Equals( "all", StringComparison.CurrentCultureIgnoreCase );
                if ( m.Groups["value"].Value.Equals( "mp", StringComparison.CurrentCultureIgnoreCase ) || all)
                {
                    foreach ( MarkerPosition mp in aPos )
                    {
                        Wordsmith.Configuration.ChunkMarkers.Add( new( $"({counter:X2})", mp, aRepeat[counter% aRepeat.Length], lDisplay[counter% lDisplay.Count], (uint)counter % 3, (uint)counter % 4 ) );
                        counter++;
                    }
                }
                if ( m.Groups["value"].Value.Equals( "rm", StringComparison.CurrentCultureIgnoreCase ) || all )
                {
                    foreach ( RepeatMode rm in aRepeat )
                    {
                        Wordsmith.Configuration.ChunkMarkers.Add( new( $"({counter:X2})", aPos[counter % aPos.Length], rm, lDisplay[counter % lDisplay.Count], (uint)counter % 3, (uint)counter % 4 ) );
                        counter++;
                    }
                }
                if ( m.Groups["value"].Value.Equals( "dm", StringComparison.CurrentCultureIgnoreCase ) || all )
                {
                    foreach ( DisplayMode dm in lDisplay )
                    {
                        Wordsmith.Configuration.ChunkMarkers.Add( new( $"({counter:X2})", aPos[counter % aPos.Length], aRepeat[counter % aRepeat.Length], dm, (uint)counter % 3, (uint)counter % 4 ) );
                        counter++;
                    }
                }
                if ( m.Groups["value"].Value.Equals( "clear", StringComparison.CurrentCultureIgnoreCase ) )
                    Wordsmith.Configuration.ChunkMarkers.Clear();
                break;

            case "spellcheck":
                if ( m.Groups["value"].Value.Equals( "limited", StringComparison.CurrentCultureIgnoreCase ) )
                    iSpellcheckMode = 0;
                else if ( m.Groups["value"].Value.Equals( "unlimited", StringComparison.CurrentCultureIgnoreCase ) )
                    iSpellcheckMode = 2;
                else if ( m.Groups["value"].Value.Equals( "onedit", StringComparison.CurrentCultureIgnoreCase ) )
                    iSpellcheckMode = 1;
                break;

            case "ex":
                if ( m.Groups["value"].Value.Equals( "ffxivify", StringComparison.CurrentCultureIgnoreCase ) )
                    pad.FFXIVify();
                break;

            default:
                return false;
        }

        // Add the the command to the log.
        if ( !Log.TryGetValue( pad, out List<string>? value ) )
            Log[pad] = [$"{m.Groups["option"].Value} = {m.Groups["value"].Value}"];
        else
            value.Add( s );

        while ( Log[pad].Count > 15 )
            Log[pad].RemoveAt( 0 );

        return true;
    }

    [GeneratedRegex( @"(?<=^devx(?:\s*))(?<option>\S+)(?:\s*=\s*)(?<value>\S+)$" )]
    private static partial Regex OptionRegex();
}