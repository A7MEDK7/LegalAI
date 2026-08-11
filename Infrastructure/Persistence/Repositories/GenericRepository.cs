using Domain.Contract;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Repositories {
    public class GenericRepository<TEntity>(AppDbContext _dbContext) : IGenericRepository<TEntity> where TEntity : BaseEntity {

        // ── Private shortcut to the typed DbSet ───────────────────────
        private DbSet<TEntity> _dbSet => _dbContext.Set<TEntity>();

        // ─────────────────────────────────────────────────────────────
        // Single Entity Queries

        // Uses FindAsync so EF checks the in-memory identity cache first
        // before hitting the database — avoids a redundant round-trip when
        // the entity was already loaded earlier in the same request.
        public async Task<TEntity?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        // AsNoTracking — read-only query, no change tracking overhead.
        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AsNoTracking()
                           .FirstOrDefaultAsync(predicate);

        public async Task<TEntity?> GetAsync(ISpecifications<TEntity> specifications) // Get By Id and With Specifications
            => await SpecificationsEvaluator.QetQuery(_dbContext.Set<TEntity>(), specifications)
                                            .FirstOrDefaultAsync();

        // ─────────────────────────────────────────────────────────────
        // Collection Queries

        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
            => await _dbSet.AsNoTracking()
                           .ToListAsync();

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AsNoTracking()
                           .Where(predicate)
                           .ToListAsync();

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications)
            => await SpecificationsEvaluator.QetQuery(_dbContext.Set<TEntity>(), specifications)
                                            .ToListAsync();

        // ─────────────────────────────────────────────────────────────
        // Aggregation Operations

        public async Task<int> CountAsync()
            => await _dbSet.CountAsync();

        public async Task<int> CountAsync(ISpecifications<TEntity> specifications)
            => await SpecificationsEvaluator.QetQuery(_dbContext.Set<TEntity>(), specifications).CountAsync();

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        // ─────────────────────────────────────────────────────────────
        // Write Operations

        // AddAsync stages the entity in the change tracker.
        // Nothing is written to the DB until UnitOfWork.SaveChangesAsync() is called.
        public async Task AddAsync(TEntity entity)
            => await _dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
            => await _dbSet.AddRangeAsync(entities);

        // Stamp UpdatedAt then mark the whole entity as Modified.
        // EF Core will generate an UPDATE covering every column.
        public void Update(TEntity entity) {
            entity.UpdatedAt = DateTime.Now;
            _dbSet.Update(entity);
        }

        // Remove stages the entity for DELETE in the change tracker.
        public void Delete(TEntity entity)
            => _dbSet.Remove(entity);

        public void DeleteRange(IEnumerable<TEntity> entities)
            => _dbSet.RemoveRange(entities);
    }
}
