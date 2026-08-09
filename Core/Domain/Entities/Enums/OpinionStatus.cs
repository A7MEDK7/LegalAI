using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Enums {
    public enum OpinionStatus {
        Submitted = 1,
        UnderReview = 2,
        Approved = 3,
        Rejected = 4,
        RevisionRequired = 5
    }
}

