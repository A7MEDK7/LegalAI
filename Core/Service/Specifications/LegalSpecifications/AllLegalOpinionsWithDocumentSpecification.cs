using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Specifications.LegalSpecifications {
    internal class AllLegalOpinionsWithDocumentSpecification : BaseSpecifications<LegalOpinion> {
        public AllLegalOpinionsWithDocumentSpecification() : base(null) {
            AddIncludes(o => o.CreatedBy);
            AddIncludes(o => o.LegalDocument!);
            SetOrderByDescending(o => o.CreatedAt);
        }
    }

    internal class LegalOpinionWithDetailsSpecification : BaseSpecifications<LegalOpinion> {
        public LegalOpinionWithDetailsSpecification(int opinionId)
            : base(o => o.Id == opinionId) {
            AddIncludes(o => o.CreatedBy);
            AddIncludes(o => o.ReviewedBy!);
            AddIncludes(o => o.LegalDocument!);
            AddIncludes(o => o.Reviews!);
            AddInclude("Reviews.Reviewer");
        }
    }
}
