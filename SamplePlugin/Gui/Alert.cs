using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using Dalamud.Interface.Windowing;

namespace Wordsmith.Gui
{
    public class Alert : Window, IDisposable
    {
        public string Name => "";

        public Alert() : base($"{Plugin.AppName}") { }

        public void AppendMessage(string message) => NewMessage($"{_message}\n{message}");

        public void NewMessage(string message)
        {
            _message = message;
            //this.Visible = Plugin.Debug;
        }

        public void AlertUser(string message)
        {
            _message = message;
            //this.Visible = true;
        }

        protected static string _message = "";
        public string Message { get => _message; set => _message = value; }

        public override void Draw()
        {
            //ImGui.SetNextWindowSize(new Vector2(375, 200), ImGuiCond.Appearing);
            //ImGui.SetNextWindowSizeConstraints(new Vector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
            //if (ImGui.Begin($"{Plugin.Name} - Alert!", ref this._visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            //{
            //    ImGui.TextWrapped(_message);
            //}
            //ImGui.End();
        }

        public void Dispose()
        {
            
        }
    }
}
