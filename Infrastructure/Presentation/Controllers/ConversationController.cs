using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Service.Abstraction.Contracts;
using Shared.DTOs.ConversationModule;
using Shared.DTOs.LegalAiModule;
using System.Security.Claims;

namespace Presentation.Controllers {

    [Authorize(Policy = "LawyerOrAdvisorOrManger")]
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController(IServiceManger serviceManger) : ControllerBase {

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConversationDTO dto)
            => Ok(await serviceManger.ConversationService.CreateAsync(dto.Title, CurrentUserId));

        [HttpGet]
        public async Task<IActionResult> GetMyConversations()
            => Ok(await serviceManger.ConversationService.GetMyConversationsAsync(CurrentUserId));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHistory(int id)
            => Ok(await serviceManger.ConversationService.GetHistoryAsync(id, CurrentUserId));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) {
            await serviceManger.ConversationService.DeleteAsync(id, CurrentUserId);
            return Ok();
        }

        [HttpPost("Ask")]
        [RedisCache(durationInSec: 60)]
        public async Task<IActionResult> Ask([FromBody] AskQueryDTO dto)
            => Ok(await serviceManger.ConversationService.AskAsync(dto.ConversationId, dto.Query, CurrentUserId));
    }
}
