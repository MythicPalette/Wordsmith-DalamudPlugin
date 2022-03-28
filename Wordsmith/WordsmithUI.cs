using Dalamud.Interface.Windowing;
using Wordsmith.Gui;

namespace Wordsmith;

// It is good to have this be disposable in general, in case you ever need it
// to do any cleanup
internal static class WordsmithUI
{
    private static List<Window> _windows { get; set; } = new();
    internal static IReadOnlyList<Window> Windows => _windows;
    internal static WindowSystem WindowSystem { get; private set; } = new("Wordsmith");

    // passing in the image here just for simplicity
    internal static void ShowThesaurus() => Show<ThesaurusUI>($"{Wordsmith.AppName} - Thesaurus");
    internal static void ShowScratchPad(int id) => Show<ScratchPadUI>($"{Wordsmith.AppName} - Scratch Pad #{id}");
    internal static void ShowScratchPad( string tellTarget ) => Show<ScratchPadUI>( new ScratchPadUI( tellTarget ) );
    //{
    //    ScratchPadUI pad = new ScratchPadUI( tellTarget );
    //    pad.IsOpen = true;

    //    _windows.Add( pad );
    //    WindowSystem.AddWindow( pad );
    //}
    internal static void ShowScratchPadHelp() => Show<ScratchPadHelpUI>($"{Wordsmith.AppName} - Scratch Pad Help");
    internal static void ShowSettings() => Show<SettingsUI>($"{Wordsmith.AppName} - Settings");
    internal static void ShowRestoreSettings() => Show<RestoreDefaultsUI>($"{Wordsmith.AppName} - Restore Default Settings");
    internal static void ShowResetDictionary() => Show<ResetDictionaryUI>($"{Wordsmith.AppName} - Reset Dictionary");
    internal static void ShowDebugUI() => Show<DebugUI>( $"{Wordsmith.AppName} - Debug" );

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

                // Add it to the list.
                _windows.Add( w );
            }
        }

        // If the result wasn't null, open the window
        else
        {
            if (!_windows.Contains(w))
            {
                _windows.Add( w );
                WindowSystem.AddWindow( w );
            }
            w.IsOpen = true;
        }
    }

    /// <summary>
    /// Removes the window from the <see cref="WindowSystem"/> and <see cref="List{T}"/>.
    /// </summary>
    /// <param name="w"><see cref="Window"/> to be removed</param>
    internal static void RemoveWindow(Window w)
    {
        // If the Window can be disposed do it.
        if (w is IDisposable disposable)
            disposable.Dispose();

        // Remove from the list
        _windows.Remove(w);

        // Remove from the WindowSystem
        WindowSystem.RemoveWindow(w);
    }

    /// <summary>
    /// Draw handler for interface call.
    /// </summary>
    internal static void Draw()
    {
        try
        {
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
    }

    /// <summary>
    /// Update handler for framework call
    /// </summary>
    internal static void Update()
    {
        foreach ( Window w in _windows )
            w.Update();
    }

    /// <summary>
    /// Disposes all child objects.
    /// </summary>
    internal static void Dispose()
    {
        Window[] windows = _windows.ToArray();
        foreach ( Window w in windows )
            RemoveWindow( w );
    }
}
