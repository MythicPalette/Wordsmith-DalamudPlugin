global using System;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Numerics;
global using System.Collections.Generic;
global using Dalamud.Logging;

using Dalamud.Interface;

namespace Wordsmith;

internal static class Global
{
    internal const int BUTTON_Y = 25;
    internal static float BUTTON_Y_SCALED => BUTTON_Y * ImGuiHelpers.GlobalScale;
}
