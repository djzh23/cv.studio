namespace CvStudio.Application.Services;

public interface IDocxExportService
{
    Task<byte[]> ExportAsync(Guid resumeId, Guid? versionId = null, CancellationToken cancellationToken = default);
}

