using LV.SignFlow.Domain.Entities.Audit;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Organizations;
using LV.SignFlow.Domain.Entities.Signing;
using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { 
        }
        public DbSet<Organization> Organizations=>Set<Organization>();
        public DbSet<Department> Departments=>Set<Department>();
        public DbSet<EnvelopeFieldValue> EnvelopeFieldValues=> Set<EnvelopeFieldValue>();
        public DbSet<Template> Templates=>Set<Template>();
        public DbSet<TemplateDocument> TemplateDocuments=>Set<TemplateDocument>();
        public DbSet<TemplateField> TemplateFields=>Set<TemplateField>();
        public DbSet<TemplateRecipientRole> TemplateRecipientRoles=>Set<TemplateRecipientRole>();
        public DbSet<TemplateRoutingCondition> TemplateRoutingConditions => Set<TemplateRoutingCondition>();
        public DbSet<TemplateRoutingRule> TemplateRoutingRules => Set<TemplateRoutingRule>();
        public DbSet<TemplateRoutingRuleSet> TemplateRoutingRuleSets => Set<TemplateRoutingRuleSet>();
        public DbSet<TemplateShare> TemplateShares => Set<TemplateShare>();
        public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();


        public DbSet<User> Users => Set<User>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<PermissionProfile> PermissionProfiles => Set<PermissionProfile>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<PermissionProfilePermission> PermissionProfilePermissions => Set<PermissionProfilePermission>();
        public DbSet<UserPermissionProfile> UserPermissionProfiles => Set<UserPermissionProfile>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<Envelope> Envelopes => Set<Envelope>();
        public DbSet<Documents> EnvelopeDocuments => Set<Documents>();

        public DbSet<Recipients> EnvelopeRecipients => Set<Recipients>();

        public DbSet<Fields> EnvelopeFields  => Set<Fields>();

        public DbSet<Events> EnvelopeEvents  => Set<Events>();

        public DbSet<StatusHistory> EnvelopeStatusHistories => Set<StatusHistory>();

        public DbSet<EnvelopeRoutingRuleSet> EnvelopeRoutingRuleSets => Set<EnvelopeRoutingRuleSet>();

        public DbSet<EnvelopeRoutingRule> EnvelopeRoutingRules => Set<EnvelopeRoutingRule>();

        public DbSet<EnvelopeRoutingCondition> EnvelopeRoutingConditions => Set<EnvelopeRoutingCondition>();

        public DbSet<Stamps> Stamps => Set<Stamps>();
        public DbSet<Signature> Signatures => Set<Signature>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<EnvelopeAppliedSignature> EnvelopeAppliedSignatures => Set<EnvelopeAppliedSignature>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
