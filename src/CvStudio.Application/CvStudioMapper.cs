using System.Text.Json;
using CvStudio.Application.Contracts;
using CvStudio.Application.DTOs;
using CvStudio.Domain.Entities;

namespace CvStudio.Application;

public static class CvStudioMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static string Serialize(ResumeData data) => JsonSerializer.Serialize(data, JsonOptions);

    public static ResumeData Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ResumeData>(json, JsonOptions) ?? new ResumeData();
    }

    public static ResumeDto ToDto(Resume resume)
    {
        return new ResumeDto
        {
            Id = resume.Id,
            Title = resume.Title,
            TemplateKey = resume.TemplateKey,
            ResumeData = Deserialize(resume.CurrentContentJson),
            UpdatedAtUtc = resume.UpdatedAtUtc
        };
    }

    public static ResumeVersionDto ToDto(Snapshot version)
    {
        return new ResumeVersionDto
        {
            Id = version.Id,
            ResumeId = version.ResumeId,
            VersionNumber = version.VersionNumber,
            Label = version.Label,
            ResumeData = Deserialize(version.ContentJson),
            CreatedAtUtc = version.CreatedAtUtc
        };
    }
}

