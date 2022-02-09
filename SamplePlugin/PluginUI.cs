using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace Wordsmith
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public class PluginUI : IDisposable
    {
        private Plugin Plugin;
        public Gui.ThesaurusUI Main;
        public static Gui.Alert? Alert;
        public static readonly WindowSystem WindowSystem = new WindowSystem("Wordsmith");

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(Plugin configuration)
        {
            this.Plugin = configuration;
            Main = new Gui.ThesaurusUI(Plugin);
            //Alert = new Gui.Alert(Plugin);
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            Main.Draw();
            Alert?.Draw();
            DrawSettingsWindow();
        }

        public void ShowMain()
        {
            Main.IsOpen = true;
        }
        public void RaiseAlert(string alert) => Alert.AppendMessage(alert);

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                //var configValue = Plugin.Configuration.SomePropertyToBeSavedAndWithADefault;
                //if (ImGui.Checkbox("Random Config Bool", ref configValue))
                //{
                //    Plugin.Configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                //    // can save immediately on change, if you don't want to provide a "Save and Close" button
                //    Plugin.Configuration.Save();
                //}
            }
            ImGui.End();
        }
    }
}
