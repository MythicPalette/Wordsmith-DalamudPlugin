using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using ImGuiNET;

namespace Wordsmith.Gui;

public class ResetDictionaryUI : Window
{
    public ResetDictionaryUI() : base($"{Wordsmith.AppName} - Reset Dictionary")
    {
        Size = ImGuiHelpers.ScaledVector2(300, 160);
        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        Flags |= ImGuiWindowFlags.NoResize;
    }
    public override void Draw()
    {
        ImGui.TextColored(new(255, 0, 0, 255), "WARNING");
        ImGui.TextWrapped("This will delete all entries that you added to the dictionary. This cannot be undone.");
        ImGui.Text("Proceed?");

        if (ImGui.Button("Yes##RestoreDefaultSettingsConfirmedButton", ImGuiHelpers.ScaledVector2(120, 20)))
        {
            // Spell Check settings.
            Wordsmith.Configuration.CustomDictionaryEntries = new();
            Wordsmith.Configuration.Save();
            IsOpen = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel##RestoreDefaultSettingsAbortedButton", ImGuiHelpers.ScaledVector2(120, 20)))
            IsOpen = false;
    }
}
