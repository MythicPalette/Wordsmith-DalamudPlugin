// Author: Lady Defile
// Description: Wordsmith is a plugin for Dalamud; a plugin API for Final Fantasy XIV Online.
// The purpose of Wordsmith is to make roleplay easier. Roleplay in FFXIV requires typing
// into a small, single horizontal line with very little width. This makes it impossible to
// proofread your text for spelling and grammatical errors. The other option is to type in
// external programs but then copy/paste is a bit of guess work. Wordsmith aims to solve this
// by being the perfect-ish roleplay text editor.
//
// A few notes to anyone looking to explore this code and/or my future self if I forget how all of
// this code works:
// *    Wordsmith.cs is the main plugin interface and the entry point for Dalamud. It handles the
//          initialization of the plugin and text commands.
// *    WordsmithUI.cs manages the creation, showing/hiding, and disposal of GUI windows.
// *    Extensions.cs has several extension methods.
// *    Global.cs has plugin-wide constants and global usings
//
// Files in the Helpers namespace are processing code removed from the file that uses them.
// This was done to help cut down on file bloat and separate functions from UI where possible.
//
// Files in the Gui namespace are GUI windows that are displayed to the user at one point or
// another for a variety of reasons.
//
// While the code may appear complicated, I've done my best to simplify and compartmentalize
// anything that I can to keep things easy to pick up. It should not be too difficult for
// others or my future self to (re)visit the code in these files and quickly rediscover
// the functions of each file as well as the flow of the program.
#region GLOBAL USING
global using System;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Numerics;
global using System.Collections.Generic;
global using Dalamud.Logging;
global using System.Text.RegularExpressions;
#endregion

using Dalamud.Game.Command;
using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.Plugin;
using Wordsmith.Gui;
using Wordsmith.Helpers;
using Dalamud.Plugin.Services;
namespace Wordsmith;

public sealed class Wordsmith : IDalamudPlugin
{
    #region Constants
    /// <summary>
    /// Plugin name.
    /// </summary>
    internal const string AppName = "Wordsmith";

    /// <summary>
    /// The default height of a button.
    /// </summary>
    internal const int BUTTON_Y = 25;

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

    /// <summary>
    /// The maximum length of scratch text.
    /// </summary>
    internal const int MAX_SCRATCH_LENGTH = 32768 ;
    #endregion

    /// <summary>
    /// Plugin name interface property.
    /// </summary>
    public string Name => AppName;

    #region Plugin Services
    // All plugin services are automatically populated by Dalamud. These are important
    // classes that are used to directly interface with the plugin API.
    [PluginService]
    internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;
    #endregion

    /// <summary>
    /// <see cref="Configuration"/> holding all configurable data for the plugin.
    /// </summary>
    internal static Configuration Configuration { get; private set; } = null!;

    /// <summary>
    /// <see cref="WebManifest"/> containing notice and language information.
    /// </summary>
    internal static WebManifest WebManifest { get; private set; } = null!;

    #region Constructor and Disposer
    /// <summary>
    /// Default constructor and initializer for the Wordsmith plugin.
    /// </summary>
    public Wordsmith()
    {
        // Get the configuration.
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Try to load the Merriam-Webster logo if the file exists. It should be in the correct location
        // but it should always be verified beforehand.
        if ( File.Exists( Path.Combine( PluginInterface.AssemblyLocation.Directory!.FullName, "mwlogo.png" ) ) )
            ScratchPadHelpUI.MerriamWebsterLogo = PluginInterface.UiBuilder.LoadImage( Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "mwlogo.png" ));

        // Add commands for the different windows
        // Note that the first two use inline functions but scratch pads require a little text parsing.
        CommandManager.AddHandler(THES_CMD_STRING, new CommandInfo( ( c, a ) => { WordsmithUI.ShowThesaurus(); } ) { HelpMessage = "Display the thesaurus window." });
        CommandManager.AddHandler(SETTINGS_CMD_STRING, new CommandInfo( ( c, a ) => { WordsmithUI.ShowSettings(); }) { HelpMessage = "Display the configuration window." });
        CommandManager.AddHandler(SCRATCH_CMD_STRING, new CommandInfo( this.OnScratchCommand )
        {
            HelpMessage = "Opens or creates a scratch pad. Follow with a number or custom name if you like. (i.e. \"/scratchpad 5\" or \"/scratchpad Juliet\")"
        });

        WebManifest = Git.GetManifest();

        // Register handlers for draw and openconfig events.
        PluginInterface.UiBuilder.Draw += WordsmithUI.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += WordsmithUI.ShowSettings;

        // Initialize the dictionary.
        Lang.Init();
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

    internal static void ResetConfig()
    {
        Configuration = new();
        Configuration.Save();
    }
    #endregion
    #region Event Callbacks
    private void OnScratchCommand(string command, string args)
    {
        int x;
        if ( int.TryParse( args.Trim(), out x ) )
            WordsmithUI.ShowScratchPad( x );
        else if ( args.Trim().Length > 3 )
            WordsmithUI.ShowScratchPad( args );
        else
            WordsmithUI.ShowScratchPad();
    }
    internal static void ReloadManifest() => WebManifest = Git.GetManifest();
    #endregion
}