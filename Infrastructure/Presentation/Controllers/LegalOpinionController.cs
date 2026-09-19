using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Service.Abstraction.Contracts;
using Shared.DTOs.LegalAiModule;
using Shared.DTOs.LegalOpinionModule;
using System.Security.Claims;

namespace Presentation.Controllers {

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LegalOpinionController(IServiceManger serviceManger) : ControllerBase {
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        #region ═══════════ GET FLOW (Lawyer, Legal Advisor, Legal Manger) ═══════════
        [Authorize(Policy = "LawyerOrAdvisorOrManger")]
        [HttpGet("Suggested-Questions")]
        public async Task<IActionResult> GetSuggestedQuestions([FromQuery] int count = 10)
            => Ok(await serviceManger.LegalOpinionService.GetSuggestedQuestionsAsync(count));

        [Authorize(Policy = "LawyerOrAdvisorOrManger")]
        [HttpGet("Document-By-Source")]
        public async Task<IActionResult> GetDocumentBySource([FromQuery] string fileName) {
            var (stream, resolvedFileName) = await serviceManger.LegalOpinionService.GetDocumentBySourceAsync(fileName);
            return File(stream, "application/octet-stream", resolvedFileName);
        }

        [Authorize(Policy = "LawyerOrAdvisorOrManger")]
        [HttpGet("Document/Download")]
        public async Task<IActionResult> Download([FromQuery] string fileName) {
            var stream = await serviceManger.LegalOpinionService.DownloadFileAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }
        #endregion

        #region ═══════════ ADD NEW OPINION FLOW (Legal Advisor) ═══════════
        [Authorize(Policy = "LegalAdvisor")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateLegalOpinionDTO dto)
            => Ok(await serviceManger.LegalOpinionService.CreateAsync(dto, CurrentUserId));

        [Authorize(Policy = "LegalAdvisor")]
        [HttpGet("Legal-Advisor-Opinions")]
        public async Task<IActionResult> GetAdvisorOpinions()
            => Ok(await serviceManger.LegalOpinionService.GetLegalOpinionsForUser(CurrentUserId));

        [Authorize(Policy = "LegalAdvisor")]
        [HttpPut("{id}/Resubmit")]
        public async Task<IActionResult> Resubmit(int id, [FromForm] UpdateLegalOpinionDTO dto)
            => Ok(await serviceManger.LegalOpinionService.ResubmitAsync(id, dto, CurrentUserId));

        [Authorize(Policy = "LegalAdvisor")]
        [HttpGet("Unknown-Questions")]
        public async Task<IActionResult> GetUnknownQuestions()
            => Ok(await serviceManger.UnknownQuestionService.GetAllAsync());

        [Authorize(Policy = "LegalAdvisor")]
        [HttpDelete("Unknown-Questions/{id}")]
        public async Task<IActionResult> DeleteUnknownQuestion(int id) {
            await serviceManger.UnknownQuestionService.DeleteAsync(id);
            return Ok();
        }
        #endregion

        #region ═══════════ REVIEW FLOW (Legal Manager) ═══════════
        [Authorize(Policy = "LegalManager")]
        [HttpGet]
        public async Task<IActionResult> GetAllForReview()
            => Ok(await serviceManger.LegalOpinionService.GetAllForReviewAsync());

        [Authorize(Policy = "AdvisorOrManger")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) {
            var isManager = User.IsInRole("Legal Manager");
            return Ok(await serviceManger.LegalOpinionService.GetByIdAsync(id, CurrentUserId, isManager));
        }

        [Authorize(Policy = "LegalManager")]
        [HttpPost("{id}/Review")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewOpinionDTO dto) {
            await serviceManger.LegalOpinionService.ReviewAsync(id, dto, CurrentUserId);
            return Ok();
        }
        #endregion

        #region ═══════════ DELETE LEGAL OPINION (Lagal Advisor, Lagal Advisor) ═══════════
        [Authorize(Policy = "AdvisorOrManger")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) {
            var isManager = User.IsInRole("Legal Manager");
            await serviceManger.LegalOpinionService.DeleteAsync(id, CurrentUserId, isManager);
            return Ok();
        } 
        #endregion
    }
}