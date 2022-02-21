using Dalamud.Interface.Windowing;
using Wordsmith.Gui;
using Wordsmith.Helpers;

namespace Wordsmith
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public static class WordsmithUI
    {
        private static List<Window> _windows { get; set; } = new();
        public static Window[] Windows { get => _windows.ToArray(); }

        public static WindowSystem WindowSystem { get; private set; } = new("Wordsmith");

        public static FontBuilder FontBuilder { get; private set; } = null!;
        //public static void Init() => FontBuilder = new();

        // passing in the image here just for simplicity
        public static void ShowThesaurus() => Show<ThesaurusUI>($"{Wordsmith.AppName} - Thesaurus");
        public static void ShowScratchPad(int id) => Show<ScratchPadUI>($"{Wordsmith.AppName} - Scratch Pad #{id}");
        public static void ShowScratchPad(string tellTarget) => _windows.Add(new ScratchPadUI(tellTarget));
        public static void ShowScratchPadHelp() => Show<ScratchPadHelpUI>($"{Wordsmith.AppName} - Scratch Pad Help");
        public static void ShowSettings() => Show<SettingsUI>($"{Wordsmith.AppName} - Settings");
        public static void ShowRestoreSettings() => Show<RestoreDefaultsUI>($"{Wordsmith.AppName} - Restore Default Settings");
        public static void ShowResetDictionary() => Show<ResetDictionaryUI>($"{Wordsmith.AppName} - Reset Dictionary");

        private static void Show<T>(string name)
        {
            // If the given type is not a subclass of Window leave the method
            if (!typeof(T).IsSubclassOf(typeof(Window)))
                return;
            
            // Attempt to get the window by name.
            Window? w = _windows.FirstOrDefault(w => w.WindowName == name);

            // If the result is null, create a new window
            if (w == null)
                _windows.Add((Activator.CreateInstance(typeof(T)) as Window)!);

                // If the result wasn't null, open the window
            else
                w.IsOpen = true;
            
        }
        public static void RemoveWindow(Window w)
        {
            _windows.Remove(w);
            WindowSystem.RemoveWindow(w);
            if (w is ScratchPadUI pad)
                pad.Dispose();
        }

        public static void Draw()
        {
            try { WordsmithUI.WindowSystem.Draw(); }
            catch (InvalidOperationException e)
            {
                // If the message isn't about collection being modified, log it. Otherwise
                // Discard the error.
                if (!e.Message.StartsWith("Collection was modified"))
                    PluginLog.LogError($"{e.Message}");
            }
            catch (Exception e) { PluginLog.LogError($"{e} :: {e.Message}"); }
        }

        public static void Dispose()
        {
            FontBuilder?.Dispose();
            Window[] windows = _windows.ToArray();
            foreach (Window w in windows)
                RemoveWindow(w);
        }
    }
}
