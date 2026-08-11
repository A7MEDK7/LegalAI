using Domain.Contract;
using Domain.Entities;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity
        => (IGenericRepository<TEntity, TKey>)_repositories.GetOrAdd(typeof(TEntity).Name,
                                                   (_) => new GenericRepository<TEntity, TKey>(_dbContext));

        public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();


    }
}
