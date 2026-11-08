using PdfSharp.Fonts;

namespace LV.SignFlow.Infrastructure.Pdf.Fonts
{
    public sealed class LvSignFontResolver : IFontResolver
    {
        private const string SansRegular =
            "lv-sign-sans-regular";

        private const string SansBold =
            "lv-sign-sans-bold";

        private const string GreatVibes =
            "lv-sign-great-vibes";

        private const string MrsSaintDelafield =
            "lv-sign-mrs-saint-delafield";

        private const string Caveat =
            "lv-sign-caveat";

        /*
         * EmbeddedResource logical name prefix.
         *
         * Font files:
         *
         * LV.SignFlow.Infrastructure/
         *   Pdf/
         *     Fonts/
         *       NotoSans-Regular.ttf
         *       NotoSans-Bold.ttf
         *       GreatVibes-Regular.ttf
         *       MrsSaintDelafield-Regular.ttf
         *       Caveat-Regular.ttf
         */
        private const string ResourcePrefix =
            "LV.SignFlow.Infrastructure.Pdf.Fonts.";

        public FontResolverInfo? ResolveTypeface(
            string familyName,
            bool bold,
            bool italic)
        {
            /*
             * Normal application/PDF text.
             */
            if (string.Equals(
                    familyName,
                    LvSignFontFamilies.Sans,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (bold)
                {
                    return new FontResolverInfo(
                        SansBold,
                        false,
                        italic);
                }

                return new FontResolverInfo(
                    SansRegular,
                    false,
                    italic);
            }

            /*
             * Typed signature:
             * Great Vibes
             */
            if (string.Equals(
                    familyName,
                    LvSignFontFamilies.GreatVibes,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo(
                    GreatVibes);
            }

            /*
             * Typed signature:
             * Mrs Saint Delafield
             */
            if (string.Equals(
                    familyName,
                    LvSignFontFamilies.MrsSaintDelafield,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo(
                    MrsSaintDelafield);
            }

            /*
             * Typed signature:
             * Caveat
             */
            if (string.Equals(
                    familyName,
                    LvSignFontFamilies.Caveat,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo(
                    Caveat);
            }

            /*
             * OS fontlarına fallback etmirik.
             *
             * Azure Windows/Linux-da nəticənin
             * eyni olması üçün yalnız bizim
             * embedded fontlarımız istifadə olunur.
             */
            return null;
        }

        public byte[]? GetFont(
            string faceName)
        {
            return faceName switch
            {
                SansRegular =>
                    LoadFont(
                        "NotoSans-Regular.ttf"),

                SansBold =>
                    LoadFont(
                        "NotoSans-Bold.ttf"),

                GreatVibes =>
                    LoadFont(
                        "GreatVibes-Regular.ttf"),

                MrsSaintDelafield =>
                    LoadFont(
                        "MrsSaintDelafield-Regular.ttf"),

                Caveat =>
                    LoadFont(
                        "CaveatBrush-Regular.ttf"),

                _ => throw new InvalidOperationException(
     $"Unknown PDF font face: '{faceName}'.")
            };
        }

        private static byte[] LoadFont(
            string fileName)
        {
            var assembly =
                typeof(LvSignFontResolver)
                    .Assembly;

            var resourceName =
                ResourcePrefix +
                fileName;

            using var stream =
                assembly.GetManifestResourceStream(
                    resourceName);

            if (stream is null)
            {
                /*
                 * Font embed olunmayıbsa startup zamanı
                 * hansı resource-ların olduğunu görmək
                 * üçün faydalı error veririk.
                 */
                var availableResources =
                    string.Join(
                        Environment.NewLine,
                        assembly
                            .GetManifestResourceNames()
                            .Where(x =>
                                x.Contains(
                                    ".Pdf.Fonts.",
                                    StringComparison.OrdinalIgnoreCase)));

                throw new InvalidOperationException(
                    $"Embedded font '{resourceName}' was not found."
                    + Environment.NewLine
                    + "Available font resources:"
                    + Environment.NewLine
                    + availableResources);
            }

            using var memory =
                new MemoryStream();

            stream.CopyTo(
                memory);

            return memory.ToArray();
        }
    }
}