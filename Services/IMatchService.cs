using BlindMatchPAS.Models;

namespace BlindMatchPAS.Services
{
    public interface IMatchService
    {
        Task<List<Match>> GetAllMatchesAsync();
        Task<List<Match>> GetMatchesBySupervisorAsync(string supervisorId);
        Task<Match?> GetMatchByProposalAsync(int proposalId);
        Task ConfirmMatchAsync(int proposalId, string supervisorId);
        Task ReassignMatchAsync(int matchId, string newSupervisorId);
    }
}
