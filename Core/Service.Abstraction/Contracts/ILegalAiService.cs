using Microsoft.AspNetCore.Http;
using Shared.DTOs.LegalAiModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface ILegalAiService {
        Task<AskResultDTO> AskAsync(string query);
        Task<bool> AddNewOpinionAsync(Stream fileStream, string fileName);
    }
}
