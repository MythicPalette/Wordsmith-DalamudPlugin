global using System;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Numerics;
global using System.Collections.Generic;
global using Dalamud.Logging;
global using Wordsmith.Extensions;

using Dalamud.Game.Command;
using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.Plugin;

using XivCommon;
using XivCommon.Functions.ContextMenu;
using Lumina.Excel.GeneratedSheets;

namespace Wordsmith
{
    public sealed class Wordsmith : IDalamudPlugin
    {
        public static readonly string AppName = "Wordsmith";
        public string Name => AppName;
        private const string THES_CMD_STRING = "/thesaurus";
        private const string SETTINGS_CMD_STRING = "/wordsmith";
        private const string SCRATCH_CMD_STRING = "/scratchpad";

        internal static DalamudPluginInterface PluginInterface { get; set; } = null!;
        internal CommandManager CommandManager { get; init; }
        internal static Configuration Configuration { get; private set; } = null!;

        internal static XivCommonBase XivCommon { get; private set; } = null!;

        [PluginService]
        internal static DataManager DataManager { get; private set; } = null!;

        public Wordsmith(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            // you might normally want to embed resources and load them from the manifest stream
            //var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            //var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            this.CommandManager.AddHandler(THES_CMD_STRING, new CommandInfo(OnMainCommand)
            {
                HelpMessage = "Display the thesaurus window."
            });

            this.CommandManager.AddHandler(SETTINGS_CMD_STRING, new CommandInfo(OnSettingsCommand)
            {
                HelpMessage = "Display the configuration window."
            });

            this.CommandManager.AddHandler(SCRATCH_CMD_STRING, new CommandInfo(OnScratchCommand)
            {
                HelpMessage = "Opens a new scratch pad or a specific pad if number given i.e. /scratchpad 5"
            });

            PluginInterface.UiBuilder.Draw += WordsmithUI.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += WordsmithUI.ShowSettings;

            // Setup the context menu.
            XivCommon = new XivCommonBase(Hooks.ContextMenu);

            XivCommon.Functions.ContextMenu.OpenContextMenu += OnContextMenu;
            WordsmithUI.Init();
            Data.Lang.Init();
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(THES_CMD_STRING);
            this.CommandManager.RemoveHandler(SETTINGS_CMD_STRING);
            this.CommandManager.RemoveHandler(SCRATCH_CMD_STRING);

            XivCommon.Functions.ContextMenu.OpenContextMenu -= this.OnContextMenu;
        }

        private void OnMainCommand(string command, string args) => WordsmithUI.ShowThesaurus();
        private void OnSettingsCommand(string command, string args) => WordsmithUI.ShowSettings();
        private void OnScratchCommand(string command, string args)
        {
            int x;
            if (int.TryParse(args.Trim(), out x))
                WordsmithUI.ShowScratchPad(x);
            else
                WordsmithUI.ShowScratchPad(-1);
        }

        private void OnContextMenu(ContextMenuOpenArgs args)
        {
            // If the user doesn't want the context menu option return from the function.
            if (!Configuration.AddContextMenuOption)
                return;

            // Get the "Send Tell" option.
            int index = args.Items.FindIndex(a => a is NativeContextMenuItem n && n.Name.TextValue == "Send Tell");

            // If there is no "Send Tell" option exit the function.
            if (index < 0)
                return;

            // Get the world name.
            World? w = DataManager.Excel.GetSheet<World>()?.GetRow(args.ObjectWorld);

            // If the world name was found, create a scratch pad targetting the player@world.
            if (w != null)
                args.Items.Insert(index + 1, new NormalContextMenuItem("Tell in Scratch Pad", selectedArgs => WordsmithUI.ShowScratchPad($"{args.Text}@{w?.Name}")));
        }
    }
}
