using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Dalamud.Interface.Windowing;

namespace Wordsmith.Gui
{
    public class ScratchPadUI : Window
    {
        protected static int _nextID = 0;
        public static int LastID => _nextID;
        public static int NextID => _nextID++;
        public int ID { get; set; }

        protected string _scratch = "";
        public ScratchPadUI() :base($"{Wordsmith.AppName} - Scratch Pad #{_nextID}")
        {
            ID = NextID;

            IsOpen = true;
            WordsmithUI.WindowSystem.AddWindow(this);
            SizeConstraints = new()
            {
                MinimumSize = new(200, 100),
                MaximumSize = new(float.MaxValue, float.MaxValue)
            };

            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        }

        public override void Draw()
        {
            
            ImGui.InputTextMultiline("Scratch Pad", ref _scratch, 4096, new(-1, (Size?.X ?? 25) - 50));
            if (ImGui.Button($"Delete", new(-1, 20)))
            {
                this.IsOpen = false;
                WordsmithUI.RemoveWindow(this);
            }
        }

        public override void OnClose()
        {
            base.OnClose();

            if (Wordsmith.Configuration.DeleteClosedScratchPads && !IsOpen)
                WordsmithUI.RemoveWindow(this);
                
        }
    }
}
