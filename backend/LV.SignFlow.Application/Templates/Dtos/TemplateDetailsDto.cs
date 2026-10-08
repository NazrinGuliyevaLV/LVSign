using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateDetailsDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TemplateStatus Status { get; set; }

        public Guid OwnerUserId { get; set; }

        public int LatestVersionNumber { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid LatestVersionId { get; set; }

        public bool LatestVersionIsPublished { get; set; }

        public bool HasDraftVersion { get; set; }

        public int? LatestPublishedVersionNumber { get; set; }

        public bool IsOwner { get; set; }

        public TemplateAccessLevel EffectiveAccessLevel { get; set; }
    }
}
