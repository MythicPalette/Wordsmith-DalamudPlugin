
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
namespace Wordsmith.Gui;
internal sealed class DebugUI : Window
{
    public DebugUI() : base( $"{Wordsmith.AppName} - Debug" )
    {
        this.SizeConstraints = new()
        {
            MinimumSize = ImGuiHelpers.ScaledVector2(200, 200),
            MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
        };

        this.Flags |= ImGuiWindowFlags.HorizontalScrollbar;

        PluginLog.LogDebug( $"DebugUI created." );
    }

    public override void Draw()
    {
        if ( ImGui.CollapsingHeader( $"Wordsmith Configuration" ) )
            DrawClassData( Wordsmith.Configuration, $"Configuration" );

        // If there is a setting, draw the settings section.
        if ( ImGui.CollapsingHeader( "Settings Data##DebugUIHeDebugUICollapsingHeaderader" ) )
            DrawClassData( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( SettingsUI ) ), SettingsUI.GetWindowName() );        

        // If there is a thesaurus, draw the thesaurus section.
        if ( ImGui.CollapsingHeader( "Thesaurus Data##DebugUICollapsingHeader" ) )
            DrawClassData( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( ThesaurusUI ) ), ThesaurusUI.GetWindowName() );

        // If there is a MessageBox, draw the MessageBox section.
        if ( ImGui.CollapsingHeader( $"Message Boxes##DebugUICollapsingHeader" ) )
        {
            ImGui.Indent();
            foreach ( MessageBox mb in WordsmithUI.Windows.Where(w => w.GetType() == typeof(MessageBox)) )
                if ( ImGui.CollapsingHeader($"{mb.WindowName}") )
                    DrawClassData( mb, $"MessageBoxUI" );

            foreach ( ErrorWindow ew in WordsmithUI.Windows.Where( w => w.GetType() == typeof( ErrorWindow ) ) )
                if ( ImGui.CollapsingHeader( $"{ew.WindowName}" ) )
                    DrawClassData( ew, $"ErrorWindowUI" );
            ImGui.Unindent();
        }

        // If there is a scratch pad, draw the scratch pad section.
        if ( ImGui.CollapsingHeader( "Scratch Pad Data##DebugUICollapsingHeader" ) )
        {
            ImGui.Indent();
            foreach ( ScratchPadUI pad in WordsmithUI.Windows.Where( w => w.GetType() == typeof( ScratchPadUI ) ) )
                if ( ImGui.CollapsingHeader($"Scratch Pad {pad.ID}##DebugUICollapsingHeader") )
                    DrawClassData( pad, $"ScratchPad{pad.ID}", "NextID" );
            ImGui.Unindent();
        }
        
    }
    private void DrawClassData( object? obj, object id, params string[]? excludes )
    {
        if ( obj == null )
            return;

        ImGui.Indent();
        // Get the list of results
        IReadOnlyList<(int Type, string Name, string Value) > data = obj.GetProperties(excludes);

        // Draw Properties
        if ( ImGui.CollapsingHeader( $"Properties##{id}DebugUIHeader" ) )
        {
            ImGui.Indent();
            foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 0 ) )
                ImGui.TextWrapped( $"{Name}\t: {Value.Replace("\r", "\\r").Replace("\n", "\\n")}" );
            ImGui.Unindent();
        }

        // Draw Fields
        if ( ImGui.CollapsingHeader( $"Fields##{id}DebugUIHeader" ) )
        {
            ImGui.Indent();
            foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 1 ) )
                ImGui.TextWrapped( $"{Name}\t: {Value.Replace( "\r", "\\r" ).Replace( "\n", "\\n" )}" );
            ImGui.Unindent();
        }
        ImGui.Unindent();
    }
}
