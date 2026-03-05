using CvStudio.Application.DTOs;

namespace CvStudio.Application.Services;

public interface ISnapshotService
{
    Task<ResumeVersionDto> CreateSnapshotAsync(Guid resumeId, CreateVersionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResumeVersionDto>> ListSnapshotsAsync(Guid resumeId, CancellationToken cancellationToken = default);
    Task<ResumeVersionDto> GetSnapshotAsync(Guid resumeId, Guid versionId, CancellationToken cancellationToken = default);
    Task<ResumeVersionDto> UpdateSnapshotAsync(Guid resumeId, Guid versionId, UpdateVersionRequest request, CancellationToken cancellationToken = default);
    Task DeleteSnapshotAsync(Guid resumeId, Guid versionId, CancellationToken cancellationToken = default);
}

