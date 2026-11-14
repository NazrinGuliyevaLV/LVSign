using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Templates
{
    public class TemplateVersion
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public int VersionNumber { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public int? ExpirationDays { get; set; }

        public int? ReminderIntervalDays { get; set; }

        public Template Template { get; set; } = null!;
        public ICollection<TemplateDocument> Documents { get; set; } = new List<TemplateDocument>();
        public ICollection<TemplateRecipientRole> RecipientsRoles { get; set; } = new List<TemplateRecipientRole>();

        public ICollection<Envelope>? SourceEnvelopes { get; set; }
        public ICollection<TemplateRoutingRuleSet> RoutingRuleSets { get; set; }
    = new List<TemplateRoutingRuleSet>();
    }
}
