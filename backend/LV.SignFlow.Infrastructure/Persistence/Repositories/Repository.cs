using LV.SignFlow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T:class
    {
        public readonly AppDbContext appDbContext;
        public readonly DbSet<T> dbSet;

        public Repository(AppDbContext dbContext)
        {
            appDbContext = dbContext;
            dbSet = dbContext.Set<T>();
        }
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
             await dbSet.AddAsync(entity,cancellationToken);
        }

        public void Delete(T entity)
        {
             dbSet.Remove(entity);
        }

        public async Task<IReadOnlyList<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

        }

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await dbSet.AsNoTracking().ToListAsync(cancellationToken);

        }

        public async Task<T?> GetByIdAsync(Guid? id, CancellationToken cancellationToken = default)
        {
            return await dbSet.FindAsync(new object?[] { id }, cancellationToken);
        }

        public void Update(T entity)
        {
            dbSet.Update(entity);
        }
    }
}
