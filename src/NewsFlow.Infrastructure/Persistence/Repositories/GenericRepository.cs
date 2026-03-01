using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NewsFlow.Domain.Common;
using NewsFlow.Domain.Interfaces;

namespace NewsFlow.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly NewsFlowDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(NewsFlowDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await DbSet.Where(predicate).ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        return entity;
    }

    public void Update(T entity) => DbSet.Update(entity);

    public void Delete(T entity) => DbSet.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await DbSet.AnyAsync(e => e.Id == id, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? await DbSet.CountAsync(ct) : await DbSet.CountAsync(predicate, ct);
}
