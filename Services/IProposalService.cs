using BlindMatchPAS.Models;

namespace BlindMatchPAS.Services
{
    public interface IProposalService
    {
        Task<List<Proposal>> GetProposalsByStudentAsync(string studentId);
        Task<List<Proposal>> GetPendingProposalsAsync(int? tagId);
        Task AddProposalAsync(Proposal proposal);
        Task UpdateProposalAsync(Proposal proposal);
        Task DeleteProposalAsync(int id);
        Task<Proposal?> GetByIdAsync(int id);
    }
}
