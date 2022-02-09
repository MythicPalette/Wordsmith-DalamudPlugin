using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class Alert : Window
    {
        public void AppendMessage(string message) => NewMessage($"{_message}\n{message}");

        public void NewMessage(string message)
        {
            _message = message;
            this.Visible = Plugin.Debug;
        }

        public void AlertUser(string message)
        {
            _message = message;
            this.Visible = true;
        }

        protected static string _message = "";
        public string Message { get => _message; set => _message = value; }
        public Alert(Plugin plugin) : base(plugin) { }

        protected override void DrawUI()
        {
            ImGui.SetNextWindowSize(new Vector2(375, 200), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin($"{Plugin.Name} - Alert!", ref this._visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.TextWrapped(_message);
            }
            ImGui.End();
        }
    }
}
