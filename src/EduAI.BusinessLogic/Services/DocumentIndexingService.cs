using System.Text.Json;
using EduAI.BusinessLogic.Helpers;
using EduAI.BusinessLogic.IService;
using EduAI.Model.Constants;
using EduAI.Model.DTOs;
using EduAI.Model.Entities;
using EduAI.Model.Enums;
using EduAI.Model.IRepository;

namespace EduAI.BusinessLogic.Services;

public sealed class DocumentIndexingService : IDocumentIndexingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGeminiAiService _geminiAiService;
    private readonly INotificationService _notificationService;
    private readonly ISystemConfigurationService _systemConfigurationService;

    public DocumentIndexingService(
        IUnitOfWork unitOfWork,
        IGeminiAiService geminiAiService,
        INotificationService notificationService,
        ISystemConfigurationService systemConfigurationService)
    {
        _unitOfWork = unitOfWork;
        _geminiAiService = geminiAiService;
        _notificationService = notificationService;
        _systemConfigurationService = systemConfigurationService;
    }

    public async Task IndexAsync(int documentId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.Documents.GetWithDetailsAsync(documentId);
        if (document == null)
            return;

        if (!File.Exists(document.FilePath))
        {
            await SetStatusAsync(document, DocumentIndexStatus.Failed, "File is missing on server.");
            await NotifyProgressAsync(documentId, "Failed", new { status = "Failed", error = "File is missing on server." });
            return;
        }

        await SetStatusAsync(document, DocumentIndexStatus.Processing, null);
        await NotifyProgressAsync(documentId, "Started", new { status = "Processing", chunkCount = 0, embedded = 0 });

        try
        {
            // Idempotent: clear any partial/previous index so a re-queue (e.g. after a restart)
            // does not create duplicate chunks/embeddings.
            await ClearExistingIndexAsync(document);

            int chunkCount;
            await using (var readStream = File.OpenRead(document.FilePath))
            {
                chunkCount = await CreateChunksAsync(document, readStream, cancellationToken);
            }

            await NotifyProgressAsync(documentId, "ChunksCreated", new { status = "Processing", chunkCount, embedded = 0 });

            var createdChunks = await _unitOfWork.Chunks.GetByDocumentIdAsync(documentId);
            var embedded = 0;
            foreach (var chunk in createdChunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var embedInput = chunk.Content.Length > 8000 ? chunk.Content[..8000] : chunk.Content;
                var vector = await _geminiAiService.EmbedTextAsync(embedInput);
                await _unitOfWork.Embeddings.AddAsync(new DocumentEmbedding
                {
                    ChunkId = chunk.Id,
                    SubjectId = document.SubjectId,
                    ChapterId = document.ChapterId,
                    DocumentId = document.Id,
                    EmbeddingVector = VectorHelper.Serialize(vector)
                });
                embedded++;

                if (embedded % 5 == 0 || embedded == chunkCount)
                    await NotifyProgressAsync(documentId, "EmbeddingProgress", new { status = "Processing", chunkCount, embedded });
            }

            await _unitOfWork.SaveChangesAsync();

            await SetStatusAsync(document, DocumentIndexStatus.Indexed, null);
            await NotifyProgressAsync(documentId, "Completed", new
            {
                status = "Indexed",
                chunkCount,
                embedded,
                processedAt = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            // App is shutting down: leave status as Processing so it can be retried later.
            throw;
        }
        catch (Exception ex)
        {
            await SetStatusAsync(document, DocumentIndexStatus.Failed, ex.Message);
            await NotifyProgressAsync(documentId, "Failed", new { status = "Failed", error = ex.Message });
        }
    }

    private async Task ClearExistingIndexAsync(Document document)
    {
        var embeddings = await _unitOfWork.Embeddings.GetBySubjectIdAsync(document.SubjectId);
        foreach (var embedding in embeddings.Where(e => e.DocumentId == document.Id))
            _unitOfWork.Embeddings.Remove(embedding);

        var existingChunks = await _unitOfWork.Chunks.GetByDocumentIdAsync(document.Id);
        foreach (var chunk in existingChunks)
            _unitOfWork.Chunks.Remove(chunk);

        if (embeddings.Any(e => e.DocumentId == document.Id) || existingChunks.Count > 0)
            await _unitOfWork.SaveChangesAsync();
    }

    private async Task SetStatusAsync(Document document, DocumentIndexStatus status, string? error)
    {
        document.IndexStatus = status;
        document.IndexError = error is { Length: > 1000 } ? error[..1000] : error;
        document.IndexedAt = status == DocumentIndexStatus.Indexed ? DateTime.UtcNow : document.IndexedAt;
        _unitOfWork.Documents.Update(document);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<int> CreateChunksAsync(Document document, Stream fileStream, CancellationToken cancellationToken)
    {
        fileStream.Position = 0;
        var text = await DocumentTextExtractor.ExtractTextAsync(fileStream, document.FileName);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("No readable text was found in the uploaded file.");

        // Use system configuration for chunking strategy, fall back to defaults
        var config = await _systemConfigurationService.GetAsync();
        var textChunks = DocumentTextExtractor.ChunkText(
            text,
            chunkSize: config.ChunkSize,
            overlap: config.ChunkOverlap,
            strategy: config.ChunkingStrategy);

        if (textChunks.Count == 0)
            throw new InvalidOperationException("Document text could not be split into chunks.");

        var index = 0;
        foreach (var content in textChunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.Chunks.AddAsync(new DocumentChunk
            {
                SubjectId = document.SubjectId,
                ChapterId = document.ChapterId,
                DocumentId = document.Id,
                Content = content,
                ChunkIndex = index++
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return textChunks.Count;
    }

    private Task NotifyProgressAsync(int documentId, string action, object payload) =>
        _notificationService.NotifyAsync(new RealtimeEventDto
        {
            EntityType = "DocumentIndex",
            Action = action,
            EntityId = documentId,
            Message = JsonSerializer.Serialize(payload)
        });
}

