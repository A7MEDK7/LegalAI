using Domain.Contract;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Specifications {
    internal abstract class BaseSpecifications<TEntity> : ISpecifications<TEntity> where TEntity : BaseEntity {

        #region Criteria
        protected BaseSpecifications(Expression<Func<TEntity, bool>>? criteria) {
            Criteria = criteria;
        }
        public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
        #endregion

        #region Includes
        public List<Expression<Func<TEntity, object>>> IncludeExpression { get; } = new List<Expression<Func<TEntity, object>>>();
        protected void AddIncludes(Expression<Func<TEntity, object>> includeExpressions) => IncludeExpression.Add(includeExpressions);

        public List<string> IncludesStrings { get; } = new();
        public void AddInclude(string include) => IncludesStrings.Add(include);
        #endregion

        #region Order By Specifications
        public Expression<Func<TEntity, object>>? OrderBy { get; private set; }

        public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }

        protected void SetOrderBy(Expression<Func<TEntity, object>> OrderByExpression)
            => OrderBy = OrderByExpression;

        protected void SetOrderByDescending(Expression<Func<TEntity, object>> OrderByDescExpression)
            => OrderByDescending = OrderByDescExpression;
        #endregion

        #region Paginataiton
        public int Skip { get; private set; }
        public int Take { get; private set; }
        public bool IsPaginated { get; private set; }


        protected void ApplyPagination(int pageIndex, int pageSize) {
            IsPaginated = true;
            Take = pageSize;
            Skip = (pageIndex - 1) * pageSize;
        }
        #endregion
    }
}
