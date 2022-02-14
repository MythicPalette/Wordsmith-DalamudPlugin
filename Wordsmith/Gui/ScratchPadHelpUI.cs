using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    public class ScratchPadHelpUI : Window
    {
        public ScratchPadHelpUI() : base($"{Wordsmith.AppName} - Scratch Pad Help")
        {
            IsOpen = true;
            WordsmithUI.WindowSystem.AddWindow(this);
            SizeConstraints = new()
            {
                MinimumSize = new(300, 450),
                MaximumSize = new(float.MaxValue, float.MaxValue)
            };

            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        }
        public override void Draw()
        {
            ImGui.TextWrapped("Scratch Pads are fairly easy to use. First, on the top left, there is a drop down menu where you get to select what chat channel prefix you want to use. If you use choose /tell, a texbox will appear where you can type the user name and world!");
            ImGui.Separator();
            ImGui.TextWrapped("Up next is the OOC checkbox. If you enable it, it will wrap any text blocks in ooc tags (( like this )).");
            ImGui.Separator();
            ImGui.TextWrapped("The rest is easy. Type in the bar at the bottom and you can view the output above. It shows all of your text there so you can proofread.");
            ImGui.Separator();
            ImGui.TextWrapped("Once you're done typing, try hitting the Copy button to copy the text then paste it in the chat. Remember, if there is more than one block, you'll have to copy/paste and send each block one at a time. Just hit the copy button again to copy the next block!");
            ImGui.Separator();
            ImGui.TextWrapped("Not sure if you spelt everything correctly? Try using the Spell Check button. It will find any words not in the internal dictionary and let you know then offer you the chance to replace the word, add it to the dictionary, or if you just hit enter in the blank text box it will ignore the spelling.");
            ImGui.Separator();
            ImGui.TextWrapped("Of course, the Clear button will clear any text you've written and Delete Pad will close the pad and clear all the data.");
        }
    }
}
