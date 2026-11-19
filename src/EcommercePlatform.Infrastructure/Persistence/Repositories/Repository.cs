using System.Linq.Expressions;
using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Infrastructure.Persistence.Repositories;

public class Repository<T>(AppDbContext dbContext) : IRepository<T>
    where T : BaseEntity
{
    private readonly DbSet<T> _dbSet = dbContext.Set<T>();

    public IQueryable<T> AsQueryable(bool asNoTracking = true)
        => asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

    public Task<T?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default)
        => AsQueryable(asNoTracking).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
        => AsQueryable(asNoTracking).FirstOrDefaultAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => _dbSet.AnyAsync(predicate, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Remove(T entity)
        => _dbSet.Remove(entity);
}
