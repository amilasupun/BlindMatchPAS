using BlindMatchPAS.Data;
using BlindMatchPAS.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Models = BlindMatchPAS.Models;

namespace BlindMatchPAS.Tests
{
    // =====================================================================
    // INTEGRATION TESTS  – use real in-memory EF Core database
    // These tests verify that MatchService correctly persists and reads data.
    // =====================================================================
    public class MatchServiceIntegrationTests
    {
        private ApplicationDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task ConfirmMatch_ShouldSetStatusToMatched()
        {
            // Arrange
            var db = GetInMemoryDb();
            db.Proposals.Add(new Models.Proposal
            {
                Id = 1,
                Title = "Test Proposal",
                Description = "Desc",
                Abstract = "Abstract",
                TechStack = "ASP.NET",
                StudentId = "student1",
                TagId = 1,
                Status = "Pending"
            });
            await db.SaveChangesAsync();

            var service = new MatchService(db);

            // Act
            await service.ConfirmMatchAsync(1, "supervisor1");

            // Assert
            var proposal = await db.Proposals.FindAsync(1);
            Assert.Equal("Matched", proposal!.Status);
        }

        [Fact]
        public async Task ConfirmMatch_ShouldCreateMatchRecord()
        {
            // Arrange
            var db = GetInMemoryDb();
            db.Proposals.Add(new Models.Proposal
            {
                Id = 2,
                Title = "Test Proposal 2",
                Description = "Desc",
                Abstract = "Abstract",
                TechStack = "ASP.NET",
                StudentId = "student2",
                TagId = 1,
                Status = "Pending"
            });
            await db.SaveChangesAsync();

            var service = new MatchService(db);

            // Act
            await service.ConfirmMatchAsync(2, "supervisor2");

            // Assert
            var match = await db.Matches.FirstOrDefaultAsync(m => m.ProposalId == 2);
            Assert.NotNull(match);
            Assert.Equal("supervisor2", match.SupervisorId);
            Assert.Equal("Confirmed", match.Status);
        }

        [Fact]
        public async Task ConfirmMatch_ShouldNotMatch_WhenAlreadyMatched()
        {
            // Arrange – proposal is already in "Matched" state
            var db = GetInMemoryDb();
            db.Proposals.Add(new Models.Proposal
            {
                Id = 3,
                Title = "Test Proposal 3",
                Description = "Desc",
                Abstract = "Abstract",
                TechStack = "ASP.NET",
                StudentId = "student3",
                TagId = 1,
                Status = "Matched"   // already matched
            });
            await db.SaveChangesAsync();

            var service = new MatchService(db);

            // Act
            await service.ConfirmMatchAsync(3, "supervisor3");

            // Assert – no new Models.Match record should be created
            var matchCount = await db.Matches.CountAsync(m => m.ProposalId == 3);
            Assert.Equal(0, matchCount);
        }

        [Fact]
        public async Task ReassignMatch_ShouldUpdateSupervisorAndStatus()
        {
            // Arrange
            var db = GetInMemoryDb();
            db.Matches.Add(new Models.Match
            {
                Id = 1,
                ProposalId = 1,
                SupervisorId = "oldSupervisor",
                Status = "Confirmed"
            });
            await db.SaveChangesAsync();

            var service = new MatchService(db);

            // Act
            await service.ReassignMatchAsync(1, "newSupervisor");

            // Assert
            var match = await db.Matches.FindAsync(1);
            Assert.Equal("newSupervisor", match!.SupervisorId);
            Assert.Equal("AdminChanged", match.Status);
        }

        [Fact]
        public async Task GetAllMatches_ShouldReturnAllMatches()
        {
            // Arrange
            // ApplicationUser records needed for .ThenInclude(p => p!.Student) and .Include(m => m.Supervisor)
            var db = GetInMemoryDb();
            db.Users.AddRange(
                new Models.ApplicationUser { Id = "s1", UserName = "s1@test.com", Email = "s1@test.com", FullName = "Student One", Role = "Student" },
                new Models.ApplicationUser { Id = "s2", UserName = "s2@test.com", Email = "s2@test.com", FullName = "Student Two", Role = "Student" },
                new Models.ApplicationUser { Id = "sup1", UserName = "sup1@test.com", Email = "sup1@test.com", FullName = "Supervisor One", Role = "Supervisor" },
                new Models.ApplicationUser { Id = "sup2", UserName = "sup2@test.com", Email = "sup2@test.com", FullName = "Supervisor Two", Role = "Supervisor" }
            );
            db.Tags.Add(new Models.Tag { Id = 1, Name = "AI" });
            db.Proposals.AddRange(
                new Models.Proposal { Id = 1, Title = "P1", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s1", TagId = 1, Status = "Pending" },
                new Models.Proposal { Id = 2, Title = "P2", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s2", TagId = 1, Status = "Pending" }
            );
            db.Matches.AddRange(
                new Models.Match { ProposalId = 1, SupervisorId = "sup1", Status = "Confirmed" },
                new Models.Match { ProposalId = 2, SupervisorId = "sup2", Status = "Confirmed" }
            );
            await db.SaveChangesAsync();

            var service = new MatchService(db);

            // Act
            var results = await service.GetAllMatchesAsync();

            // Assert
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task GetMatchesBySupervisor_ShouldReturnOnlySupervisorMatches()
        {
            // Arrange
            // ApplicationUser + Tag records needed for nested Include/ThenInclude chains
            var db = GetInMemoryDb();
            db.Users.AddRange(
                new Models.ApplicationUser { Id = "s1", UserName = "s1@test.com", Email = "s1@test.com", FullName = "Student One", Role = "Student" },
                new Models.ApplicationUser { Id = "s2", UserName = "s2@test.com", Email = "s2@test.com", FullName = "Student Two", Role = "Student" },
                new Models.ApplicationUser { Id = "s3", UserName = "s3@test.com", Email = "s3@test.com", FullName = "Student Three", Role = "Student" },
                new Models.ApplicationUser { Id = "sup1", UserName = "sup1@test.com", Email = "sup1@test.com", FullName = "Supervisor One", Role = "Supervisor" },
                new Models.ApplicationUser { Id = "sup2", UserName = "sup2@test.com", Email = "sup2@test.com", FullName = "Supervisor Two", Role = "Supervisor" }
            );
            db.Tags.Add(new Models.Tag { Id = 1, Name = "AI" });
            db.Proposals.AddRange(
                new Models.Proposal { Id = 1, Title = "P1", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s1", TagId = 1, Status = "Pending" },
                new Models.Proposal { Id = 2, Title = "P2", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s2", TagId = 1, Status = "Pending" },
                new Models.Proposal { Id = 3, Title = "P3", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s3", TagId = 1, Status = "Pending" }
            );
            db.Matches.AddRange(
                new Models.Match { ProposalId = 1, SupervisorId = "sup1", Status = "Confirmed" },
                new Models.Match { ProposalId = 2, SupervisorId = "sup2", Status = "Confirmed" },
                new Models.Match { ProposalId = 3, SupervisorId = "sup1", Status = "Confirmed" }
            );
            await db.SaveChangesAsync();

            var service = new MatchService(db);

            // Act
            var results = await service.GetMatchesBySupervisorAsync("sup1");

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, m => Assert.Equal("sup1", m.SupervisorId));
        }

        [Fact]
        public async Task GetMatchByProposal_ShouldReturnNull_WhenNoMatch()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = new MatchService(db);

            // Act
            var result = await service.GetMatchByProposalAsync(999);

            // Assert
            Assert.Null(result);
        }
    }

    // =====================================================================
    // UNIT TESTS  – use Moq to mock IMatchService
    // These tests verify consumer logic (e.g., a controller or orchestrator)
    // in complete isolation from the database.
    // =====================================================================
    public class MatchServiceMockTests
    {
        [Fact]
        public async Task ConfirmMatchAsync_IsCalled_WithCorrectArguments()
        {
            // Arrange
            var mockMatchService = new Mock<IMatchService>();
            mockMatchService
                .Setup(s => s.ConfirmMatchAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act – simulate a controller calling the service
            await mockMatchService.Object.ConfirmMatchAsync(10, "supervisor99");

            // Assert – verify the method was called exactly once with the right args
            mockMatchService.Verify(
                s => s.ConfirmMatchAsync(10, "supervisor99"),
                Times.Once);
        }

        [Fact]
        public async Task GetAllMatchesAsync_ReturnsMockedData()
        {
            // Arrange
            var expectedMatches = new List<Models.Match>
            {
                new Models.Match { Id = 1, ProposalId = 1, SupervisorId = "sup1", Status = "Confirmed" },
                new Models.Match { Id = 2, ProposalId = 2, SupervisorId = "sup2", Status = "Confirmed" }
            };

            var mockMatchService = new Mock<IMatchService>();
            mockMatchService
                .Setup(s => s.GetAllMatchesAsync())
                .Returns(Task.FromResult(expectedMatches));

            // Act
            var result = await mockMatchService.Object.GetAllMatchesAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("sup1", result[0].SupervisorId);
        }

        [Fact]
        public async Task ReassignMatchAsync_IsCalled_WithCorrectMatchIdAndSupervisor()
        {
            // Arrange
            var mockMatchService = new Mock<IMatchService>();
            mockMatchService
                .Setup(s => s.ReassignMatchAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await mockMatchService.Object.ReassignMatchAsync(5, "newSup");

            // Assert
            mockMatchService.Verify(
                s => s.ReassignMatchAsync(5, "newSup"),
                Times.Once);
        }

        [Fact]
        public async Task GetMatchesBySupervisorAsync_ReturnsMockedList()
        {
            // Arrange
            var mockMatches = new List<Models.Match>
            {
                new Models.Match { ProposalId = 1, SupervisorId = "sup1", Status = "Confirmed" }
            };

            var mockMatchService = new Mock<IMatchService>();
            mockMatchService
                .Setup(s => s.GetMatchesBySupervisorAsync("sup1"))
                .Returns(Task.FromResult(mockMatches));

            // Act
            var result = await mockMatchService.Object.GetMatchesBySupervisorAsync("sup1");

            // Assert
            Assert.Single(result);
            mockMatchService.Verify(s => s.GetMatchesBySupervisorAsync("sup1"), Times.Once);
        }

        [Fact]
        public async Task GetMatchByProposalAsync_ReturnsMockedMatch()
        {
            // Arrange
            var expectedMatch = new Models.Match { Id = 7, ProposalId = 3, SupervisorId = "sup3" };

            var mockMatchService = new Mock<IMatchService>();
            mockMatchService
                .Setup(s => s.GetMatchByProposalAsync(3))
                .ReturnsAsync(expectedMatch);

            // Act
            var result = await mockMatchService.Object.GetMatchByProposalAsync(3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result!.Id);
        }

        [Fact]
        public async Task ConfirmMatchAsync_IsNeverCalled_WhenNotInvoked()
        {
            // Arrange
            var mockMatchService = new Mock<IMatchService>();

            // Act – intentionally do NOT call ConfirmMatchAsync

            // Assert – verify it was never called
            mockMatchService.Verify(
                s => s.ConfirmMatchAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}