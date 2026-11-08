using LV.SignFlow.Infrastructure.Pdf.Fonts;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Pdf
{
    public static class PdfFontBootstrapper
    {
        private static bool _initialized;
        private static readonly object Sync =new();

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            lock (Sync)
            {
                if (_initialized)
                {
                    return;
                }

                GlobalFontSettings.FontResolver =
                    new LvSignFontResolver();

              
                ValidateFonts();

                _initialized =
                    true;
            }
        }

        private static void ValidateFonts()
        {
            ValidateFont(
                "Noto Sans Regular",
                LvSignFontFamilies.Sans,
                XFontStyleEx.Regular);

            ValidateFont(
                "Noto Sans Bold",
                LvSignFontFamilies.Sans,
                XFontStyleEx.Bold);

            ValidateFont(
                "Great Vibes",
                LvSignFontFamilies.GreatVibes,
                XFontStyleEx.Regular);

            ValidateFont(
                "Mrs Saint Delafield",
                LvSignFontFamilies.MrsSaintDelafield,
                XFontStyleEx.Regular);

            ValidateFont(
                "CaveatBrush",
                LvSignFontFamilies.Caveat,
                XFontStyleEx.Regular);
        }

        private static void ValidateFont(
            string displayName,
            string familyName,
            XFontStyleEx style)
        {
            try
            {
                _ = new XFont(
                    familyName,
                    12,
                    style);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"PDF font validation failed for '{displayName}' "
                    + $"(family: '{familyName}'). "
                    + "Check that the embedded file is a valid static TTF font.",
                    ex);
            }
        }
    }
}
