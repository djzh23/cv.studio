using FluentAssertions;
using CvStudio.Application;
using CvStudio.Application.Contracts;
using CvStudio.Domain.Entities;

namespace CvStudio.UnitTests.Mapping;

public sealed class CvStudioMapperTests
{
    [Fact]
    public void ToDto_Resume_MapsAllProperties()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var expectedUpdatedAt = DateTime.UtcNow;
        var data = CreateResumeData();
        var entity = new Resume
        {
            Id = expectedId,
            Title = "My Resume",
            TemplateKey = "softwareentwickler",
            CurrentContentJson = CvStudioMapper.Serialize(data),
            UpdatedAtUtc = expectedUpdatedAt
        };

        // Act
        var dto = CvStudioMapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(expectedId);
        dto.Title.Should().Be("My Resume");
        dto.TemplateKey.Should().Be("softwareentwickler");
        dto.UpdatedAtUtc.Should().Be(expectedUpdatedAt);
        dto.ResumeData.Profile.FirstName.Should().Be("Jane");
        dto.ResumeData.Profile.LastName.Should().Be("Doe");
    }

    [Fact]
    public void ToDto_Snapshot_MapsAllProperties()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var expectedResumeId = Guid.NewGuid();
        var expectedCreatedAt = DateTime.UtcNow;
        var data = CreateResumeData("Max", "Mustermann");
        var entity = new Snapshot
        {
            Id = expectedId,
            ResumeId = expectedResumeId,
            VersionNumber = 4,
            Label = "Snapshot 4",
            ContentJson = CvStudioMapper.Serialize(data),
            CreatedAtUtc = expectedCreatedAt
        };

        // Act
        var dto = CvStudioMapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(expectedId);
        dto.ResumeId.Should().Be(expectedResumeId);
        dto.VersionNumber.Should().Be(4);
        dto.Label.Should().Be("Snapshot 4");
        dto.CreatedAtUtc.Should().Be(expectedCreatedAt);
        dto.ResumeData.Profile.FirstName.Should().Be("Max");
        dto.ResumeData.Profile.LastName.Should().Be("Mustermann");
    }

    [Fact]
    public void SerializeDeserialize_ValidResumeData_RoundTripsWithoutDataLoss()
    {
        // Arrange
        var original = CreateResumeData("Alice", "Walker");

        // Act
        var json = CvStudioMapper.Serialize(original);
        var deserialized = CvStudioMapper.Deserialize(json);

        // Assert
        deserialized.Profile.FirstName.Should().Be("Alice");
        deserialized.Profile.LastName.Should().Be("Walker");
        deserialized.Profile.Email.Should().Be("jane.doe@example.com");
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
