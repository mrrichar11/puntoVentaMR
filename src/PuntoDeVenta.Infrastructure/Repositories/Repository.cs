using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Infrastructure.Data;

namespace PuntoDeVenta.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.Activo, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Activo).ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Activo).Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Activo).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.FechaModificacion = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        // Borrado lógico para preservar integridad referencial y auditoría
        entity.Activo = false;
        entity.FechaModificacion = DateTime.UtcNow;
        _dbSet.Update(entity);
    }
}
