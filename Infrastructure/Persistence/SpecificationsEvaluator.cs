using Domain.Contract;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence {
    internal static class SpecificationsEvaluator {
        public static IQueryable<TEntity> QetQuery<TEntity>(
            IQueryable<TEntity> inputQuery,
            ISpecifications<TEntity> specifications) where TEntity : BaseEntity {

            var query = inputQuery;

            if (specifications.Criteria is not null)
                query = query.Where(specifications.Criteria);
            // query = _dbcontext.set<Entity>().Where(P => P.Id == 5)

            if (specifications.IncludeExpression?.Count() > 0) {
                foreach (var expression in specifications.IncludeExpression)
                    query = query.Include(expression);
                // query = _dbcontext.set<Entity>()
                // .Where(P => P.Id == 5)
                // .Include(P => P.Property).Include(P => P.Property)

            }

            if (specifications.IncludesStrings?.Count > 0) {
                query = specifications.IncludesStrings
                    .Aggregate(query, (current, include) => current.Include(include));
            }

            // Check For OrderBy
            if (specifications.OrderBy is not null)
                query = query.OrderBy(specifications.OrderBy);
            else if (specifications.OrderByDescending is not null)
                query = query.OrderByDescending(specifications.OrderByDescending);

            // Check The Pagination
            if (specifications.IsPaginated)
                query = query.Skip(specifications.Skip).Take(specifications.Take);

            return query;
        }

    }
}
