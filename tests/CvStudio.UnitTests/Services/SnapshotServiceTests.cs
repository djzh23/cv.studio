using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using CvStudio.Application;
using CvStudio.Application.Contracts;
using CvStudio.Application.DTOs;
using CvStudio.Application.Exceptions;
using CvStudio.Application.Repositories;
using CvStudio.Application.Services;
using CvStudio.Domain.Entities;

namespace CvStudio.UnitTests.Services;

public sealed class SnapshotServiceTests
{
    [Fact]
    public async Task CreateSnapshotAsync_ValidRequest_PersistsSnapshotAndReturnsDto()
    {
        // Arrange
        var resumeRepository = new Mock<IResumeRepository>();
        var snapshotRepository = new Mock<ISnapshotRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<SnapshotService>>();
        var sut = new SnapshotService(resumeRepository.Object, snapshotRepository.Object, dbContext.Object, logger);

        var resumeId = Guid.NewGuid();
        var resume = new Resume
        {
            Id = resumeId,
            Title = "Resume",
            CurrentContentJson = CvStudioMapper.Serialize(CreateResumeData()),
            UpdatedAtUtc = DateTime.UtcNow
        };

        Snapshot? createdSnapshot = null;

        resumeRepository.Setup(x => x.GetByIdAsync(resumeId, It.IsAny<CancellationToken>())).ReturnsAsync(resume);
        snapshotRepository.Setup(x => x.GetNextVersionNumberAsync(resumeId, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        snapshotRepository
            .Setup(x => x.AddAsync(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()))
            .Callback<Snapshot, CancellationToken>((snapshot, _) => createdSnapshot = snapshot)
            .Returns(Task.CompletedTask);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new CreateVersionRequest
        {
            Label = "  Bewerbung Maerz  "
        };

        // Act
        var result = await sut.CreateSnapshotAsync(resumeId, request);

        // Assert
        createdSnapshot.Should().NotBeNull();
        createdSnapshot!.ResumeId.Should().Be(resumeId);
        createdSnapshot.VersionNumber.Should().Be(3);
        createdSnapshot.Label.Should().Be("Bewerbung Maerz");
        createdSnapshot.ContentJson.Should().Be(resume.CurrentContentJson);

        result.ResumeId.Should().Be(resumeId);
        result.VersionNumber.Should().Be(3);
        result.Label.Should().Be("Bewerbung Maerz");
        result.ResumeData.Profile.FirstName.Should().Be("Jane");

        snapshotRepository.Verify(x => x.AddAsync(It.IsAny<Snapshot>(), It.IsAny<CancellationToken>()), Times.Once);
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListSnapshotsAsync_ResumeExists_ReturnsMappedSnapshots()
    {
        // Arrange
        var resumeRepository = new Mock<IResumeRepository>();
        var snapshotRepository = new Mock<ISnapshotRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<SnapshotService>>();
        var sut = new SnapshotService(resumeRepository.Object, snapshotRepository.Object, dbContext.Object, logger);

        var resumeId = Guid.NewGuid();
        resumeRepository.Setup(x => x.GetByIdAsync(resumeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Resume { Id = resumeId, CurrentContentJson = "{}" });

        var snapshots = new List<Snapshot>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ResumeId = resumeId,
                VersionNumber = 1,
                Label = "V1",
                ContentJson = CvStudioMapper.Serialize(CreateResumeData("Alice", "Miller")),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ResumeId = resumeId,
                VersionNumber = 2,
                Label = "V2",
                ContentJson = CvStudioMapper.Serialize(CreateResumeData("Bob", "Miller")),
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        snapshotRepository.Setup(x => x.ListByResumeIdAsync(resumeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshots);

        // Act
        var result = await sut.ListSnapshotsAsync(resumeId);

        // Assert
        result.Should().HaveCount(2);
        result[0].VersionNumber.Should().Be(1);
        result[0].ResumeData.Profile.FirstName.Should().Be("Alice");
        result[1].VersionNumber.Should().Be(2);
        result[1].ResumeData.Profile.FirstName.Should().Be("Bob");
    }

    [Fact]
    public async Task UpdateSnapshotAsync_ExistingSnapshot_UpdatesLabelAndReturnsDto()
    {
        // Arrange
        var resumeRepository = new Mock<IResumeRepository>();
        var snapshotRepository = new Mock<ISnapshotRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<SnapshotService>>();
        var sut = new SnapshotService(resumeRepository.Object, snapshotRepository.Object, dbContext.Object, logger);

        var resumeId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var snapshot = new Snapshot
        {
            Id = snapshotId,
            ResumeId = resumeId,
            VersionNumber = 9,
            Label = "Old",
            ContentJson = CvStudioMapper.Serialize(CreateResumeData()),
            CreatedAtUtc = DateTime.UtcNow
        };

        snapshotRepository
            .Setup(x => x.GetTrackedByResumeAndVersionIdAsync(resumeId, snapshotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await sut.UpdateSnapshotAsync(resumeId, snapshotId, new UpdateVersionRequest { Label = "  New Label  " });

        // Assert
        snapshot.Label.Should().Be("New Label");
        result.Label.Should().Be("New Label");
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSnapshotAsync_SnapshotDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var resumeRepository = new Mock<IResumeRepository>();
        var snapshotRepository = new Mock<ISnapshotRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<SnapshotService>>();
        var sut = new SnapshotService(resumeRepository.Object, snapshotRepository.Object, dbContext.Object, logger);

        var resumeId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        snapshotRepository
            .Setup(x => x.GetByResumeAndVersionIdAsync(resumeId, snapshotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Snapshot?)null);

        // Act
        Func<Task> act = async () => await sut.GetSnapshotAsync(resumeId, snapshotId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static ResumeData CreateResumeData(string firstName = "Jane", string lastName = "Doe")
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = firstName,
                LastName = lastName,
                Email = "jane.doe@example.com"
            }
        };
    }
}
