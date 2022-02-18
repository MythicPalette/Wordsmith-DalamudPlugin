using System.Runtime.InteropServices;
using SharpDX.DirectWrite;
using FontStyle = SharpDX.DirectWrite.FontStyle;

namespace Wordsmith.Helpers
{
    /// <summary>
    /// This class is taken directly from https://git.annaclemens.io/ascclemens/ChatTwo/src/branch/main
    /// </summary>
    
    // TODO rewrite class with own font implementation.
    internal static class Fonts
    {
        internal static List<string> GetFonts()
        {
            var fonts = new List<string>();

            using var factory = new Factory();
            using var collection = factory.GetSystemFontCollection(false);
            for (var i = 0; i < collection.FontFamilyCount; i++)
            {
                using var family = collection.GetFontFamily(i);
                var anyItalic = false;
                for (var j = 0; j < family.FontCount; j++)
                {
                    using var font = family.GetFont(j);
                    if (font.IsSymbolFont || font.Style is not (FontStyle.Italic or FontStyle.Oblique))
                    {
                        continue;
                    }

                    anyItalic = true;
                    break;
                }

                if (!anyItalic)
                {
                    continue;
                }

                var name = family.FamilyNames.GetString(0);
                fonts.Add(name);
            }

            fonts.Sort();
            return fonts;
        }

        internal static List<string> GetJpFonts()
        {
            var fonts = new List<string>();

            using var factory = new Factory();
            using var collection = factory.GetSystemFontCollection(false);
            for (var i = 0; i < collection.FontFamilyCount; i++)
            {
                using var family = collection.GetFontFamily(i);
                var probablyJp = false;
                for (var j = 0; j < family.FontCount; j++)
                {
                    using var font = family.GetFont(j);
                    if (!font.HasCharacter('気') || font.IsSymbolFont)
                    {
                        continue;
                    }

                    probablyJp = true;
                    break;
                }

                if (!probablyJp)
                {
                    continue;
                }

                var name = family.FamilyNames.GetString(0);
                fonts.Add(name);
            }

            fonts.Sort();
            return fonts;
        }

        internal static FontData? GetFont(string name, bool withItalic)
        {
            // Get all installed fonts.
            using var factory = new Factory();
            using var collection = factory.GetSystemFontCollection(false);

            // Iterate through each.
            for (var i = 0; i < collection.FontFamilyCount; i++)
            {
                // If the the name doesn't match, go to the next.
                using var family = collection.GetFontFamily(i);
                if (family.FamilyNames.GetString(0) != name)
                    continue;
                
                // If there is no normal font found, return null
                using var normal = family.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
                if (normal == null)
                    return null;

                // Get the normal font data and return null if not found.
                var normalData = GetFontData(normal);
                if (normalData == null)
                    return null;

                // If also getting italic data.
                FaceData? italicData = null;
                if (withItalic)
                {
                    // Get italic font.
                    using var italic = family.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Italic)
                                       ?? family.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Oblique);

                    // If unable to get the italic font, return null
                    if (italic == null)
                        return null;

                    // Get the font data for italic.
                    italicData = GetFontData(italic);
                    if (italicData == null)
                        return null;
                }

                // Return the data.
                return new FontData(normalData, italicData);
            }

            // return null.
            return null;
        }

        private static FaceData? GetFontData(SharpDX.DirectWrite.Font font)
        {
            using var face = new FontFace(font);
            var files = face.GetFiles();
            if (files.Length == 0)
                return null;

            var key = files[0].GetReferenceKey();
            using var stream = files[0].Loader.CreateStreamFromKey(key);

            stream.ReadFileFragment(out var start, 0, stream.GetFileSize(), out var release);

            var data = new byte[stream.GetFileSize()];
            Marshal.Copy(start, data, 0, data.Length);

            stream.ReleaseFileFragment(release);

            var metrics = font.Metrics;
            var ratio = (metrics.Ascent + metrics.Descent + metrics.LineGap) / (float)metrics.DesignUnitsPerEm;

            return new FaceData(data, ratio);
        }
    }

    internal sealed class FaceData
    {
        internal byte[] Data { get; }
        internal float Ratio { get; }

        internal FaceData(byte[] data, float ratio)
        {
            this.Data = data;
            this.Ratio = ratio;
        }
    }

    internal sealed class FontData
    {
        internal FaceData Regular { get; }
        internal FaceData? Italic { get; }

        internal FontData(FaceData regular, FaceData? italic)
        {
            this.Regular = regular;
            this.Italic = italic;
        }
    }

    internal sealed class Font
    {
        internal string Name { get; }
        internal string ResourcePath { get; }
        internal string ResourcePathItalic { get; }

        internal Font(string name, string resourcePath, string resourcePathItalic)
        {
            this.Name = name;
            this.ResourcePath = resourcePath;
            this.ResourcePathItalic = resourcePathItalic;
        }
    }
}