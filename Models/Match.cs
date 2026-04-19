namespace BlindMatchPAS.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int ProposalId { get; set; }
        public Proposal? Proposal { get; set; }

        public string SupervisorId { get; set; } = string.Empty;
        public ApplicationUser? Supervisor { get; set; }

        public DateTime MatchedDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Confirmed"; // Confirmed, AdminChanged
    }
}
