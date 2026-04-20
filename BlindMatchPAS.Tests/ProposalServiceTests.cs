using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BlindMatchPAS.Tests
{
    // =====================================================================
    // INTEGRATION TESTS  – use real in-memory EF Core database
    // These tests verify that ProposalService correctly persists and reads data.
    // =====================================================================
    public class ProposalServiceIntegrationTests
    {
        private ApplicationDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task AddProposal_ShouldSaveToDatabase()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = new ProposalService(db);

            // Act
            await service.AddProposalAsync(new Proposal
            {
                Title = "AI Project",
                Description = "Desc",
                Abstract = "Abstract",
                TechStack = "Python",
                StudentId = "s1",
                TagId = 1,
                Status = "Pending"
            });

            // Assert
            var count = await db.Proposals.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetProposalsByStudent_ShouldReturnOnlyStudentProposals()
        {
            // Arrange – Tag record needed because GetProposalsByStudentAsync uses .Include(p => p.Tag)
            var db = GetInMemoryDb();
            db.Tags.Add(new Tag { Id = 1, Name = "AI" });
            db.Proposals.AddRange(
                new Proposal { Title = "P1", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s1", TagId = 1, Status = "Pending" },
                new Proposal { Title = "P2", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s2", TagId = 1, Status = "Pending" }
            );
            await db.SaveChangesAsync();

            var service = new ProposalService(db);

            // Act
            var results = await service.GetProposalsByStudentAsync("s1");

            // Assert
            Assert.Single(results);
            Assert.Equal("P1", results[0].Title);
        }

        [Fact]
        public async Task GetPendingProposals_ShouldNotReturnMatchedProposals()
        {
            // Arrange
            var db = GetInMemoryDb();
            db.Tags.Add(new Tag { Id = 1, Name = "AI" });
            db.Proposals.AddRange(
                new Proposal { Title = "Pending", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s1", TagId = 1, Status = "Pending" },
                new Proposal { Title = "Matched", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s2", TagId = 1, Status = "Matched" }
            );
            await db.SaveChangesAsync();

            var service = new ProposalService(db);

            // Act
            var results = await service.GetPendingProposalsAsync(null);

            // Assert
            Assert.Single(results);
            Assert.Equal("Pending", results[0].Title);
        }

        [Fact]
        public async Task DeleteProposal_ShouldRemoveFromDatabase()
        {
            // Arrange
            var db = GetInMemoryDb();
            db.Proposals.Add(new Proposal
            {
                Id = 1,
                Title = "To Delete",
                Description = "D",
                Abstract = "A",
                TechStack = "T",
                StudentId = "s1",
                TagId = 1,
                Status = "Pending"
            });
            await db.SaveChangesAsync();

            var service = new ProposalService(db);

            // Act
            await service.DeleteProposalAsync(1);

            // Assert
            var count = await db.Proposals.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetPendingProposals_WithTagFilter_ShouldReturnOnlyMatchingTag()
        {
            // Arrange
            var db = GetInMemoryDb();
            db.Tags.AddRange(
                new Tag { Id = 1, Name = "AI" },
                new Tag { Id = 2, Name = "Web" }
            );
            db.Proposals.AddRange(
                new Proposal { Title = "AI Project",  Description = "D", Abstract = "A", TechStack = "T", StudentId = "s1", TagId = 1, Status = "Pending" },
                new Proposal { Title = "Web Project", Description = "D", Abstract = "A", TechStack = "T", StudentId = "s2", TagId = 2, Status = "Pending" }
            );
            await db.SaveChangesAsync();

            var service = new ProposalService(db);

            // Act – filter by TagId = 1
            var results = await service.GetPendingProposalsAsync(1);

            // Assert
            Assert.Single(results);
            Assert.Equal("AI Project", results[0].Title);
        }

        [Fact]
        public async Task UpdateProposal_ShouldPersistChanges()
        {
            // Arrange
            var db = GetInMemoryDb();
            var proposal = new Proposal
            {
                Id = 1,
                Title = "Old Title",
                Description = "D",
                Abstract = "A",
                TechStack = "T",
                StudentId = "s1",
                TagId = 1,
                Status = "Pending"
            };
            db.Proposals.Add(proposal);
            await db.SaveChangesAsync();

            var service = new ProposalService(db);

            // Act
            proposal.Title = "Updated Title";
            await service.UpdateProposalAsync(proposal);

            // Assert
            var updated = await db.Proposals.FindAsync(1);
            Assert.Equal("Updated Title", updated!.Title);
        }

        [Fact]
        public async Task GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = new ProposalService(db);

            // Act
            var result = await service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }
    }

    // =====================================================================
    // UNIT TESTS  – use Moq to mock IProposalService
    // These tests verify consumer logic in complete isolation from the database.
    // =====================================================================
    public class ProposalServiceMockTests
    {
        [Fact]
        public async Task AddProposalAsync_IsCalled_WithCorrectProposal()
        {
            // Arrange
            var mockProposalService = new Mock<IProposalService>();
            mockProposalService
                .Setup(s => s.AddProposalAsync(It.IsAny<Proposal>()))
                .Returns(Task.CompletedTask);

            var newProposal = new Proposal
            {
                Title = "New AI Project",
                StudentId = "s1",
                Status = "Pending"
            };

            // Act
            await mockProposalService.Object.AddProposalAsync(newProposal);

            // Assert
            mockProposalService.Verify(
                s => s.AddProposalAsync(It.Is<Proposal>(p => p.Title == "New AI Project")),
                Times.Once);
        }

        [Fact]
        public async Task GetProposalsByStudentAsync_ReturnsMockedList()
        {
            // Arrange
            var mockProposals = new List<Proposal>
            {
                new Proposal { Id = 1, Title = "Mock Proposal", StudentId = "s1", Status = "Pending" }
            };

            var mockProposalService = new Mock<IProposalService>();
            mockProposalService
                .Setup(s => s.GetProposalsByStudentAsync("s1"))
                .ReturnsAsync(mockProposals);

            // Act
            var result = await mockProposalService.Object.GetProposalsByStudentAsync("s1");

            // Assert
            Assert.Single(result);
            Assert.Equal("Mock Proposal", result[0].Title);
        }

        [Fact]
        public async Task GetPendingProposalsAsync_ReturnsMockedData()
        {
            // Arrange
            var mockProposals = new List<Proposal>
            {
                new Proposal { Id = 1, Title = "Pending 1", Status = "Pending" },
                new Proposal { Id = 2, Title = "Pending 2", Status = "Pending" }
            };

            var mockProposalService = new Mock<IProposalService>();
            mockProposalService
                .Setup(s => s.GetPendingProposalsAsync(null))
                .ReturnsAsync(mockProposals);

            // Act
            var result = await mockProposalService.Object.GetPendingProposalsAsync(null);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DeleteProposalAsync_IsCalled_WithCorrectId()
        {
            // Arrange
            var mockProposalService = new Mock<IProposalService>();
            mockProposalService
                .Setup(s => s.DeleteProposalAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Act
            await mockProposalService.Object.DeleteProposalAsync(42);

            // Assert
            mockProposalService.Verify(s => s.DeleteProposalAsync(42), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMockedProposal()
        {
            // Arrange
            var expectedProposal = new Proposal { Id = 5, Title = "Specific Project", Status = "Pending" };

            var mockProposalService = new Mock<IProposalService>();
            mockProposalService
                .Setup(s => s.GetByIdAsync(5))
                .ReturnsAsync(expectedProposal);

            // Act
            var result = await mockProposalService.Object.GetByIdAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Specific Project", result!.Title);
        }

        [Fact]
        public async Task UpdateProposalAsync_IsCalled_Once()
        {
            // Arrange
            var mockProposalService = new Mock<IProposalService>();
            mockProposalService
                .Setup(s => s.UpdateProposalAsync(It.IsAny<Proposal>()))
                .Returns(Task.CompletedTask);

            var proposal = new Proposal { Id = 1, Title = "Updated", Status = "Pending" };

            // Act
            await mockProposalService.Object.UpdateProposalAsync(proposal);

            // Assert
            mockProposalService.Verify(s => s.UpdateProposalAsync(proposal), Times.Once);
        }
    }
}