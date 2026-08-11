using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contract
{
    public interface ISpecifications<TEntity, TKey> where TEntity : BaseEntity
    {
        public Expression<Func<TEntity, bool>>? Criteria { get; }  // P => P.Id

        // Include --> Include(P => P.ProductBrand) [May Be More Than One Include]
        public List<Expression<Func<TEntity, object>>> IncludeExpression { get; } // P => P.ProductBrand

        public Expression<Func<TEntity, object>>? OrderBy { get; }
        public Expression<Func<TEntity, object>>? OrderByDescending { get; }

        public int Skip { get; }
        public int Take { get; }
        public bool IsPaginated { get; }
    }
}
