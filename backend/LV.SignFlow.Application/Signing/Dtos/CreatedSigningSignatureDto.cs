using LV.SignFlow.Domain.Enums.SigningEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Dtos
{
    public class CreatedSigningSignatureDto
    {
        public Guid Id { get; set; }

        public SignatureKind Kind { get; set; }

        public SignatureType Type { get; set; }

        public string ContentUrl { get; set; }
            = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
