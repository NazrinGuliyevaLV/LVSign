using LV.SignFlow.Domain.Entities.Organizations;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Templates
{
    public class Template
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid OwnerUserId { get; set; }
        public TemplateStatus Status { get; set; } = TemplateStatus.Draft;
        public TemplateCategory? Category { get; set; }
        public bool IsDeleted { get; set; }
        public  DateTimeOffset? DeletedAt { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid OrganizationId { get; set; }

        public Organization? Organization { get; set; }
        public User OwnerUser { get; set; } = null!;

        public ICollection<TemplateVersion> Versions { get; set; } = new List<TemplateVersion>();
        public ICollection<TemplateShare> Shares { get; set; }
    = new List<TemplateShare>();

    }
}
