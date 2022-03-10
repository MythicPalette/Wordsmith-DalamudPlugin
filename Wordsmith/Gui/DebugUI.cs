using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui;

internal sealed class DebugUI : Window
{
    internal static bool ShowWordIndex = false;

    public DebugUI() : base( $"{Wordsmith.AppName} - Debug" )
    {
        this.SizeConstraints = new()
        {
            MinimumSize = ImGuiHelpers.ScaledVector2(200, 200),
            MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
        };

        this.Flags |= ImGuiWindowFlags.HorizontalScrollbar;

#if DEBUG
        PluginLog.LogDebug( $"DebugUI created." );
#endif
    }
    public override void Draw()
    {
        ImGui.Checkbox( "Show Word Index", ref ShowWordIndex );
        foreach (Window w in WordsmithUI.Windows)
        {
            if (w != null && w is ScratchPadUI pad)
            {
                // Draw the pad info
                ImGui.Text( $"{pad.GetDebugString()}" );
                ImGui.Separator();
                ImGui.Spacing();
            }
        }
    }
}
