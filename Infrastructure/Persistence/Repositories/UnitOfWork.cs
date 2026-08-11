using Domain.Contract;
using Domain.Entities;
using Persistence.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class UnitOfWork(AppDbContext _dbContext) : IUnitOfWork {

        private readonly ConcurrentDictionary<String, object> _repositories = new();

        public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity
            => (IGenericRepository<TEntity>)_repositories.GetOrAdd(typeof(TEntity).Name, (_) => new GenericRepository<TEntity>(_dbContext));
    }
}
