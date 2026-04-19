using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Services
{
    public class MatchService : IMatchService
    {
        private readonly ApplicationDbContext _db;

        public MatchService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Match>> GetAllMatchesAsync()
        {
            return await _db.Matches
                .Include(m => m.Proposal!).ThenInclude(p => p!.Student)
                .Include(m => m.Supervisor)
                .ToListAsync();
        }

        public async Task<List<Match>> GetMatchesBySupervisorAsync(string supervisorId)
        {
            return await _db.Matches
                .Include(m => m.Proposal!).ThenInclude(p => p!.Student)
                .Include(m => m.Proposal!).ThenInclude(p => p!.Tag)
                .Where(m => m.SupervisorId == supervisorId)
                .ToListAsync();
        }

        public async Task<Match?> GetMatchByProposalAsync(int proposalId)
        {
            return await _db.Matches
                .Include(m => m.Supervisor)
                .FirstOrDefaultAsync(m => m.ProposalId == proposalId);
        }

        public async Task ConfirmMatchAsync(int proposalId, string supervisorId)
        {
            var proposal = await _db.Proposals.FindAsync(proposalId);
            if (proposal == null || proposal.Status != "Pending") return;

            proposal.Status = "Matched";
            _db.Matches.Add(new Match
            {
                ProposalId = proposalId,
                SupervisorId = supervisorId,
                Status = "Confirmed",
                MatchedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        public async Task ReassignMatchAsync(int matchId, string newSupervisorId)
        {
            var match = await _db.Matches.FindAsync(matchId);
            if (match == null) return;

            match.SupervisorId = newSupervisorId;
            match.Status = "AdminChanged";
            await _db.SaveChangesAsync();
        }
    }
}
