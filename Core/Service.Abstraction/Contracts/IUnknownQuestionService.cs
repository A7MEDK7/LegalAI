using Shared.DTOs.LegalOpinionModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface IUnknownQuestionService {
        Task AddAsync(string question, int askedById);
        Task<IReadOnlyList<UnknownQuestionResultDTO>> GetAllAsync();
        Task DeleteAsync(int id);
        Task CheckExistQuestion(string question);
    }
}
