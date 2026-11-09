using LV.SignFlow.Domain.Entities.Audit;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Notifications;
using LV.SignFlow.Domain.Entities.Organizations;
using LV.SignFlow.Domain.Entities.Signing;
using LV.SignFlow.Domain.Entities.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Users
{
    public class User
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid? DepartmentId { get; set; }

        public string? ExternalIdentityId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset? LastLoginAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }


        public ICollection<UserRole> UserRoles { get; set; }
            = new List<UserRole>();

        public ICollection<UserPermissionProfile> PermissionProfiles { get; set; }
            = new List<UserPermissionProfile>();
        public ICollection<Signature> Signatures { get; set; }
            = new List<Signature>();
        public ICollection<Stamps> Stamps { get; set; }
           = new List<Stamps>();
        public ICollection<Template> Templates { get; set; }
            = new List<Template>();
        public ICollection<Envelope> CreatedEnvelopes { get; set; }
           = new List<Envelope>();
        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();
        public Organization Organization { get; set; } = null!;

        public Department? Department { get; set; }

        public ICollection<Notification> Notifications { get; set; }
    = new List<Notification>();

        public NotificationPreference? NotificationPreference { get; set; }
    }
}
