using Dalamud.Bindings.ImGui;
using System.Text.Json; // Add this for JsonSerializerOptions


namespace Wordsmith.Gui;

internal sealed class ErrorWindow( Dictionary<string, object> dump ) : MessageBox( $"Wordsmith Error", _MESSAGE, ButtonStyle.YesNo, Callback)
{
    private const string _MESSAGE = "Wordsmith has encountered an error.\nCopy error dump to clipboard and open bug report page?\n\nWARNING: I WILL be able to see anything and everything\ntyped as part of the log.";
    private static readonly JsonSerializerOptions _jsonOptions = new() { IncludeFields = true }; // Cache the options
    internal Dictionary<string, object> ErrorDump = dump;

    public static void Callback(MessageBox mb)
    {
        if ( mb is ErrorWindow ew )
        {
            try
            {
                if ( ew.Result == DialogResult.Yes )
                {
                    foreach ( string key in ew.ErrorDump.Keys )
                    {
                        if ( ew.ErrorDump[key] is IntPtr )
                            _ = ew.ErrorDump.Remove( key );
                    }
                    ImGui.SetClipboardText( JsonSerializer.Serialize( ew.ErrorDump, _jsonOptions ) );
                    _ = System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo( "https://github.com/MythicPalette/Wordsmith-DalamudPlugin/issues" ) { UseShellExecute = true } );
                }
            }
            catch ( Exception e )
            {
                Wordsmith.PluginLog.Error( e.ToString() );
            }
        }
        WordsmithUI.RemoveWindow( mb );
    }
}
