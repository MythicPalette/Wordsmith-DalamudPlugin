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

#if DEBUG
        PluginLog.LogDebug( $"DebugUI created." );
#endif
    }
    public override void Draw()
    {
        foreach (Window w in WordsmithUI.Windows)
        {
            if (w != null && w is ScratchPadUI pad)
            {
                // Draw the pad info
                ImGui.TextWrapped( $"{pad.GetDebugString()}" );
                ImGui.Separator();
                ImGui.Spacing();
            }
        }
    }
}
