using System;
using System.IO;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Logging;

namespace Wordsmith
{
    public sealed class Wordsmith : IDalamudPlugin
    {

        public static readonly string AppName = "Wordsmith";
        public string Name => AppName;
        private const string MAIN_CMD_STRING = "/wordsmith";
        private const string SETTINGS_CMD_STRING = "/wordsmithconfig";

        public DalamudPluginInterface PluginInterface { get; init; }
        public CommandManager CommandManager { get; init; }
        public static Configuration Configuration { get; set; }

        public Wordsmith(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            //var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            //var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            this.CommandManager.AddHandler(MAIN_CMD_STRING, new CommandInfo(OnMainCommand)
            {
                HelpMessage = "Display the main Wordsmith window."
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;

            Data.Lang.Init();
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(MAIN_CMD_STRING);
            this.CommandManager.RemoveHandler(SETTINGS_CMD_STRING);
        }

        private void OnMainCommand(string command, string args) => WordsmithUI.ShowMain();

        private void DrawUI()
        {
            try { WordsmithUI.WindowSystem.Draw(); }
            catch (InvalidOperationException e)
            {
                // If the message isn't about collection being modified, log it. Otherwise
                // Discard the error.
                if(!e.Message.StartsWith("Collection was modified"))
                    PluginLog.LogError($"{e.Message}");
            }
            catch (Exception e) { PluginLog.LogError($"{e} :: {e.Message}"); }
        }
    }
}
