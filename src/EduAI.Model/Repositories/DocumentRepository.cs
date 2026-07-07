using EduAI.Model.Entities;
using EduAI.Model.Enums;
using EduAI.Model.IRepository;
using Microsoft.EntityFrameworkCore;

namespace EduAI.Model.Repositories;

public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Document>> GetBySubjectIdAsync(int subjectId) =>
        await DbSet.AsNoTracking()
            .Include(d => d.Chapter)
            .Include(d => d.Lesson)
            .Include(d => d.UploadedBy)
            .Include(d => d.LastModifiedBy)
            .Where(d => d.SubjectId == subjectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Document>> GetByChapterIdAsync(int chapterId) =>
        await DbSet.AsNoTracking()
            .Where(d => d.ChapterId == chapterId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Document>> GetByLessonIdAsync(int lessonId) =>
        await DbSet.AsNoTracking()
            .Where(d => d.LessonId == lessonId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<Document?> GetWithDetailsAsync(int id) =>
        await DbSet.Include(d => d.Subject)
            .Include(d => d.Chapter)
            .Include(d => d.Lesson)
            .Include(d => d.UploadedBy)
            .Include(d => d.LastModifiedBy)
            .FirstOrDefaultAsync(d => d.Id == id);

    // Documents that were never finished indexing (e.g. server restarted mid-processing).
    public async Task<IReadOnlyList<Document>> GetPendingIndexingAsync() =>
        await DbSet.AsNoTracking()
            .Where(d => d.IndexStatus == DocumentIndexStatus.Pending ||
                        d.IndexStatus == DocumentIndexStatus.Processing)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();
}
