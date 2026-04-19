using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlindMatchPAS.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly IProposalService _proposalService;
        private readonly IMatchService _matchService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupervisorController(IProposalService proposalService, IMatchService matchService, UserManager<ApplicationUser> userManager)
        {
            _proposalService = proposalService;
            _matchService = matchService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard(int? tagId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Proposals = await _proposalService.GetPendingProposalsAsync(tagId);
            ViewBag.ConfirmedMatches = await _matchService.GetMatchesBySupervisorAsync(user!.Id);
            ViewBag.Tags = await GetTagsAsync();
            ViewBag.SelectedTag = tagId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmMatch(int proposalId)
        {
            var user = await _userManager.GetUserAsync(User);
            await _matchService.ConfirmMatchAsync(proposalId, user!.Id);
            return RedirectToAction("Dashboard");
        }

        private async Task<List<BlindMatchPAS.Models.Tag>> GetTagsAsync()
        {
            var db = HttpContext.RequestServices.GetRequiredService<BlindMatchPAS.Data.ApplicationDbContext>();
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Tags);
        }
    }
}
