using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlindMatchPAS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly IProposalService _proposalService;
        private readonly IMatchService _matchService;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(IProposalService proposalService, IMatchService matchService, UserManager<ApplicationUser> userManager)
        {
            _proposalService = proposalService;
            _matchService = matchService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var proposals = await _proposalService.GetProposalsByStudentAsync(user!.Id);
            var matches = new List<Match>();
            foreach (var p in proposals)
            {
                var match = await _matchService.GetMatchByProposalAsync(p.Id);
                if (match != null) matches.Add(match);
            }
            ViewBag.Proposals = proposals;
            ViewBag.Matches = matches;
            ViewBag.Tags = await GetTagsAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProposal(string title, string description, string abstractText, string techStack, int tagId)
        {
            var user = await _userManager.GetUserAsync(User);
            await _proposalService.AddProposalAsync(new Proposal
            {
                Title = title,
                Description = description,
                Abstract = abstractText,
                TechStack = techStack,
                TagId = tagId,
                StudentId = user!.Id,
                Status = "Pending"
            });
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> EditProposal(int id, string title, string description, string abstractText, string techStack, int tagId)
        {
            var proposal = await _proposalService.GetByIdAsync(id);
            if (proposal != null && proposal.Status == "Pending")
            {
                proposal.Title = title;
                proposal.Description = description;
                proposal.Abstract = abstractText;
                proposal.TechStack = techStack;
                proposal.TagId = tagId;
                await _proposalService.UpdateProposalAsync(proposal);
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> WithdrawProposal(int id)
        {
            var proposal = await _proposalService.GetByIdAsync(id);
            if (proposal != null && proposal.Status == "Pending")
                await _proposalService.DeleteProposalAsync(id);
            return RedirectToAction("Dashboard");
        }

        private async Task<List<BlindMatchPAS.Models.Tag>> GetTagsAsync()
        {
            // Get tags via DI - using a small helper
            var db = HttpContext.RequestServices.GetRequiredService<BlindMatchPAS.Data.ApplicationDbContext>();
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Tags);
        }
    }
}
