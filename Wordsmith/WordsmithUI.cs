using System;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using Wordsmith.Gui;

namespace Wordsmith
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public static class WordsmithUI
    {
        private static List<Window> _windows = new();
        public static Window[] Windows => _windows.ToArray();

        public static readonly WindowSystem WindowSystem = new WindowSystem("Wordsmith");

        // passing in the image here just for simplicity
        public static void ShowMain() => Show<ThesaurusUI>($"{Wordsmith.AppName} - Thesaurus");
        public static void ShowScratchPad(int id) => Show<ScratchPadUI>($"{Wordsmith.AppName} - Scratch Pad #{id}");
        public static void ShowSettings() => Show<SettingsUI>($"{Wordsmith.AppName} - Settings");

        private static void Show<T>(string name)
        {
            // If the given type is not a subclass of Window leave the method
            if (!typeof(T).IsSubclassOf(typeof(Window)))
                return;
            
            // Attempt to get the window by name.
            Window? w = _windows.FirstOrDefault(w => w.WindowName == name);

            // If the result is null, create a new window
            if (w == null)
                _windows.Add(Activator.CreateInstance(typeof(T)) as Window);
            
            // If the result wasn't null, open the window
            else
                w.IsOpen = true;
            
        }
        public static void RemoveWindow(Window w)
        {
            _windows.Remove(w);
            WindowSystem.RemoveWindow(w);
        }
    }
}
