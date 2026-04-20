using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers
{
    [Authorize(Roles = "ModuleLeader")]
    public class ModuleLeaderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMatchService _matchService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModuleLeaderController(ApplicationDbContext db, IMatchService matchService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _matchService = matchService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.Users = await _userManager.Users.Where(u => u.Role != "ModuleLeader").ToListAsync();
            ViewBag.Tags = await _db.Tags.ToListAsync();
            ViewBag.Matches = await _matchService.GetAllMatchesAsync();
            ViewBag.StudentCount = (await _userManager.GetUsersInRoleAsync("Student")).Count;
            ViewBag.SupervisorCount = (await _userManager.GetUsersInRoleAsync("Supervisor")).Count;
            ViewBag.Supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(string fullName, string email, string password, string role)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Role = role
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, role);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null) await _userManager.DeleteAsync(user);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AddTag(string name)
        {
            _db.Tags.Add(new Tag { Name = name });
            await _db.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveTag(int id)
        {
            var tag = await _db.Tags.FindAsync(id);
            if (tag != null) _db.Tags.Remove(tag);
            await _db.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> EditMatch(int matchId, string supervisorId)
        {
            await _matchService.ReassignMatchAsync(matchId, supervisorId);
            return RedirectToAction("Dashboard");
        }
    }
}
