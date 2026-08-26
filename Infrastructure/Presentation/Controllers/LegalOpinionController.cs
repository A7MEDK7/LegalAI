using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Abstraction.Contracts;
using Shared.DTOs.LegalAiModule;
using Shared.DTOs.LegalOpinionModule;

namespace Presentation.Controllers {

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LegalOpinionController(IServiceManger serviceManger) : ControllerBase {

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ═══════════ ASK FLOW (Lawyer, Legal Advisor) ═══════════

        [Authorize(Roles = "Lawyer,Legal Advisor")]
        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] AskQueryDTO dto)
            => Ok(await serviceManger.LegalOpinionService.AskAsync(dto.Query, CurrentUserId));

        [Authorize(Roles = "Lawyer,Legal Advisor")]
        [HttpGet("Document-By-Source")]
        public async Task<IActionResult> GetDocumentBySource([FromQuery] string fileName) {
            var (stream, resolvedFileName) = await serviceManger.LegalOpinionService.GetDocumentBySourceAsync(fileName);
            return File(stream, "application/octet-stream", resolvedFileName);
        }

        // ═══════════ ADD NEW OPINION FLOW (Legal Advisor) ═══════════

        [Authorize(Roles = "Legal Advisor")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateLegalOpinionDTO dto)
            => Ok(await serviceManger.LegalOpinionService.CreateAsync(dto, CurrentUserId));

        [Authorize(Roles = "Legal Advisor")]
        [HttpGet("Legal-Advisor-Opinions")]
        public async Task<IActionResult> GetAdvisorOpinions() 
            => Ok(await serviceManger.LegalOpinionService.GetLegalOpinionsForUser(CurrentUserId));

        [Authorize(Roles = "Legal Advisor")]
        [HttpPut("{id}/Resubmit")]
        public async Task<IActionResult> Resubmit(int id, [FromForm] UpdateLegalOpinionDTO dto)
            => Ok(await serviceManger.LegalOpinionService.ResubmitAsync(id, dto, CurrentUserId));

        [Authorize(Roles = "Legal Advisor")]
        [HttpGet("Unknown-Questions")]
        public async Task<IActionResult> GetUnknownQuestions()
            => Ok(await serviceManger.UnknownQuestionService.GetAllAsync());

        [Authorize(Roles = "Legal Advisor")]
        [HttpDelete("Unknown-Questions/{id}")]
        public async Task<IActionResult> DeleteUnknownQuestion(int id) {
            await serviceManger.UnknownQuestionService.DeleteAsync(id);
            return Ok();
        }

        // ═══════════ REVIEW FLOW (Legal Manager) ═══════════

        [Authorize(Roles = "Legal Manager")]
        [HttpGet]
        public async Task<IActionResult> GetAllForReview()
            => Ok(await serviceManger.LegalOpinionService.GetAllForReviewAsync());

        [Authorize(Roles = "Legal Manager,Legal Advisor")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) {
            var isManager = User.IsInRole("Legal Manager");
            return Ok(await serviceManger.LegalOpinionService.GetByIdAsync(id, CurrentUserId, isManager));
        }

        [Authorize(Roles = "Legal Manager")]
        [HttpPost("{id}/Review")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewOpinionDTO dto) {
            await serviceManger.LegalOpinionService.ReviewAsync(id, dto, CurrentUserId);
            return Ok();
        }
    }
}