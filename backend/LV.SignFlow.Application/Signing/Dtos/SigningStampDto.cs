using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Dtos
{
    public class SigningStampDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }
}
