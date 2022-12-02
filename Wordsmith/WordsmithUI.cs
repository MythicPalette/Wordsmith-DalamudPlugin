using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Wordsmith.Gui;
using static Wordsmith.Gui.MessageBox;

namespace Wordsmith;

internal static class WordsmithUI
{
    internal static IReadOnlyList<Window> Windows => _windowSystem.Windows;
    private static WindowSystem _windowSystem = new("Wordsmith");

    // Window queue system.
    private static bool _window_lock = false;
    private static List<Window> _removal_queue = new();
    private static List<Window> _add_queue = new();

    internal static void AddWindow( Window? w )
    {
        if ( w is null )
            return;

        if ( !_window_lock )
        {
            try
            {
                // Check if the Window already exists.
                Window? w_test = _windowSystem.GetWindow(w.WindowName);

                // If it exists, show the existing window and dispose
                // of the passed in window.
                if ( w_test != null )
                {
                    w_test.IsOpen = true;
                    if ( w is IDisposable idisp )
                        idisp.Dispose();
                }
                else
                {
                    _windowSystem.AddWindow( w );
                }
            }
            catch ( Exception e )
            {
                PluginLog.LogError( e.ToString() );
            }
        }
        else
            _add_queue.Add( w );
    }

    /// <summary>
    /// Removes all windows in the removal queue.
    /// </summary>
    internal static void CleanWindowList()
    {
        // Remove each window.
        foreach ( Window w in _removal_queue )
            RemoveWindow( w );

        foreach ( Window w in _add_queue )
            AddWindow( w );

        // Clear the list.
        _removal_queue.Clear();
        _add_queue.Clear();
    }

    internal static bool Contains( string windowName ) => _windowSystem.GetWindow( windowName ) != null;

    /// <summary>
    /// Disposes all child objects.
    /// </summary>
    internal static void Dispose()
    {
        while ( _windowSystem.Windows.Count > 0 )
        {
            // If the Window is disposable, dispose it.
            if ( _windowSystem.Windows[0] is IDisposable idisp )
                idisp?.Dispose();

            // Remove The window.
            RemoveWindow( _windowSystem.Windows[0] );
        }
    }

    /// <summary>
    /// Draw handler for interface call.
    /// </summary>
    internal static void Draw()
    {
        try
        {
            // Lock the window list.
            _window_lock = true;

            // Check if the configuration was recently saved before drawing. This is to prevent
            // resetting the RecentlySaved bool to "false" if the state changed in the middle of
            // the draw function.
            bool resetConfigSaved = Wordsmith.Configuration.RecentlySaved;

            // Draw all windows.
            _windowSystem.Draw();

            // Set RecentlySaved to false if it has already had a full cycle.
            if ( resetConfigSaved )
                Wordsmith.Configuration.RecentlySaved = false;
        }
        catch ( InvalidOperationException e )
        {
            // If the message isn't about collection being modified, log it. Otherwise
            // Discard the error.
            if ( !e.Message.StartsWith( "Collection was modified" ) )
                PluginLog.LogError( $"{e.Message}" );
        }
        catch ( Exception e ) { PluginLog.LogError( $"{e} :: {e.Message}" ); }

        // After everything, unlock and clean the window list.
        finally
        {
            // Unlock the window list.
            _window_lock = false;
            // Clean any queued windows.
            CleanWindowList();
        }
    }

    /// <summary>
    /// Removes the window from the <see cref="WindowSystem"/> and <see cref="List{T}"/>.
    /// </summary>
    /// <param name="w"><see cref="Window"/> to be removed</param>
    internal static void RemoveWindow( Window w )
    {
        // If the windows are not locked
        if ( !_window_lock )
        {
            try
            {
                try
                {
                    // If the Window can be disposed do it.
                    if ( w is IDisposable disposable )
                        disposable.Dispose();
                }
                // If the object is already disposed silently
                // drop the exception
                catch ( ObjectDisposedException ) { }


                // Remove from the WindowSystem
                if ( _windowSystem.Windows.Contains(w) )
                    _windowSystem.RemoveWindow( w );
            }
            catch ( Exception e ) { PluginLog.LogFatal( $"FATAL ERROR: {e.Message}\n{e}" ); }
        }
        // If the windows are locked queue deletion for next cycle
        else
            _removal_queue.Add( w );
    }

    internal static void ShowDebugUI() { if ( Wordsmith.Configuration.EnableDebug ) AddWindow( new DebugUI() { IsOpen = true } ); }

    #region Alerts and Messages
    internal static void ShowErrorWindow( Dictionary<string, object> d, string name ) => AddWindow( new ErrorWindow( d ) { IsOpen = true } );

    internal static void ShowMessageBox(
        string title,
        string message,
        Action<MessageBox>? callback = null,
        Vector2? size = null,
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse,
        ButtonStyle buttonStyle = ButtonStyle.OkCancel
        ) => AddWindow( new MessageBox( title, message, callback, size, flags, buttonStyle ) );

    internal static void ShowResetDictionary() => AddWindow( new MessageBox(
        $"{Wordsmith.AppName} - Reset Dictionary",
        "This will delete all entries that you added to the dictionary.This cannot be undone. Proceed?",
        ( mb ) => {
            if ( (mb.Result & DialogResult.Ok) == DialogResult.Ok )
            {
                Wordsmith.Configuration.CustomDictionaryEntries = new();
                Wordsmith.Configuration.Save();
            }
        },
        ImGuiHelpers.ScaledVector2( 300, 160 ),
        buttonStyle: ButtonStyle.OkCancel
        ) );

    internal static void ShowRestoreSettings() => AddWindow( new MessageBox(
        $"{Wordsmith.AppName} - Restore Default Settings",
        "Restoring defaults resets all settings to their original values (not including words added to your dictionary). Proceed?",
        ( mb ) =>
        {
            if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                Wordsmith.Configuration.ResetToDefault();
        },
        ImGuiHelpers.ScaledVector2( 300, 180 ),
        buttonStyle: ButtonStyle.OkCancel
        ) );
    #endregion

    /// <summary>
    /// Shows a new ScratchPad.
    /// </summary>
    internal static void ShowScratchPad() => AddWindow( new ScratchPadUI() { IsOpen = true } );

    /// <summary>
    /// Shows a ScratchPad by ID or creates a new one.
    /// </summary>
    /// <param name="id">The id of the pad to be shown.</param>
    internal static void ShowScratchPad( int id ) { if ( !ShowWindow( ScratchPadUI.CreateWindowName(id) ) ) AddWindow(new ScratchPadUI(id) { IsOpen = true } ); }

    /// <summary>
    /// Creates or shows the ScratchPadHelpUI help <see cref="Window"/>
    /// </summary>
    internal static void ShowScratchPadHelp() { if ( !ShowWindow( typeof( ScratchPadHelpUI ) ) ) AddWindow( new ScratchPadHelpUI() { IsOpen = true } ); }
    
    /// <summary>
    /// Creates or shows the SettingsUI <see cref="Window"/>
    /// </summary>
    internal static void ShowSettings() { if ( !ShowWindow( typeof( SettingsUI ) ) ) AddWindow( new SettingsUI() { IsOpen = true } ); }

    /// <summary>
    /// Shows and/or creates ThesaurusUI.
    /// </summary>
    internal static void ShowThesaurus() { if ( !ShowWindow( typeof( ThesaurusUI ) ) ) AddWindow( new ThesaurusUI() { IsOpen = true } ); }

    /// <summary>
    /// Shows a <see cref="Window"/> if it exists.
    /// </summary>
    /// <param name="str">The name of the <see cref="Window"/> to show.</param>
    /// <returns><see langword="true"/> if a window was shown.</returns>
    internal static bool ShowWindow(string str)
    {
        Window? w = Windows.FirstOrDefault(x => x.WindowName == str);
        if ( w is null )
            return false;

        w.IsOpen = true;
        return true;
    }

    /// <summary>
    /// Shows a <see cref="Window"/> if it exists
    /// </summary>
    /// <param name="t">The <see cref="Type"/> of the <see cref="Window"/> to show.</param>
    /// <returns><see langword="true"/> if the window was shown.</returns>
    internal static bool ShowWindow(Type t)
    {
        Window? w = Windows.FirstOrDefault(x => x.GetType() == t);
        if ( w == null )
            return false;

        w.IsOpen = true;
        return true;
    }

    /// <summary>
    /// Update handler for framework call
    /// </summary>
    internal static void Update()
    {
        foreach ( Window w in _windowSystem.Windows )
            w.Update();
    }
}
