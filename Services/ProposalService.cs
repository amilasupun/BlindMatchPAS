using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Services
{
    public class ProposalService : IProposalService
    {
        private readonly ApplicationDbContext _db;

        public ProposalService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Proposal>> GetProposalsByStudentAsync(string studentId)
        {
            return await _db.Proposals
                .Include(p => p.Tag)
                .Where(p => p.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<List<Proposal>> GetPendingProposalsAsync(int? tagId)
        {
            var query = _db.Proposals.Include(p => p.Tag).Where(p => p.Status == "Pending");
            if (tagId.HasValue)
                query = query.Where(p => p.TagId == tagId.Value);
            return await query.ToListAsync();
        }

        public async Task AddProposalAsync(Proposal proposal)
        {
            _db.Proposals.Add(proposal);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateProposalAsync(Proposal proposal)
        {
            _db.Proposals.Update(proposal);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteProposalAsync(int id)
        {
            var proposal = await _db.Proposals.FindAsync(id);
            if (proposal != null)
            {
                _db.Proposals.Remove(proposal);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Proposal?> GetByIdAsync(int id)
        {
            return await _db.Proposals.FindAsync(id);
        }
    }
}
