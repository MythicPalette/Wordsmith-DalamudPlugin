using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Reflection;

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

            this.CommandManager.AddHandler(SETTINGS_CMD_STRING, new CommandInfo(OnSettingsCommand)
            {
                HelpMessage = "Display the Wordsmith settings window."
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(MAIN_CMD_STRING);
        }

        private void OnMainCommand(string command, string args) => WordsmithUI.ShowMain();
        private void OnSettingsCommand(string command, string args) => WordsmithUI.ShowSettings();

        private void DrawUI()
        {
            try
            {
                WordsmithUI.WindowSystem.Draw();
            }
            catch (Exception e)
            {
                Dalamud.Logging.PluginLog.LogError($"{e} :: {e.Message}");
            }
        }

        private void DrawConfigUI()
        {
        }
    }
}
