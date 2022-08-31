using Dalamud.Interface.Windowing;
using Wordsmith.Gui;

namespace Wordsmith;

// It is good to have this be disposable in general, in case you ever need it
// to do any cleanup
internal static class WordsmithUI
{
    internal static IReadOnlyList<Window> Windows => WindowSystem.Windows;
    internal static WindowSystem WindowSystem { get; private set; } = new("Wordsmith");

    // passing in the image here just for simplicity
    internal static void ShowThesaurus() => Show<ThesaurusUI>($"{Wordsmith.AppName} - Thesaurus");
    internal static void ShowScratchPad(int id) => Show<ScratchPadUI>($"{Wordsmith.AppName} - Scratch Pad #{id}");
    internal static void ShowScratchPad( string tellTarget ) => Show<ScratchPadUI>( new ScratchPadUI( tellTarget ) );
    internal static void ShowScratchPadHelp() => Show<ScratchPadHelpUI>($"{Wordsmith.AppName} - Scratch Pad Help");
    internal static void ShowSettings() => Show<SettingsUI>($"{Wordsmith.AppName} - Settings");
    internal static void ShowRestoreSettings() => Show<RestoreDefaultsUI>($"{Wordsmith.AppName} - Restore Default Settings");
    internal static void ShowResetDictionary() => Show<ResetDictionaryUI>($"{Wordsmith.AppName} - Reset Dictionary");
    internal static void ShowErrorWindow( Dictionary<string, object> d, string name ) => WindowSystem.AddWindow( new ErrorWindow( d ) { IsOpen = true } );

    // Window removal queue system.
    private static bool _window_lock = false;
    private static List<Window> _removal_queue = new();

#if DEBUG
    internal static void ShowDebugUI() => Show<DebugUI>( $"{Wordsmith.AppName} - Debug" );
#endif

    /// <summary>
    /// Shows the window with the specified name or creates a new one.
    /// </summary>
    /// <typeparam name="T"><see cref="Window"/> child class to show.</typeparam>
    /// <param name="name"><see cref="string"/> name of the <see cref="Window"/></param>
    private static void Show<T>(object obj)
    {
        // If the given type is not a subclass of Window leave the method
        if (!typeof(T).IsSubclassOf(typeof(Window)))
            return;

        Window? w = null;
        if ( obj is string name )
        {
            // Attempt to get the window by name.
            w = WindowSystem.GetWindow( name );
        }
        else if ( obj is Window )
            w = obj as Window;

        // If the result is null, create a new window
        if ( w is null )
        {
            // Create the Window object
            w = (Activator.CreateInstance( typeof( T ) ) as Window)!;

            // If the object was successfully created
            if ( w != null )
            {
                // Open it
                w.IsOpen = true;

                // Add it to the WindowSystem.
                WindowSystem.AddWindow( w );
                return;
            }
        }

        // If the result wasn't null, open the window
        else
        {
            if (!WindowSystem.Windows.Contains(w))
                WindowSystem.AddWindow( w );

            w.IsOpen = true;
            return;
        }
    }

    /// <summary>
    /// Removes the window from the <see cref="WindowSystem"/> and <see cref="List{T}"/>.
    /// </summary>
    /// <param name="w"><see cref="Window"/> to be removed</param>
    internal static void RemoveWindow(Window w)
    {
        // If the windows are not locked
        if ( !_window_lock )
        {
            try
            {
                // If the Window can be disposed do it.
                if ( w is IDisposable disposable )
                    disposable.Dispose();

                // Remove from the WindowSystem
                WindowSystem.RemoveWindow( w );
            }
            catch (Exception e)
            {
                PluginLog.LogFatal( $"FATAL ERROR: {e.Message}\n{e}" );
            }
        }
        // If the windows are locked queue deletion for next cycle
        else
            _removal_queue.Add( w );
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
            WindowSystem.Draw();            

            // Set RecentlySaved to false if it has already had a full cycle.
            if (resetConfigSaved)
                Wordsmith.Configuration.RecentlySaved = false;
        }
        catch (InvalidOperationException e)
        {
            // If the message isn't about collection being modified, log it. Otherwise
            // Discard the error.
            if (!e.Message.StartsWith("Collection was modified"))
                PluginLog.LogError($"{e.Message}");
        }
        catch (Exception e) { PluginLog.LogError($"{e} :: {e.Message}"); }

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
    /// Removes all windows in the removal queue.
    /// </summary>
    internal static void CleanWindowList()
    {
        // If the queue is null or empty return.
        if ( _removal_queue is null )
            return;
        if ( _removal_queue.Count < 1 )
            return;

        // Remove each window.
        foreach ( Window w in _removal_queue )
            RemoveWindow( w );

        // Clear the list.
        _removal_queue.Clear();
    }

    /// <summary>
    /// Update handler for framework call
    /// </summary>
    internal static void Update()
    {
        foreach ( Window w in WindowSystem.Windows )
            w.Update();
    }

    /// <summary>
    /// Disposes all child objects.
    /// </summary>
    internal static void Dispose()
    {
        while ( WindowSystem.Windows.Count > 0 )
            RemoveWindow( WindowSystem.Windows[0] );
    }
}
