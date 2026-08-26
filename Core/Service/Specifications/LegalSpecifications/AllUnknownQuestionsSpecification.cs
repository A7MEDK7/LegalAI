using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Specifications.LegalSpecifications {
    internal class AllUnknownQuestionsSpecification : BaseSpecifications<UnknownQuestion> {
        public AllUnknownQuestionsSpecification() : base(null) {
            AddIncludes(q => q.AskedBy);
            SetOrderByDescending(q => q.CreatedAt);
        }
    }
}
