using LV.SignFlow.Domain.Entities.Organizations;
using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class Envelope
    {
        public Guid Id { get; set; }
        public string Subject { get; set; }
        public string? Message { get; set; }
        public EnvelopeType Type { get; set; } = EnvelopeType.Standart;
        public EnvelopeStatus Status { get; set; } = EnvelopeStatus.Draft;
        public Guid CreatedByUserId { get; set; }
        public int? CurrentRoutingOrder { get; set; }

        //envelope template-den create olunubsa source bilmek ucun
        public Guid? SourceTemplateVersionId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? VoidedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public Guid OrganizationId { get; set; }

        public int? ReminderIntervalDays { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public Organization Organization { get; set; } = null!;
        public TemplateVersion? SourceTemplateVersion { get; set; }
        public ICollection<Documents> Documents { get; set; }= new List<Documents>();
        public ICollection<Events> Events { get; set; }= new List<Events>();
        public ICollection<StatusHistory> StatusHistory { get; set; }= new List<StatusHistory>();
        public ICollection<Fields> Fields { get; set; }=new List<Fields>();
        public ICollection<Recipients> Recipients { get; set; }=new List<Recipients>();
        public ICollection<EnvelopeRoutingRuleSet> EnvelopeRoutingRuleSets { get; set; }
    = new List<EnvelopeRoutingRuleSet>();



    }
}
