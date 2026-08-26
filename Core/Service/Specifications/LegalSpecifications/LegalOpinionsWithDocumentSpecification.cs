using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Specifications.LegalSpecifications {
    internal class LegalOpinionsWithDocumentSpecification : BaseSpecifications<LegalOpinion> {
        public LegalOpinionsWithDocumentSpecification(int userId) : base(U => U.CreatedById == userId) {
            AddIncludes(o => o.CreatedBy);
            AddIncludes(o => o.LegalDocument!);
            SetOrderByDescending(o => o.CreatedAt);
        }
    }
}
