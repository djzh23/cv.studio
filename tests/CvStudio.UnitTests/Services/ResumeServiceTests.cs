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

public sealed class ResumeServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsResumeAndReturnsDto()
    {
        // Arrange
        var repository = new Mock<IResumeRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<ResumeService>>();
        var sut = new ResumeService(repository.Object, dbContext.Object, logger);

        Resume? addedResume = null;
        repository
            .Setup(x => x.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
            .Callback<Resume, CancellationToken>((resume, _) => addedResume = resume)
            .Returns(Task.CompletedTask);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new CreateResumeRequest
        {
            Title = "  Senior Developer CV  ",
            TemplateKey = "  Softwareentwickler  ",
            ResumeData = CreateResumeData()
        };

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        addedResume.Should().NotBeNull();
        addedResume!.Title.Should().Be("Senior Developer CV");
        addedResume.TemplateKey.Should().Be("softwareentwickler");

        result.Id.Should().Be(addedResume.Id);
        result.Title.Should().Be("Senior Developer CV");
        result.TemplateKey.Should().Be("softwareentwickler");
        result.ResumeData.Profile.FirstName.Should().Be("Jane");
        result.ResumeData.Profile.LastName.Should().Be("Doe");

        repository.Verify(x => x.AddAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()), Times.Once);
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentAsync_ExistingResume_ReturnsMappedDto()
    {
        // Arrange
        var repository = new Mock<IResumeRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<ResumeService>>();
        var sut = new ResumeService(repository.Object, dbContext.Object, logger);

        var expectedId = Guid.NewGuid();
        var entity = new Resume
        {
            Id = expectedId,
            Title = "Backend CV",
            TemplateKey = "it-support",
            CurrentContentJson = CvStudioMapper.Serialize(CreateResumeData()),
            UpdatedAtUtc = DateTime.UtcNow
        };

        repository
            .Setup(x => x.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await sut.GetCurrentAsync(expectedId);

        // Assert
        result.Id.Should().Be(expectedId);
        result.Title.Should().Be("Backend CV");
        result.TemplateKey.Should().Be("it-support");
        result.ResumeData.Profile.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task UpdateAsync_ExistingResume_UpdatesEntityAndReturnsDto()
    {
        // Arrange
        var repository = new Mock<IResumeRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<ResumeService>>();
        var sut = new ResumeService(repository.Object, dbContext.Object, logger);

        var resumeId = Guid.NewGuid();
        var entity = new Resume
        {
            Id = resumeId,
            Title = "Old Title",
            TemplateKey = "softwareentwickler",
            CurrentContentJson = CvStudioMapper.Serialize(CreateResumeData()),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        repository.Setup(x => x.GetByIdAsync(resumeId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repository.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new UpdateResumeRequest
        {
            Title = "  Updated Title  ",
            TemplateKey = "  it-support  ",
            ResumeData = CreateResumeData("John", "Smith")
        };

        // Act
        var result = await sut.UpdateAsync(resumeId, request);

        // Assert
        entity.Title.Should().Be("Updated Title");
        entity.TemplateKey.Should().Be("it-support");
        entity.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ResumeData.Profile.FirstName.Should().Be("John");
        result.ResumeData.Profile.LastName.Should().Be("Smith");

        repository.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentAsync_ResumeDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var repository = new Mock<IResumeRepository>();
        var dbContext = new Mock<IApplicationDbContext>();
        var logger = Mock.Of<ILogger<ResumeService>>();
        var sut = new ResumeService(repository.Object, dbContext.Object, logger);
        var missingId = Guid.NewGuid();

        repository
            .Setup(x => x.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resume?)null);

        // Act
        Func<Task> act = async () => await sut.GetCurrentAsync(missingId);

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
