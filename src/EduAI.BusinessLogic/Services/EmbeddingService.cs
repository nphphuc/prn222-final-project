using EduAI.BusinessLogic.Helpers;
using EduAI.Model.Constants;
using EduAI.Model.DTOs;
using EduAI.Model.Entities;
using EduAI.Model.IRepository;
using EduAI.BusinessLogic.IService;

namespace EduAI.BusinessLogic.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGeminiAiService _geminiAiService;
    private readonly INotificationService _notificationService;

    public EmbeddingService(
        IUnitOfWork unitOfWork,
        IGeminiAiService geminiAiService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _geminiAiService = geminiAiService;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<EmbeddingDto>> GetBySubjectAsync(int subjectId, string userId, string role)
    {
        if (role != Roles.Admin)
            return Array.Empty<EmbeddingDto>();

        var embeddings = await _unitOfWork.Embeddings.GetBySubjectIdAsync(subjectId);
        return embeddings.Select(MapEntity).ToList();
    }

    public async Task<EmbeddingDto?> GetByIdAsync(int id, string userId, string role)
    {
        if (role != Roles.Admin)
            return null;

        var embedding = await _unitOfWork.Embeddings.GetByIdAsync(id);
        return embedding == null ? null : MapEntity(embedding);
    }

    public async Task<EmbeddingDto?> GetByChunkIdAsync(int chunkId, string userId, string role)
    {
        if (role != Roles.Admin)
            return null;

        var embedding = await _unitOfWork.Embeddings.GetByChunkIdAsync(chunkId);
        return embedding == null ? null : MapEntity(embedding);
    }

    public Task<EmbeddingOperationResultDto> CreateForChunkAsync(int chunkId, string userId, string role) =>
        RegenerateForChunkAsync(chunkId, userId, role);

    public async Task<EmbeddingOperationResultDto> RegenerateForChunkAsync(int chunkId, string userId, string role)
    {
        if (role != Roles.Admin)
            return Fail("Only admins can regenerate embeddings.");

        var chunk = await _unitOfWork.Chunks.GetByIdAsync(chunkId);
        if (chunk == null)
            return Fail("Chunk not found.");

        var vector = await _geminiAiService.EmbedTextAsync(chunk.Content);
        var embedding = await _unitOfWork.Embeddings.GetByChunkIdAsync(chunkId);

        if (embedding == null)
        {
            embedding = new DocumentEmbedding
            {
                ChunkId = chunk.Id,
                SubjectId = chunk.SubjectId,
                ChapterId = chunk.ChapterId,
                DocumentId = chunk.DocumentId,
                EmbeddingVector = VectorHelper.Serialize(vector)
            };
            await _unitOfWork.Embeddings.AddAsync(embedding);
        }
        else
        {
            embedding.EmbeddingVector = VectorHelper.Serialize(vector);
            _unitOfWork.Embeddings.Update(embedding);
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = MapEntity(embedding);
        await _notificationService.NotifyAsync(new RealtimeEventDto
        {
            EntityType = "Embedding",
            Action = "Regenerated",
            EntityId = embedding.Id,
            Message = $"Embedding regenerated for chunk #{chunk.ChunkIndex}"
        });

        return new EmbeddingOperationResultDto { Success = true, Embedding = dto };
    }

    public async Task<bool> DeleteAsync(int id, string userId, string role)
    {
        if (role != Roles.Admin)
            return false;

        var embedding = await _unitOfWork.Embeddings.GetByIdAsync(id);
        if (embedding == null)
            return false;

        _unitOfWork.Embeddings.Remove(embedding);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.NotifyAsync(new RealtimeEventDto
        {
            EntityType = "Embedding",
            Action = "Deleted",
            EntityId = id,
            Message = $"Embedding #{id} deleted"
        });

        return true;
    }

    private static EmbeddingDto MapEntity(DocumentEmbedding embedding) =>
        MapToDto(
            embedding.EmbeddingVector,
            embedding.Id,
            embedding.ChunkId,
            embedding.SubjectId,
            embedding.DocumentId,
            embedding.ChapterId,
            embedding.CreatedAt);

    internal static EmbeddingDto MapToDto(
        string vectorJson,
        int id,
        int chunkId,
        int subjectId,
        int documentId,
        int chapterId,
        DateTime createdAt)
    {
        var preview = "Not available";
        var dimensionCount = 0;

        if (VectorHelper.TryDeserialize(vectorJson, out var vector))
        {
            dimensionCount = vector.Length;
            var sample = vector.Take(8).Select(v => v.ToString("0.####"));
            preview = $"[{string.Join(", ", sample)}{(vector.Length > 8 ? ", ..." : "")}]";
        }
        else if (!string.IsNullOrWhiteSpace(vectorJson))
        {
            preview = vectorJson.Length > 120 ? vectorJson[..120] + "..." : vectorJson;
            dimensionCount = vectorJson.Length;
        }

        return new EmbeddingDto
        {
            Id = id,
            ChunkId = chunkId,
            SubjectId = subjectId,
            DocumentId = documentId,
            ChapterId = chapterId,
            DimensionCount = dimensionCount,
            VectorPreview = preview,
            CreatedAt = createdAt
        };
    }

    private static EmbeddingOperationResultDto Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
