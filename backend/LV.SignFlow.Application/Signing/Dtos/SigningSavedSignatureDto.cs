using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Dtos
{
    public class SigningSavedSignatureDto
    {
        public Guid Id { get; set; }

        public string? Label { get; set; }

        public string? ImageUrl { get; set; }

        public string? TypedText { get; set; }

        public string? FontKey { get; set; }
    }
}
