using System.Linq.Expressions;
using EcommercePlatform.Domain.Common;

namespace EcommercePlatform.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> AsQueryable(bool asNoTracking = true);

    Task<T?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}
