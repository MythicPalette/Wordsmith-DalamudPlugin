using Dalamud.Interface.Windowing;
using Wordsmith.Gui;
using Wordsmith.Helpers;

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
    internal static void ShowScratchPad(string tellTarget) => _windows.Add(new ScratchPadUI(tellTarget));
    internal static void ShowScratchPadHelp() => Show<ScratchPadHelpUI>($"{Wordsmith.AppName} - Scratch Pad Help");
    internal static void ShowSettings() => Show<SettingsUI>($"{Wordsmith.AppName} - Settings");
    internal static void ShowRestoreSettings() => Show<RestoreDefaultsUI>($"{Wordsmith.AppName} - Restore Default Settings");
    internal static void ShowResetDictionary() => Show<ResetDictionaryUI>($"{Wordsmith.AppName} - Reset Dictionary");
    internal static void ShowDebugUI() => Show<DebugUI>( $"{Wordsmith.AppName} - Debug" );

    private static void Show<T>(string name)
    {
        // If the given type is not a subclass of Window leave the method
        if (!typeof(T).IsSubclassOf(typeof(Window)))
            return;
        
        // Attempt to get the window by name.
        Window? w = WindowSystem.GetWindow(name);

        // If the result is null, create a new window
        if ( w == null )
        {
            w = (Activator.CreateInstance( typeof( T ) ) as Window)!;
            if (w != null)
            {
                w.IsOpen = true;
                WindowSystem.AddWindow(w);
                _windows.Add(w);
            }
        }

        // If the result wasn't null, open the window
        else
            w.IsOpen = true;
        
    }
    internal static void RemoveWindow(Window w)
    {
        _windows.Remove(w);
        WindowSystem.RemoveWindow(w);
    }

    internal static void Draw()
    {
        try
        {
            // Check if the configuration was recently saved before drawing. This is to prevent
            // resetting the RecentlySaved bool to "false" if the state changed in the middle of
            // the draw function.
            bool resetConfigSaved = Wordsmith.Configuration.RecentlySaved;

            // Draw all windows.
            WordsmithUI.WindowSystem.Draw();

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

    internal static void Dispose()
    {
        Window[] windows = _windows.ToArray();
        foreach (Window w in windows)
            RemoveWindow(w);
    }
}
