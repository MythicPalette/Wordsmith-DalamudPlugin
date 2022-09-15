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
    #region Strings
    internal const string SPACED_WRAP_MARKER = "\r\r";
    internal const string NOSPACE_WRAP_MARKER = "\r";
    internal const string SPELL_CHECK_NOTICE = "Checking your spelling...";
    #endregion

    #region Keys
    internal const int ENTER_KEY = 0xD;
    #endregion

    #region NoticeID
    internal const int CORRECTIONS_FOUND = -1;

    internal const int CHECKING_SPELLING = 1;
    internal const int CORRECTIONS_NOT_FOUND = 2;
    #endregion

    #region Scratch Pad Editor State
    internal const int EDITING_TEXT = 0;
    internal const int VIEWING_HISTORY = 1;
    #endregion
}
