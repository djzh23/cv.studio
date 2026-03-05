namespace CvStudio.Application.Services;

public interface IPdfExportService
{
    Task<byte[]> ExportAsync(Guid resumeId, Guid? versionId = null, PdfDesign design = PdfDesign.DesignA, CancellationToken cancellationToken = default);
}

