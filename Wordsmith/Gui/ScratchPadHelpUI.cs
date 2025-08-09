using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Wordsmith.Gui;

internal sealed class ScratchPadHelpUI : Window
{
    //internal static IDalamudTextureWrap? MerriamWebsterLogo = null;

    internal ScratchPadHelpUI() : base($"{Wordsmith.APPNAME} - Help")
    {
        this.SizeConstraints = new()
        {
            MinimumSize = new( 300, 450 ),
            MaximumSize = new( 9999, 9999 )
        };
    }
    public override void Draw()
    {
        if (ImGui.BeginTabBar($"{Wordsmith.APPNAME}HelpTabBar"))
        {
            // General tab.
            if (ImGui.BeginTabItem($"General##HelpTabBarItem"))
            {
                //ImGui.Image( MerriamWebsterLogo.ImGuiHandle, ImGuiHelpers.ScaledVector2( 64, 64 ) );
                // TODO: Fix image display with new texture API
                // var texture = Wordsmith.TextureProvider.GetFromFile(Path.Combine(Wordsmith.PluginInterface.AssemblyLocation.Directory!.FullName, "mwlogo.png")).GetWrapOrEmpty();
                ImGui.SameLine();
                ImGui.TextWrapped( "Thesaurus functionality provided through Merriam-Webster's API. Thank you to Merriam-Webster for providing a free API-Key to Wordsmith to allow for integrated thesaurus functionality.\n\nNote: This support is experimental and Merriam-Webster only provides 1,000 free queries a day. If 1,000 queries a day is not enough I will look into more options." );
                ImGui.Separator();
                // Create multiple subsections of text for the user to read over.
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
                ImGui.TextWrapped("/Tell is a little different than other headers because it requires a target. Your target can be User Name@World or any usual placehold.\n\nMore information on Placeholders in the Placeholders tab.");
                ImGui.EndTabItem();
            }

            // Placeholders tab.
            if (ImGui.BeginTabItem($"Placeholders##HelpTabBarItem"))
            {
                // Create a table.
                if (ImGui.BeginTable("ScratchPadTellHelpPlaceholderTable", 2, ImGuiTableFlags.Borders))
                {
                    // The table will have two headers, one for placeholder text and one for placeholder description.
                    ImGui.TableSetupColumn("PlaceholderValueColumn", ImGuiTableColumnFlags.WidthFixed, 125 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("PlaceholderValueColumn", ImGuiTableColumnFlags.WidthStretch, 2);

                    // Setup the column headers.
                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Placeholder##ColumnHeader");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Description##ColumnHeader");

                    // Create a string array with all of the placeholders.
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

                    // Create an array with all of the placeholder definitions at the same index
                    // as the placeholder in the previous array.
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

                    // Iterate through both arrays simultaneously and put the data in the
                    // table left then right.
                    for (int i = 0; i < placeholders.Length; ++i)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(placeholders[i]);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(descriptions[i]);
                    }

                    ImGui.EndTable();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"Roleplaying##HelpTabBarItem"))
            {
                ImGui.TextWrapped($"Some useful terms to know about roleplaying.");
                if (ImGui.BeginTable("##RoleplayTermsHelpTable", 2))
                {
                    ImGui.TableSetupColumn("RoleplayHelpTermColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("God Modding").X);
                    ImGui.TableSetupColumn("RoleplayHelpDescriptionColumn", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Term");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Description");

                    string[] terms =
                    {
                        "IC",
                        "OOC",
                        "RP",
                        "MRP",
                        "ERP",
                        "WU",
                        "God Modding",
                        "Metagaming",
                    };

                    string[] desc =
                    {
                        "Stands for \"In Character\" which means any text you enter is as your character, not as yourself.",
                        "Stands for \"Out of Character\" which means you are speaking as yourself.",
                        "Stands for \"Roleplay\" or \"Roleplaying\".",
                        "Stands for \"Mature Roleplay\". MRP is generally roleplay with adult themes but not necessarily sexual (think drugs or gangs).",
                        "Stands for \"Erotic Roleplay\". This kind of roleplay is sexual in nature.",
                        "Stands for \"Walk Up\" meaning you don't mind if a stranger walks up and joins your RP or starts a new one with you.",
                        "This is when your roleplay tries to control the other player's actions and dictates what they do.",
                        "This is when you use information your character doesn't know in character. (i.e. someone tells you \"My character hates cheese\" OOC and then your character mentions knowing that the other character hates cheese even though it was impossible for them to know that."
                    };

                    for (int i = 0; i < terms.Length && i < desc.Length; ++i)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(terms[i]);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(desc[i]);
                    }
                    ImGui.EndTable();
                }
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}
