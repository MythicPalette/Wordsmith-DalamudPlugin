using System;
using Dalamud.Interface.Windowing;
using Wordsmith.Gui;

namespace Wordsmith
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public static class WordsmithUI
    {
        private static ThesaurusUI? _ThesaurusUI;
        private static SettingsUI? _SettingsUI;

        public static readonly WindowSystem WindowSystem = new WindowSystem("Wordsmith");

        // passing in the image here just for simplicity
        public static void ShowMain() => Show<ThesaurusUI>(ref _ThesaurusUI);
        public static void ShowSettings() => Show<SettingsUI>(ref _SettingsUI);

        private static void Show<T>(ref T? window)
        {
            if (window is not null && !((window as WordsmithWindow)?.Disposed ?? true))
                (window as WordsmithWindow).IsOpen = true;
            else
                window = (T)Activator.CreateInstance(typeof(T));
        }
    }
}
