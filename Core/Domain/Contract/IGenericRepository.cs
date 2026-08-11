using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contract
{
    public interface IGenericRepository<TEntity> where TEntity : BaseEntity {

        // ─────────────────────────────────────────────
        // Single Entity Queries

        /// <summary>
        /// Retrieves an entity by its primary key.
        /// Returns null if no entity is found.
        /// </summary>
        Task<TEntity?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves the first entity that matches the given condition.
        /// Returns null if no match is found.
        /// </summary>
        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Retrieves the first entity with the specifications.
        /// Returns null if no match is found.
        /// </summary>
        Task<TEntity?> GetAsync(ISpecifications<TEntity> specifications);


        // ─────────────────────────────────────────────
        // Collection Queries

        /// <summary>
        /// Retrieves all entities from the database.
        /// </summary>
        Task<IReadOnlyList<TEntity>> GetAllAsync();

        /// <summary>
        /// Retrieves all entities that match the given condition.
        /// </summary>
        Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Retrieves all entities with the specifications.
        /// </summary>
        Task<IReadOnlyList<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications);

        // ─────────────────────────────────────────────
        // Aggregation Operations

        /// <summary>
        /// Returns the total number of entities in the database.
        /// </summary>
        Task<int> CountAsync();

        /// <summary>
        /// Returns the number of entities that match the given condition.
        /// </summary>
        Task<int> CountAsync(ISpecifications<TEntity> specifications);

        /// <summary>
        /// Checks whether any entity exists that matches the given condition.
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

        // ─────────────────────────────────────────────
        // Write Operations (Tracked by EF Core)

        /// <summary>
        /// Adds a new entity to the context.
        /// Changes are not saved to the database until SaveChanges is called.
        /// </summary>
        Task AddAsync(TEntity entity);

        /// <summary>
        /// Adds a collection of entities to the context.
        /// Improves performance when inserting multiple records.
        /// </summary>
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Marks an existing entity as modified.
        /// The changes will be saved on SaveChanges.
        /// </summary>
        void Update(TEntity entity);

        /// <summary>
        /// Marks an entity for deletion.
        /// The entity will be removed on SaveChanges.
        /// </summary>
        void Delete(TEntity entity);

        /// <summary>
        /// Marks multiple entities for deletion.
        /// Useful for bulk delete operations.
        /// </summary>
        void DeleteRange(IEnumerable<TEntity> entities);
    }
}
