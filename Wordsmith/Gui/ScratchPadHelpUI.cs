using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface;
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
                MinimumSize = ImGuiHelpers.ScaledVector2(300, 450),
                MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
            };
        }
        public override void Draw()
        {
            ImGui.TextWrapped("Scratch Pads are fairly easy to use. First, on the top left, there is a drop down menu where you get to select what chat channel prefix you want to use. If you use choose /tell, a texbox will appear where you can type the user name and world or placeholders.");
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextWrapped("Once you're done typing, try hitting the Copy button to copy the text then paste it in the chat. Remember, if there is more than one block, you'll have to copy/paste and send each block one at a time. Just hit the copy button again to copy the next block!");
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextWrapped("Not sure if you spelt everything correctly? Try using the Spell Check button. It will find any words not in the internal dictionary and let you know then offer you the chance to replace the word, add it to the dictionary, or if you just hit enter in the blank text box it will ignore the spelling.");
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextWrapped("Of course, the Clear button will clear any text you've written and Delete Pad will close the pad and clear all the data.");
            ImGui.Spacing();
            ImGui.TextWrapped("To use OOC or \"Out Of Character\" markers, all you have to do is check the box at the top right of the scratch pad. It will automatically wrap all text you type in double parenthesis (( like this )). It's a simple toggle that you can easily turn on and off at any time.\n\n Note: It even works with text already entered.");
            ImGui.Separator();

            ImGui.Spacing();
            ImGui.TextWrapped("/Tell is a little different than other headers because it requires a target. Your target can be User Name@World or any usual placehold.\n\nSome placeholders you can use:");

            if(ImGui.BeginTable("ScratchPadTellHelpPlaceholderTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("PlaceholderValueColumn", ImGuiTableColumnFlags.WidthFixed, 125 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("PlaceholderValueColumn", ImGuiTableColumnFlags.WidthStretch, 2);

                ImGui.TableNextColumn();
                ImGui.TableHeader("Placeholder##ColumnHeader");

                ImGui.TableNextColumn();
                ImGui.TableHeader("Description##ColumnHeader");

                string[] placeholders = new string[]
                {
                    "<t>, <target>",
                    "<tt>, <t2t>",
                    "<me>, <0>",
                    "<r>, <reply>",
                    "<1> - <8>",
                    "<f>, <focus>",
                    "<lt>, <lasttarget>",
                    "<attack1> - <attack5>",
                    "<bind1> - <bind3>",
                    "<square>",
                    "<circle>",
                    "<cross>",
                    "<triangle>",
                    "<mo>, <mouse>"
                };

                string[] descriptions = new string[]
                {
                    "Your current target.",
                    "The target of your current target.",
                    "Yourself!",
                    "The last person to /tell you.",
                    "Party member by number.",
                    "Your focus target.",
                    "Your last target.",
                    "Person with the Attack 1-5 marker over their head.",
                    "Person with the Bind 1-3 marker over their head.",
                    "Person with the Square marker over their head.",
                    "Person with the Circle marker over their head.",
                    "Person with the Cross marker over their head.",
                    "Person with the Triangle marker over their head.",
                    "The person your mouse is currently over."
                };

                for(int i=0; i<placeholders.Length; ++i)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(placeholders[i]);

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(descriptions[i]);
                }

                ImGui.EndTable();
            }
        }
    }
}
