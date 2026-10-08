using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Templates;
using LV.SignFlow.Domain.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Repositories
{
    public class TemplateRepository : Repository<Template>,ITemplateRepository
    {
        private readonly AppDbContext _context;

        public TemplateRepository(AppDbContext context)
            : base(context)
        {
            _context = context;
        }
        private  IQueryable<Template> GetTemplateQuery()
        {
            return  _context.Templates.AsNoTracking()
                 .Include(x => x.Versions)
                 .Include(x => x.Shares)
                 .Where(x => !x.IsDeleted);
        }

        public async Task<IReadOnlyList<Template>>
            GetAllForOrganizationAsync(
                Guid organizationId,
                CancellationToken cancellationToken = default)
        {
            return await _context.Templates
                .AsNoTracking()
                .Include(x => x.Versions)
                .Where(x =>
                    x.OrganizationId == organizationId &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Template?> GetByIdForOrganizationAsync(
            Guid id,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await GetTemplateQuery()
                .Include(x => x.Versions)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == id &&
                        x.OrganizationId == organizationId &&
                        !x.IsDeleted,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Template>> GetOwnedByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await GetTemplateQuery()
                .Where(x =>
                        x.OwnerUserId == userId &&
                        x.OrganizationId == organizationId)
                .OrderByDescending(x=>x.UpdatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Template>> GetSharedByUserAsync(Guid organizationId, Guid userId, Guid? departmentId, CancellationToken cancellationToken = default)
        {
            return await GetTemplateQuery()
                .Where(x=>x.OrganizationId==organizationId && x.OwnerUserId!=userId &&
                x.Shares.Any(S=>S.SharedWithUserId==userId || (
                 S.SharedWithDepartmentId ==
                    departmentId)))
        .OrderByDescending(x => x.UpdatedAt)
        .ToListAsync(cancellationToken);

        }
    }
}
