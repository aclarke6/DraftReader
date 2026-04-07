using DraftView.Domain.Entities;
using DraftView.Domain.Enumerations;

namespace DraftView.Domain.Interfaces.Services;

public interface ISystemStateMessageService
{
    Task<SystemStateMessage> CreateMessageAsync(string message, SystemStateMessageSeverity severity = SystemStateMessageSeverity.Info, CancellationToken ct = default);
    Task DeactivateMessageAsync(Guid messageId, CancellationToken ct = default);
    Task<SystemStateMessage?> GetActiveMessageAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SystemStateMessage>> GetAllMessagesAsync(CancellationToken ct = default);
}
