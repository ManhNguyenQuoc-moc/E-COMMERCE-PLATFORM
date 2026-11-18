namespace EcommercePlatform.Domain.Common;

public abstract record DomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

