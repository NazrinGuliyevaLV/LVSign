using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.SigningEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Signing
{
    public class Signature
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public SignatureType Type { get; set; }

        public string BlobPath { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public SignatureKind Kind { get; set; }= SignatureKind.Signature;
        public User User { get; set; } = null!;
    }
}
