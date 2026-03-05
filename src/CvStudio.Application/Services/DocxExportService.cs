using Microsoft.Extensions.Logging;
using CvStudio.Application.Exceptions;
using CvStudio.Application.Repositories;

namespace CvStudio.Application.Services;

public sealed class DocxExportService : IDocxExportService
{
    private readonly IResumeRepository _resumeRepository;
    private readonly ISnapshotRepository _versionRepository;
    private readonly IDocxGenerator _docxGenerator;
    private readonly ILogger<DocxExportService> _logger;

    public DocxExportService(
        IResumeRepository resumeRepository,
        ISnapshotRepository versionRepository,
        IDocxGenerator docxGenerator,
        ILogger<DocxExportService> logger)
    {
        _resumeRepository = resumeRepository;
        _versionRepository = versionRepository;
        _docxGenerator = docxGenerator;
        _logger = logger;
    }

    public async Task<byte[]> ExportAsync(Guid resumeId, Guid? versionId = null, CancellationToken cancellationToken = default)
    {
        var contentJson = await ResolveContentJsonAsync(resumeId, versionId, cancellationToken);
        var bytes = _docxGenerator.GenerateFromResumeJson(contentJson);

        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("DOCX generation produced no content.");
        }

        _logger.LogInformation("Generated DOCX for resume {ResumeId}, version {VersionId}", resumeId, versionId);
        return bytes;
    }

    private async Task<string> ResolveContentJsonAsync(Guid resumeId, Guid? versionId, CancellationToken cancellationToken)
    {
        if (versionId.HasValue)
        {
            var version = await _versionRepository.GetByResumeAndVersionIdAsync(resumeId, versionId.Value, cancellationToken)
                ?? throw new NotFoundException($"Version '{versionId}' for resume '{resumeId}' was not found.");

            return version.ContentJson;
        }

        var resume = await _resumeRepository.GetByIdAsync(resumeId, cancellationToken)
            ?? throw new NotFoundException($"Resume '{resumeId}' was not found.");

        return resume.CurrentContentJson;
    }
}

