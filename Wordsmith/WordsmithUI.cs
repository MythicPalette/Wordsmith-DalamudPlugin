using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Wordsmith.Gui;
using static Wordsmith.Gui.MessageBox;

namespace Wordsmith;

internal static class WordsmithUI
{
    /// <summary>
    /// Returns a readonly list containing all currently registered windows.
    /// </summary>
    internal static IReadOnlyList<Window> Windows => _windowSystem.Windows;
    private static WindowSystem _windowSystem = new("Wordsmith");

    #region Window Queue
    /// <summary>
    /// The window lock prevents changing the collection of windows while
    /// in the process of drawing them. This is to ensure that the window
    /// collection is not modified during the draw cycle.
    /// </summary>
    private static bool _window_lock = false;

    /// <summary>
    /// This queue is the windows that were requested to be removed but
    /// have not yet been. The purpose of this queue is to store remove
    /// requests until after the window lock has been disabled and then
    /// remove them.
    /// </summary>
    private static List<Window> _removal_queue = new();

    /// <summary>
    /// This queue is the windows that were requested to be added but
    /// have not yet been. The purpose of this queue is to store add
    /// requests until after the window lock has been disabled and then
    /// add them.
    /// </summary>
    private static List<Window> _add_queue = new();
    #endregion

    /// <summary>
    /// Adds a window object to the <see cref="WindowSystem"/>.
    /// If the window lock is engaged the window will be stored in a
    /// queue.
    /// </summary>
    /// <param name="w"></param>
    internal static void AddWindow( Window? w )
    {
        // If the window is null then abort
        if ( w is null )
            return;

        // Check if the Window already exists.
        Window? w_test = _windowSystem.GetWindow(w.WindowName);

        // If the window already exists in the Window System show the
        // existing Window and dispose of the given Window object.
        if ( w_test != null )
        {
            w_test.IsOpen = true;
            if ( w is IDisposable idisp )
                idisp.Dispose();
        }

        // If the Window doesn't exist and the lock is not
        // engaged then add the window to the list
        else if ( !_window_lock )
            _windowSystem.AddWindow( w );

        // Lastly, if the Window does not exist in the system and
        // the lock is engaged then add it to the queue to be added
        // at a later time when the lock is removed.
        else
            _add_queue.Add( w );
    }

    /// <summary>
    /// Removes all windows in the removal queue.
    /// </summary>
    internal static void CleanWindowList()
    {
        // If the window lock is engaged then abort to prevent
        // losing windows in the process.
        if ( _window_lock )
            return;

        // Remove each window that is queued for removal
        foreach ( Window w in _removal_queue )
            RemoveWindow( w );

        // Add each window that is queued for adding
        foreach ( Window w in _add_queue )
            AddWindow( w );

        // Clear the queue lists.
        _removal_queue.Clear();
        _add_queue.Clear();
    }

    /// <summary>
    /// Checks if the private <see cref="WindowSystem"/> contains a specific <see cref="Window"/>
    /// </summary>
    /// <param name="windowName"><see cref="string"/> name of the Window.</param>
    /// <returns><see langword="true"/> if the <see cref="Window"/> exists.</returns>
    internal static bool Contains( string windowName ) => _windowSystem.GetWindow( windowName ) != null;

    /// <summary>
    /// Disposes of all child objects.
    /// </summary>
    internal static void Dispose()
    {
        // Dispose of all contained Windows.
        while ( _windowSystem.Windows.Count > 0 )
        {
            // If the Window is disposable, dispose it.
            if ( _windowSystem.Windows[0] is IDisposable idisp )
                idisp?.Dispose();

            // Remove the Window.
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

            // Set RecentlySaved to false after having gone a full draw cycle in the recently saved
            // state. This ensures all applicable objects have had opportunity to read the new changes.
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

    internal static Window? GetWindow( string name ) => _windowSystem.GetWindow( name );

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

    /// <summary>
    /// Shows the DebugUI Gui. This is for dev and troubleshooting purposes only.
    /// </summary>
    internal static void ShowDebugUI() { if ( Wordsmith.Configuration.EnableDebug ) AddWindow( new DebugUI() { IsOpen = true } ); }

    #region Alerts and Messages
    /// <summary>
    /// Show an error window to the user.
    /// </summary>
    /// <param name="d"><see cref="Dictionary{TKey, TValue}"/> data packet for sending to clipboard.</param>
    /// <param name="name"><see cref="string"/> error/Window name.</param>
    internal static void ShowErrorWindow( Dictionary<string, object> d, string name ) => AddWindow( new ErrorWindow( d ) { IsOpen = true } );

    /// <summary>
    /// Show a message box to the user.
    /// </summary>
    /// <param name="title">The <see cref="string"/> title to display at the top of the window.</param>
    /// <param name="message">The <see cref="string"/> message body to display.</param>
    /// <param name="callback">The function to call when the message box closes.</param>
    /// <param name="buttonStyle"><see cref="ButtonStyle"/> to apply to the message box window.</param>
    internal static void ShowMessageBox(
        string title,
        string message,
        ButtonStyle buttonStyle = ButtonStyle.OkCancel,
        Action<MessageBox>? callback = null
        ) => AddWindow( new MessageBox( title, message, buttonStyle, callback ) );
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

    internal static void ShowScratchPad( string str ) { if ( !ShowWindow( ScratchPadUI.CreateWindowName(str))) AddWindow(new ScratchPadUI(str) { IsOpen= true } ); }

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
