using Microsoft.Extensions.Logging;
using CvStudio.Application.DTOs;
using CvStudio.Application.Exceptions;
using CvStudio.Application.Repositories;
using CvStudio.Application.Templates;
using CvStudio.Application.Validation;
using CvStudio.Domain.Entities;

namespace CvStudio.Application.Services;

public sealed class ResumeService : IResumeService
{
    private readonly IResumeRepository _resumeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(
        IResumeRepository resumeRepository,
        IApplicationDbContext dbContext,
        ILogger<ResumeService> logger)
    {
        _resumeRepository = resumeRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<IReadOnlyList<ResumeTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult<IReadOnlyList<ResumeTemplateDto>>(ResumeTemplateCatalog.List());
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var deleted = await _resumeRepository.DeleteAllAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Deleted all resumes and snapshots for fresh start. Rows: {DeletedCount}", deleted);
        return deleted;
    }

    public async Task<IReadOnlyList<ResumeSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var resumes = await _resumeRepository.ListAsync(cancellationToken);
        return resumes
            .Select(x => new ResumeSummaryDto
            {
                Id = x.Id,
                Title = x.Title,
                TemplateKey = x.TemplateKey,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToList();
    }

    public async Task<ResumeDto> CreateFromTemplateAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeTemplateKeyRequired(templateKey);
        var template = ResumeTemplateCatalog.GetDefaultResume(normalizedKey);

        var now = DateTime.UtcNow;
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            Title = template.Title,
            TemplateKey = normalizedKey,
            CurrentContentJson = CvStudioMapper.Serialize(template.Data),
            UpdatedAtUtc = now
        };

        await _resumeRepository.AddAsync(resume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created resume {ResumeId} from template {TemplateKey}", resume.Id, normalizedKey);

        return CvStudioMapper.ToDto(resume);
    }

    public async Task<ResumeDto> CreateAsync(CreateResumeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateOrThrow(request, request.ResumeData);

        var now = DateTime.UtcNow;
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            TemplateKey = NormalizeTemplateKey(request.TemplateKey),
            CurrentContentJson = CvStudioMapper.Serialize(request.ResumeData),
            UpdatedAtUtc = now
        };

        await _resumeRepository.AddAsync(resume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created resume {ResumeId}", resume.Id);

        return CvStudioMapper.ToDto(resume);
    }

    public async Task<ResumeDto> GetCurrentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resume = await _resumeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Resume '{id}' was not found.");

        return CvStudioMapper.ToDto(resume);
    }

    public async Task<ResumeDto> UpdateAsync(Guid id, UpdateResumeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateOrThrow(request, request.ResumeData);

        var resume = await _resumeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Resume '{id}' was not found.");

        resume.Title = request.Title.Trim();
        resume.TemplateKey = NormalizeTemplateKey(request.TemplateKey) ?? resume.TemplateKey;
        resume.CurrentContentJson = CvStudioMapper.Serialize(request.ResumeData);
        resume.UpdatedAtUtc = DateTime.UtcNow;

        await _resumeRepository.UpdateAsync(resume, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated resume {ResumeId}", resume.Id);

        return CvStudioMapper.ToDto(resume);
    }

    private static string? NormalizeTemplateKey(string? templateKey)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            return null;
        }

        return templateKey.Trim().ToLowerInvariant();
    }

    private static string NormalizeTemplateKeyRequired(string templateKey)
    {
        var normalized = NormalizeTemplateKey(templateKey);
        if (normalized is null)
        {
            throw new UnprocessableEntityException(["Template key is required."]);
        }

        return normalized;
    }

    private static void ValidateOrThrow(params object[] models)
    {
        var errors = new List<string>();

        foreach (var model in models)
        {
            errors.AddRange(DataAnnotationsValidator.Validate(model));
        }

        if (errors.Count > 0)
        {
            throw new UnprocessableEntityException(errors);
        }
    }
}

