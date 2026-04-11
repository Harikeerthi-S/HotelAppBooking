using System.Linq.Expressions;
using HotelBookingApp.Context;
using HotelBookingApp.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Repositories
{
    /// <summary>
    /// Generic EF Core repository implementation.
    /// Provides CRUD, Include, predicate-based Find, Exists, and Count operations.
    /// </summary>
    public class Repository<TKey, TEntity> : IRepository<TKey, TEntity>
        where TEntity : class
    {
        private readonly HotelBookingContext _context;
        private readonly DbSet<TEntity>      _dbSet;

        public Repository(HotelBookingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet   = _context.Set<TEntity>();
        }

        // ── GET ALL ───────────────────────────────────────────────────────
        public async Task<IEnumerable<TEntity>> GetAllAsync()
            => await _dbSet.AsNoTracking().ToListAsync();

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<TEntity?> GetByIdAsync(TKey id)
            => await _dbSet.FindAsync(id);

        // ── GET ALL WITH INCLUDES ─────────────────────────────────────────
        public async Task<IEnumerable<TEntity>> GetAllIncludingAsync(
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.ToListAsync();
        }

        // ── FIND (PREDICATE) ──────────────────────────────────────────────
        public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);

        public async Task<IEnumerable<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

        // ── ADD ───────────────────────────────────────────────────────────
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public async Task<TEntity?> UpdateAsync(TKey id, TEntity entity)
        {
            var existing = await _dbSet.FindAsync(id);
            if (existing is null) return null;

            _context.Entry(existing).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return existing;
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<TEntity?> DeleteAsync(TKey id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity is null) return null;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // ── EXISTS ────────────────────────────────────────────────────────
        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        // ── COUNT ─────────────────────────────────────────────────────────
        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
            => predicate is null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
    }
}
