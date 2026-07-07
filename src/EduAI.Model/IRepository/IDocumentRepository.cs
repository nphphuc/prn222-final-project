using EduAI.Model.Entities;

namespace EduAI.Model.IRepository;

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<IReadOnlyList<Document>> GetBySubjectIdAsync(int subjectId);
    Task<IReadOnlyList<Document>> GetByChapterIdAsync(int chapterId);
    Task<IReadOnlyList<Document>> GetByLessonIdAsync(int lessonId);
    Task<Document?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Document>> GetPendingIndexingAsync();
}
