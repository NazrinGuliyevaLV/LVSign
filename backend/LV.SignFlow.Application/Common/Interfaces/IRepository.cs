using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LV.SignFlow.Application.Common.Interfaces
{
    public interface IRepository<T> where T: class
    {
        Task<T?> GetByIdAsync(Guid? id,CancellationToken cancellationToken=default);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T,bool>> predicate,CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Delete(T entity);
        void Update(T entity);
    }
}
