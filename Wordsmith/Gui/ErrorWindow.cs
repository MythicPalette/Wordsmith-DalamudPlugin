using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;


namespace Wordsmith.Gui
{
    internal class ErrorWindow : Window
    {
        private Dictionary<string, object> _dump = new Dictionary<string, object>();
        public ErrorWindow( Dictionary<string, object> dump ) : base( $"Wordsmith Error" )
        {
            this._dump = dump;
            this.Flags = ImGuiWindowFlags.NoResize;
            this.Flags |= ImGuiWindowFlags.NoScrollbar;
            this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
            this.Size = new( 250 * ImGuiHelpers.GlobalScale, 165 * ImGuiHelpers.GlobalScale );
        }

        public override void Draw()
        {
            ImGui.TextWrapped( "Wordsmith has encountered an error.\nCopy error dump to clipboard and open bug report page?\n\nWARNING: I WILL be able to see anything and everything typed as part of the log." );
            if ( ImGui.Button( $"Yes##CopyAndReportButton", new((ImGui.GetWindowWidth() / 2 ) - ( 10 * ImGuiHelpers.GlobalScale ), 25 * ImGuiHelpers.GlobalScale ) ))
            {
                ImGui.SetClipboardText( System.Text.Json.JsonSerializer.Serialize(this._dump, new System.Text.Json.JsonSerializerOptions() { IncludeFields = true } ) );
                System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo( "https://github.com/LadyDefile/Wordsmith-DalamudPlugin/issues" ) { UseShellExecute = true } );

                WordsmithUI.RemoveWindow( this );
            }
            ImGui.SameLine();
            if ( ImGui.Button( $"No##CopyAndReportButton", new( (ImGui.GetWindowWidth() / 2) - (10 * ImGuiHelpers.GlobalScale), 25 * ImGuiHelpers.GlobalScale ) ) )
                WordsmithUI.RemoveWindow( this );
        }
    }
}
