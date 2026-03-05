using Microsoft.EntityFrameworkCore;
using CvStudio.Application.Repositories;
using CvStudio.Domain.Entities;
using CvStudio.Infrastructure.Persistence;

namespace CvStudio.Infrastructure.Repositories;

public sealed class ResumeRepository : IResumeRepository
{
    private readonly CvStudioDbContext _dbContext;

    public ResumeRepository(CvStudioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Resume>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resumes
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Resume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        await _dbContext.Resumes.AddAsync(resume, cancellationToken);
    }

    public Task UpdateAsync(Resume resume, CancellationToken cancellationToken = default)
    {
        _dbContext.Resumes.Update(resume);
        return Task.CompletedTask;
    }

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Resumes.ExecuteDeleteAsync(cancellationToken);
    }
}

