using System.Runtime.InteropServices;
using Dalamud.Interface;
using ImGuiNET;

/// <summary>
/// Most of the code in this file is from https://git.annaclemens.io/ascclemens/ChatTwo/src/branch/main
/// </summary>

namespace Wordsmith.Helpers
{
    public class FontBuilder : IDisposable
    {
        internal ImFontPtr? RegularFont { get; private set; }
        internal ImFontPtr? ItalicFont { get; private set; }
        internal Vector4 DefaultText { get; private set; }

        private ImFontConfigPtr _fontCfg;
        private ImFontConfigPtr _fontCfgMerge;
        private (GCHandle, int, float) _regularFont;
        private (GCHandle, int, float) _italicFont;
        private (GCHandle, int, float) _jpFont;
        private (GCHandle, int) _gameSymFont;

        private readonly ImVector _ranges;
        private readonly ImVector _jpRange;

        private GCHandle _symRange = GCHandle.Alloc(
            new ushort[] {
            0xE020,
            0xE0DB,
            0,
            },
            GCHandleType.Pinned
        );

        public bool Enabled { get => this.RegularFont.HasValue; }


        internal unsafe FontBuilder()
        {
            this._fontCfg = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
            {
                FontDataOwnedByAtlas = false,
            };

            this._fontCfgMerge = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
            {
                FontDataOwnedByAtlas = false,
                MergeMode = true,
            };

            void BuildRange(out ImVector result, IReadOnlyList<ushort>? chars, params IntPtr[] ranges)
            {
                var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
                // text
                foreach (var range in ranges)
                    builder.AddRanges(range);

                // chars
                if (chars != null)
                {
                    for (var i = 0; i < chars.Count; i += 2)
                    {
                        if (chars[i] == 0)
                            break;

                        for (var j = (uint)chars[i]; j <= chars[i + 1]; j++)
                            builder.AddChar((ushort)j);
                    }
                }

                // various symbols
                builder.AddText("←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～");

                // French
                builder.AddText("Œœ");

                // Romanian
                builder.AddText("ĂăÂâÎîȘșȚț");

                // "Enclosed Alphanumerics" (partial) https://www.compart.com/en/unicode/block/U+2460
                for (var i = 0x2460; i <= 0x24B5; i++)
                {
                    builder.AddChar((char)i);
                }

                builder.AddChar('⓪');

                builder.BuildRanges(out result);
                builder.Destroy();
            }
            BuildRange(out this._ranges, null, ImGui.GetIO().Fonts.GetGlyphRangesDefault());
            BuildRange(out this._jpRange, GlyphRangesJapanese.GlyphRanges);
            this.SetUpUserFonts();

            var gameSym = File.ReadAllBytes(Path.Combine(Wordsmith.PluginInterface.DalamudAssetDirectory.FullName, "UIRes", "gamesym.ttf"));
            this._gameSymFont = (
                GCHandle.Alloc(gameSym, GCHandleType.Pinned),
                gameSym.Length
            );

            var uiBuilder = Wordsmith.PluginInterface.UiBuilder;
            uiBuilder.DisableCutsceneUiHide = true;
            uiBuilder.DisableGposeUiHide = true;

            uiBuilder.BuildFonts += this.BuildFonts;
            uiBuilder.RebuildFonts();                        
        }

        public void Dispose()
        {
            Wordsmith.PluginInterface.UiBuilder.BuildFonts -= this.BuildFonts;

            if (this._regularFont.Item1.IsAllocated)
                this._regularFont.Item1.Free();

            if (this._italicFont.Item1.IsAllocated)
                this._italicFont.Item1.Free();

            if (this._jpFont.Item1.IsAllocated)
                this._jpFont.Item1.Free();

            if (this._gameSymFont.Item1.IsAllocated)
                this._gameSymFont.Item1.Free();

            if (this._symRange.IsAllocated)
                this._symRange.Free();

            this._fontCfg.Destroy();
            this._fontCfgMerge.Destroy();
        }

        private void SetUpUserFonts()
        {
            FontData? fontData = null;
            //if (this.Plugin.Config.GlobalFont.StartsWith(Fonts.IncludedIndicator))
            //{
            //    var globalFont = Fonts.GlobalFonts.FirstOrDefault(font => font.Name == this.Plugin.Config.GlobalFont);
            //    if (globalFont != null)
            //    {
            //        var regular = new FaceData(this.GetResource(globalFont.ResourcePath), 1f);
            //        var italic = new FaceData(this.GetResource(globalFont.ResourcePathItalic), 1f);
            //        fontData = new FontData(regular, italic);
            //    }
            //}
            //else
            //{
            fontData = Fonts.GetFont("Balakhani", true);
            //}

            if (fontData == null) return;
            PluginLog.LogDebug("Got the font.");
            //{
            //    this.Plugin.Config.GlobalFont = Fonts.GlobalFonts[0].Name;
            //    this.Plugin.SaveConfig();

            //    var globalFont = Fonts.GlobalFonts[0];
            //    var regular = new FaceData(this.GetResource(globalFont.ResourcePath), 1f);
            //    var italic = new FaceData(this.GetResource(globalFont.ResourcePathItalic), 1f);
            //    fontData = new FontData(regular, italic);
            //}

            if (this._regularFont.Item1.IsAllocated)
            {
                this._regularFont.Item1.Free();
            }

            if (this._italicFont.Item1.IsAllocated)
            {
                this._italicFont.Item1.Free();
            }

            this._regularFont = (
                GCHandle.Alloc(fontData.Regular.Data, GCHandleType.Pinned),
                fontData.Regular.Data.Length,
                fontData.Regular.Ratio
            );

            this._italicFont = (
                GCHandle.Alloc(fontData.Italic!.Data, GCHandleType.Pinned),
                fontData.Italic.Data.Length,
                fontData.Italic.Ratio
            );

            //FontData? jpFontData = null;
            //if (this.Plugin.Config.JapaneseFont.StartsWith(Fonts.IncludedIndicator))
            //{
            //    var jpFont = Fonts.JapaneseFonts.FirstOrDefault(item => item.Item1 == this.Plugin.Config.JapaneseFont);
            //    if (jpFont != default)
            //    {
            //        jpFontData = new FontData(
            //            new FaceData(this.GetResource(jpFont.Item2), 1f),
            //            null
            //        );
            //    }
            //}
            //else
            //{
            //  jpFontData = Fonts.GetFont(this.Plugin.Config.JapaneseFont, false);
            //}

            //if (jpFontData == null)
            //{
            //    this.Plugin.Config.JapaneseFont = Fonts.JapaneseFonts[0].Item1;
            //    this.Plugin.SaveConfig();

            //    var jpFont = Fonts.JapaneseFonts[0];
            //    jpFontData = new FontData(
            //        new FaceData(this.GetResource(jpFont.Item2), 1f),
            //        null
            //    );
            //}

            //if (this._jpFont.Item1.IsAllocated)
            //{
            //    this._jpFont.Item1.Free();
            //}

            //this._jpFont = (
            //    GCHandle.Alloc(jpFontData.Regular.Data, GCHandleType.Pinned),
            //    jpFontData.Regular.Data.Length,
            //    jpFontData.Regular.Ratio
            //);
        }

        private void BuildFonts()
        {
            try
            {
                this.RegularFont = null;
                this.ItalicFont = null;

                PluginLog.LogDebug($"{_regularFont.Item1}");
                PluginLog.LogDebug($"{_regularFont.Item2}");
                // load regular noto sans and merge in jp + game icons
                this.RegularFont = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                    this._regularFont.Item1.AddrOfPinnedObject(),
                    this._regularFont.Item2,
                    Wordsmith.Configuration.FontSize,
                    this._fontCfg,
                    this._ranges.Data
                );

                //ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                //    this._jpFont.Item1.AddrOfPinnedObject(),
                //    this._jpFont.Item2,
                //    Wordsmith.Configuration.JpFontSize,
                //    this._fontCfgMerge,
                //    this._jpRange.Data
                //);

                ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                    this._gameSymFont.Item1.AddrOfPinnedObject(),
                    this._gameSymFont.Item2,
                    Wordsmith.Configuration.JpFontSize,
                    this._fontCfgMerge,
                    this._symRange.AddrOfPinnedObject()
                );

                // load italic noto sans and merge in jp + game icons
                this.ItalicFont = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                    this._italicFont.Item1.AddrOfPinnedObject(),
                    this._italicFont.Item2,
                    Wordsmith.Configuration.FontSize,
                    this._fontCfg,
                    this._ranges.Data
                );

                //ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                //    this._jpFont.Item1.AddrOfPinnedObject(),
                //    this._jpFont.Item2,
                //    Wordsmith.Configuration.JpFontSize,
                //    this._fontCfgMerge,
                //    this._jpRange.Data
                //);

                ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                    this._gameSymFont.Item1.AddrOfPinnedObject(),
                    this._gameSymFont.Item2,
                    Wordsmith.Configuration.JpFontSize,
                    this._fontCfgMerge,
                    this._symRange.AddrOfPinnedObject()
                );
            }
            catch (Exception e)
            {
                PluginLog.LogError($"{e}");
            }
        }
    }
}
