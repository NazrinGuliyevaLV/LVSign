using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public interface IEnvelopeNotificationService
    {
        Task NotifyEnvelopeSentAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default);

        Task NotifyRecipientSignedAsync(
            Guid envelopeId,
            Guid recipientId,
            CancellationToken cancellationToken = default);

        Task NotifyEnvelopeCompletedAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default);

        Task NotifyEnvelopeDeclinedAsync(
            Guid envelopeId,
            Guid recipientId,
            CancellationToken cancellationToken = default);

        Task NotifyEnvelopeVoidedAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default);
    }
}
