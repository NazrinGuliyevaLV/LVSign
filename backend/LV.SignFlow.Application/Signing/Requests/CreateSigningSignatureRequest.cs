using LV.SignFlow.Domain.Enums.SigningEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Requests
{
    public class CreateSigningSignatureRequest
    {
        public SignatureKind Kind { get; set; }

        public SignatureType Type { get; set; }

        public string? TypedText { get; set; }

        public string? FontKey { get; set; }

        public string? ImageDataUrl { get; set; }
    }
}
