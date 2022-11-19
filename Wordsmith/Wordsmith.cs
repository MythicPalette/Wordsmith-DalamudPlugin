// Author: Lady Defile
// Description: Wordsmith is a plugin for Dalamud; a plugin API for Final Fantasy XIV Online.
//
// A note to anyone looking to explore this code and/or my future self if I forget how all of
// this code works.
// Wordsmith.cs is the main interface and the entry point for Dalamud.
// WordsmithUI.cs manages the creation, showing/hiding, and disposal of user windows.
// Extensions.cs has several extension methods.
// Global.cs has plugin-wide constants and global usings
//
// Files in the Helpers namespace are processing code removed from the file that uses them.
// This was done to help cut down on file inflation and clearly separate functions from UI
// where possible.
//
// Files in the Data namespace are data container classes/structs. The exception is Lang.cs
//
// Data/Lang.cs is the file that manages the loading, searching, comparing, and unloading of
// Wordsmith's internal dictionary. This is necessary for the spell checking feature to work.
//
// While the code may appear complicated, I've done my best to simply and compartmentalize
// anything that I can to keep things easy to pick up. It should not be too difficult for
// others or my future self to (re)visit the code in these files and quickly rediscover
// the functions of each file as well as the flow of the program.

using Dalamud.Game.Command;
using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.Plugin;

using Lumina.Excel.GeneratedSheets;

namespace Wordsmith;

public sealed class Wordsmith : IDalamudPlugin
{
    #region Constants
    /// <summary>
    /// Plugin name.
    /// </summary>
    internal const string AppName = "Wordsmith";

    /// <summary>
    /// Command to open a new or specific scratch pad.
    /// </summary>
    private const string SCRATCH_CMD_STRING = "/scratchpad";

    /// <summary>
    /// Command to open the settings window.
    /// </summary>
    private const string SETTINGS_CMD_STRING = "/wordsmith";

    /// <summary>
    /// Command to open the thesaurus.
    /// </summary>
    private const string THES_CMD_STRING = "/thesaurus";
    #endregion

    /// <summary>
    /// Plugin name interface property.
    /// </summary>
    public string Name => AppName;

    #region Plugin Services
    [PluginService]
    internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static CommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static DataManager DataManager { get; private set; } = null!;
    #endregion

    /// <summary>
    /// <see cref="Configuration"/> holding all configurable data for the plugin.
    /// </summary>
    internal static Configuration Configuration { get; private set; } = null!;

    #region Constructor and Disposer
    /// <summary>
    /// Default constructor.
    /// </summary>
    public Wordsmith()
    {
        // Get the configuration.
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Add commands
        CommandManager.AddHandler(THES_CMD_STRING, new CommandInfo(this.OnMainCommand) { HelpMessage = "Display the thesaurus window." });
        CommandManager.AddHandler(SETTINGS_CMD_STRING, new CommandInfo(this.OnSettingsCommand) { HelpMessage = "Display the configuration window." });
        CommandManager.AddHandler(SCRATCH_CMD_STRING, new CommandInfo(this.OnScratchCommand) { HelpMessage = "Opens a new scratch pad or a specific pad if number given i.e. /scratchpad 5" });

        // Register handlers for draw and openconfig events.
        PluginInterface.UiBuilder.Draw += WordsmithUI.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += WordsmithUI.ShowSettings;

        // Initialize the dictionary.
        Wordsmith.Helpers.Lang.Init();
    }

    /// <summary>
    /// Disposal method for cleaning the plugin.
    /// </summary>
    public void Dispose()
    {
        // Remove events.
        PluginInterface.UiBuilder.Draw -= WordsmithUI.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= WordsmithUI.ShowSettings;

        // Remove command handlers.
        CommandManager.RemoveHandler(THES_CMD_STRING);
        CommandManager.RemoveHandler(SETTINGS_CMD_STRING);
        CommandManager.RemoveHandler(SCRATCH_CMD_STRING);

        // Dispose of the UI
        WordsmithUI.Dispose();
    }
    #endregion
    #region Event Callbacks
    private void OnMainCommand(string command, string args) => WordsmithUI.ShowThesaurus();
    private void OnSettingsCommand(string command, string args) => WordsmithUI.ShowSettings();
    private void OnScratchCommand(string command, string args)
    {
        int x;
        if (int.TryParse(args.Trim(), out x))
            WordsmithUI.ShowScratchPad(x);
        else
            WordsmithUI.ShowScratchPad();
    }
    #endregion
}