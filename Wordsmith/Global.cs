global using System;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Numerics;
global using System.Collections.Generic;
global using Dalamud.Logging;

global using Wordsmith.Interfaces;
namespace Wordsmith;

internal static class Global
{
    internal static ImGuiScene.TextureWrap MW_LOGO;
    internal const int BUTTON_Y = 25;
    #region Strings
    internal const string SPACED_WRAP_MARKER = "\r\r";
    internal const string NOSPACE_WRAP_MARKER = "\r";
    internal const string SPELL_CHECK_NOTICE = "Checking your spelling...";

    internal const string MANIFEST_JSON_URL = "https://raw.githubusercontent.com/LadyDefile/WordsmithDictionaries/main/manifest.json";
    internal const string LIBRARY_FILE_URL = "https://raw.githubusercontent.com/LadyDefile/WordsmithDictionaries/main/library";
    #endregion

    #region Keys
    internal const int ENTER_KEY = 0xD;
    #endregion

    #region NoticeID
    internal const int CORRECTIONS_FOUND = -1;

    internal const int CHECKING_SPELLING = 1;
    internal const int CORRECTIONS_NOT_FOUND = 2;
    #endregion

    #region ScratchPad
    internal const int EDITING_TEXT = 0;
    internal const int VIEWING_HISTORY = 1;
    internal const int TEXTINPUT_LINES = 5;
    #endregion
}
