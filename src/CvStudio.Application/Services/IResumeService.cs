using CvStudio.Application.DTOs;

namespace CvStudio.Application.Services;

public interface IResumeService
{
    Task<IReadOnlyList<ResumeSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResumeTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
    Task<ResumeDto> CreateFromTemplateAsync(string templateKey, CancellationToken cancellationToken = default);
    Task<ResumeDto> CreateAsync(CreateResumeRequest request, CancellationToken cancellationToken = default);
    Task<ResumeDto> GetCurrentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResumeDto> UpdateAsync(Guid id, UpdateResumeRequest request, CancellationToken cancellationToken = default);
}

