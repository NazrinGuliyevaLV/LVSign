using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Pdf.Fonts
{
    public static class LvSignFontMapper
    {
        public static string GetSignatureFamily(
            string? fontKey)
        {
            return fontKey?
                .Trim()
                .ToLowerInvariant()
                switch
            {
                "great-vibes" =>
                    LvSignFontFamilies.GreatVibes,

                "mrs-saint-delafield" =>
                    LvSignFontFamilies.MrsSaintDelafield,

                "caveat" =>
                    LvSignFontFamilies.Caveat,

                _ =>
                    LvSignFontFamilies.GreatVibes
            };
        }
    }
}
