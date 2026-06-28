using EducationContentService.Domain.ValueObjects;

namespace EducationContentService.Domain.ModuleItems;

public sealed class ModuleItem
{
    // EF Core
    private ModuleItem()
    {
    }

    private ModuleItem(
        Guid id,
        Guid moduleId,
        ItemReference itemReference,
        Position position)
    {
        Id = id;
        ModuleId = moduleId;
        ItemReference = itemReference;
        Position = position;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static ModuleItem Create(
        Guid? id,
        Guid moduleId,
        ItemReference itemReference,
        Position position)
        => new(id ?? Guid.NewGuid(), moduleId, itemReference, position);

    public Guid Id { get; private set; }

    public Guid ModuleId { get; private set; }

    public ItemReference ItemReference { get; private set; } = null!;

    public Position Position { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}