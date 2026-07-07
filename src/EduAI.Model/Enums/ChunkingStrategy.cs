namespace EduAI.Model.Enums;

public enum ChunkingStrategy
{
    /// <summary>Split by paragraph breaks (natural boundaries).</summary>
    Paragraph = 0,

    /// <summary>Split by character/word count with configurable size and overlap.</summary>
    CharacterCount = 1,

    /// <summary>Split by file/segment size in bytes (approximate).</summary>
    SizeBased = 2
}
