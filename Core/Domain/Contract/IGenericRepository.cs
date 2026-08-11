using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contract
{
    public interface IGenericRepository<TEntity , TKey> where TEntity : BaseEntity
    {
        Task<IEnumerable<TEntity>> GetAllAsync(bool asNoTracking = false);  // Get All
        Task<TEntity?> GetAsync(TKey id);  // Get By Id

        #region Specifications
        Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity, TKey> specifications);  // Get All
        Task<TEntity?> GetAsync(ISpecifications<TEntity, TKey> specifications);  // Get By Id
        Task<int> CountAsync(ISpecifications<TEntity, TKey> specifications); // Get The Count Of Products Result
        #endregion
        Task AddAsync(TEntity entity);  // Create
        void Update(TEntity entity); // Update
        void Delete(TEntity entity);  // Delete
    }
}
