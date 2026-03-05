using CvStudio.Domain.Entities;

namespace CvStudio.Application.Repositories;

public interface IResumeRepository
{
    Task<IReadOnlyList<Resume>> ListAsync(CancellationToken cancellationToken = default);
    Task<Resume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Resume resume, CancellationToken cancellationToken = default);
    Task UpdateAsync(Resume resume, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}

