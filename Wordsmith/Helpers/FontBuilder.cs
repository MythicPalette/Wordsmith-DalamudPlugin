using System.Runtime.InteropServices;
using Dalamud.Interface;
using ImGuiNET;

namespace Wordsmith.Helpers
{
    public class FontBuilder : IDisposable
    {
        internal ImFontPtr? RegularFont { get; private set; }

        private ImFontConfigPtr _fontCfg;
        private ImFontConfigPtr _fontCfgMerge;
        private (GCHandle Handle, int Size) _regularFont;
        private (GCHandle Handle, int Size) _italicFont;
        private (GCHandle Handle, int Size) _jpFont;
        private (GCHandle Handle, int Size) _gameSymFont;

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
            // For regular font
            this._fontCfg = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
            {
                FontDataOwnedByAtlas = false
            };

            // Enable merging for italic, jp, and symbols so they all become part of the same
            // font object in the atlas.
            this._fontCfgMerge = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
            {
                FontDataOwnedByAtlas = false,
                MergeMode = true
            };

            BuildRange(out this._ranges, null, ImGui.GetIO().Fonts.GetGlyphRangesDefault());
            BuildRange(out this._jpRange, GlyphRangesJapanese.GlyphRanges);
            this.SetupFont();

            byte[] gameSym = File.ReadAllBytes(Path.Combine(Wordsmith.PluginInterface.DalamudAssetDirectory.FullName, "UIRes", "gamesym.ttf"));
            this._gameSymFont = (
                GCHandle.Alloc(gameSym, GCHandleType.Pinned),
                gameSym.Length
            );

            Wordsmith.PluginInterface.UiBuilder.BuildFonts += this.BuildFonts;
            Wordsmith.PluginInterface.UiBuilder.RebuildFonts();
        }

        private static unsafe void BuildRange(out ImVector result, IReadOnlyList<ushort>? chars, params IntPtr[] ranges)
        {
            ImFontGlyphRangesBuilderPtr builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());

            // text
            foreach (IntPtr range in ranges)
                builder.AddRanges(range);

            // chars
            if (chars != null)
            {
                for (int i = 0; i < chars.Count; i += 2)
                {
                    if (chars[i] == 0)
                        break;

                    for (uint j = chars[i]; j <= chars[i + 1]; j++)
                        builder.AddChar((ushort)j);
                }
            }

            // various symbols
            builder.AddText("←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～");

            // French
            builder.AddText("Œœ");

            // Romanian
            builder.AddText("ĂăÂâÎîȘșȚț");

            // Enclosed Alphanumerics
            for (var i = 0x2460; i <= 0x24B5; i++)
                builder.AddChar((char)i);

            builder.AddChar('⓪');

            builder.BuildRanges(out result);
                builder.Destroy();
        }

        public void Dispose()
        {
            Wordsmith.PluginInterface.UiBuilder.BuildFonts -= this.BuildFonts;

            // Free regular
            if (this._regularFont.Item1.IsAllocated)
                this._regularFont.Item1.Free();

            // Free italic
            if (this._italicFont.Item1.IsAllocated)
                this._italicFont.Item1.Free();

            // Free JP
            if (this._jpFont.Item1.IsAllocated)
                this._jpFont.Item1.Free();

            // Free game
            if (this._gameSymFont.Item1.IsAllocated)
                this._gameSymFont.Item1.Free();

            // Free symbols
            if (this._symRange.IsAllocated)
                this._symRange.Free();

            this._fontCfg.Destroy();
            this._fontCfgMerge.Destroy();
        }

        private void SetupFont()
        {
            if (this._regularFont.Item1.IsAllocated)
                this._regularFont.Item1.Free();

            if (this._italicFont.Item1.IsAllocated)
                this._italicFont.Item1.Free();

            if (this._jpFont.Item1.IsAllocated)
                this._jpFont.Item1.Free();

            byte[] regular = this.GetResource("Wordsmith.Resources.NotoSans-Regular.ttf");
            this._regularFont = (
                GCHandle.Alloc(regular, GCHandleType.Pinned),
                regular.Length
            );

            byte[] italic = this.GetResource("Wordsmith.Resources.NotoSans-Italic.ttf");
            this._italicFont = (
                GCHandle.Alloc(italic, GCHandleType.Pinned),
                italic.Length
                );

            byte[] jp = this.GetResource("Wordsmith.Resources.NotoSansJP-Regular.otf");
            this._jpFont = (
                GCHandle.Alloc(jp, GCHandleType.Pinned),
                jp.Length
                );
        }

        private byte[] GetResource(string name)
        {
            using (Stream? stream = this.GetType().Assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    return Array.Empty<byte>();

                using (MemoryStream mem = new())
                {
                    stream.CopyTo(mem);
                    return mem.ToArray();
                }
            }
        }

        private void BuildFonts()
        {
            this.RegularFont = null;

            // load regular noto sans and merge in jp + game icons
            this.RegularFont = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                this._regularFont.Item1.AddrOfPinnedObject(),
                this._regularFont.Item2,
                Wordsmith.Configuration.FontSize,
                this._fontCfg,
                this._ranges.Data
            );

            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                this._jpFont.Item1.AddrOfPinnedObject(),
                this._jpFont.Item2,
                Wordsmith.Configuration.JpFontSize,
                this._fontCfgMerge,
                this._jpRange.Data
            );

            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                this._gameSymFont.Item1.AddrOfPinnedObject(),
                this._gameSymFont.Item2,
                Wordsmith.Configuration.SymbolFontSize,
                this._fontCfgMerge,
                this._symRange.AddrOfPinnedObject()
            );
        }
    }
}
