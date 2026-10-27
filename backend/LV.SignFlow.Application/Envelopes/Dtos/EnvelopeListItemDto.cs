using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeListItemDto
    {
        public Guid Id { get; set; }

        public string DocumentId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public Guid SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? DueAt { get; set; }

        public int? Pages { get; set; }

        public IReadOnlyList<EnvelopeSignerDto> Signers { get; set; }
            = Array.Empty<EnvelopeSignerDto>();
    }
}
