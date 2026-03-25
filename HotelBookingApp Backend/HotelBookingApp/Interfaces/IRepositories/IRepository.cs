using System.Linq.Expressions;

namespace HotelBookingApp.Interfaces.IRepositories
{
    /// <summary>
    /// Generic repository interface — provides full CRUD + Include support.
    /// TKey = primary key type, TEntity = entity class.
    /// </summary>
    public interface IRepository<TKey, TEntity> where TEntity : class
    {
        // ── Read ──────────────────────────────────────────────────────────
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TKey id);

        /// <summary>Returns all entities with the given navigation properties eagerly loaded.</summary>
        Task<IEnumerable<TEntity>> GetAllIncludingAsync(params Expression<Func<TEntity, object>>[] includes);

        /// <summary>Returns the first entity matching the predicate, or null.</summary>
        Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>Returns all entities matching the predicate.</summary>
        Task<IEnumerable<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate);

        // ── Write ─────────────────────────────────────────────────────────
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity?> UpdateAsync(TKey id, TEntity entity);
        Task<TEntity?> DeleteAsync(TKey id);

        // ── Existence ─────────────────────────────────────────────────────
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        // ── Count ─────────────────────────────────────────────────────────
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    }
}
