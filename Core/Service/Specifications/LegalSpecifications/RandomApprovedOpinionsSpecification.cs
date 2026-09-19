using Domain.Entities;
using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Specifications.LegalSpecifications {
    internal class RandomApprovedOpinionsSpecification : BaseSpecifications<LegalOpinion> {
        public RandomApprovedOpinionsSpecification(int count) : base(o => o.Status == OpinionStatus.Approved) {

            SetOrderBy(o => Guid.NewGuid());

            ApplyPagination(pageIndex: 1, pageSize: count);
        }
    }
}
