#if DEBUG

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
        {
            ImGui.Indent();
            DrawClassData( Wordsmith.Configuration, $"Configuration" );
            ImGui.Unindent();
        }
        // If there is a scratch pad, draw the scratch pad section.
        if ( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( ScratchPadUI ) ) is not null )
        {
            if ( ImGui.CollapsingHeader( "Scratch Pad Data##DebugUIHeader" ) )
            {
                ImGui.Indent();
                foreach ( IReflected r in WordsmithUI.Windows.Where(x => x is ScratchPadUI) )
                    DrawClassData( r, $"ScratchPad{((ScratchPadUI)r).ID}", "NextID" );

                ImGui.Unindent();
            }
        }

        // If there is a setting, draw the settings section.
        if ( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( SettingsUI ) ) is not null )
        {
            if ( ImGui.CollapsingHeader( "Settings UI Data##DebugUIHeader" ) )
            {
                ImGui.Indent();

                foreach ( IReflected r in WordsmithUI.Windows.Where(x => x is SettingsUI) )
                    DrawClassData( r, $"SettingsUI" );

                ImGui.Unindent();
            }
        }

        // If there is a thesaurus, draw the thesaurus section.
        if ( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( ThesaurusUI )) is not null )
        {
            if ( ImGui.CollapsingHeader( $"Thesuaurs UI Data##DebugUIHeader" ) )
            {
                ImGui.Indent();
                foreach ( IReflected r in WordsmithUI.Windows.Where( x => x is ThesaurusUI ) )
                    DrawClassData( r, $"ThesaurusUI" );
                ImGui.Unindent();
            }
        }
    }
    private void DrawClassData( IReflected? reflected, object id, params string[]? excludes )
    {
        if ( reflected == null )
            return;

        // Get the list of results
        IReadOnlyList<(int Type, string Name, string Value) > data = reflected.GetProperties(excludes);

        // Draw Properties
        if ( ImGui.CollapsingHeader( $"Properties##{id}" ) )
        {
            ImGui.Indent();
            foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 0 ) )
                ImGui.Text( $"{Name}\t: {Value}" );
            ImGui.Unindent();
        }

        // Draw Fields
        if ( ImGui.CollapsingHeader( $"Fields##{id}" ) )
        {
            ImGui.Indent();
            foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 1 ) )
                ImGui.Text( $"{Name}\t: {Value}" );
            ImGui.Unindent();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
}
#endif
