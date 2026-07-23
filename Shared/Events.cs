namespace Shared.Events;

public record LeadCreatedEvent(Guid LeadId, string CreatedAt);
