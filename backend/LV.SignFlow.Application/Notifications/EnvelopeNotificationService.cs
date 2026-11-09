using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public class EnvelopeNotificationService
        : IEnvelopeNotificationService
    {
        private readonly IRepository<Envelope> _envelopeRepository;
        private readonly IRepository<Recipients> _recipientRepository;
        private readonly INotificationService _notificationService;

        public EnvelopeNotificationService(
            IRepository<Envelope> envelopeRepository,
            IRepository<Recipients> recipientRepository,
            INotificationService notificationService)
        {
            _envelopeRepository = envelopeRepository;
            _recipientRepository = recipientRepository;
            _notificationService = notificationService;
        }

        public async Task NotifyEnvelopeSentAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null)
            {
                throw new InvalidOperationException(
                    "Envelope was not found.");
            }

            var recipients = await _recipientRepository.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            foreach (var recipient in recipients)
            {
                if (recipient.UserId is null)
                {
                    continue;
                }

                await _notificationService.CreateAsync(
                    recipient.UserId.Value,
                    NotificationType.EnvelopeSent,
                    "Signature requested",
                    $"\"{envelope.Subject}\" has been sent to you for signature.",
                    envelope.Id,
                    cancellationToken);
            }
        }

        public async Task NotifyRecipientSignedAsync(
            Guid envelopeId,
            Guid recipientId,
            CancellationToken cancellationToken = default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null)
            {
                throw new InvalidOperationException(
                    "Envelope was not found.");
            }

            var recipients = await _recipientRepository.FindAsync(
                x => x.Id == recipientId &&
                     x.EnvelopeId == envelopeId,
                cancellationToken);

            var recipient = recipients.FirstOrDefault();

            if (recipient is null)
            {
                throw new InvalidOperationException(
                    "Envelope recipient was not found.");
            }

            if (envelope.CreatedByUserId == Guid.Empty)
            {
                return;
            }

            var recipientName =
                !string.IsNullOrWhiteSpace(recipient.Name)
                    ? recipient.Name
                    : recipient.Email;

            await _notificationService.CreateAsync(
                envelope.CreatedByUserId,
                NotificationType.RecipientSigned,
                "Recipient signed",
                $"{recipientName} signed \"{envelope.Subject}\".",
                envelope.Id,
                cancellationToken);
        }

        public async Task NotifyEnvelopeCompletedAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null)
            {
                throw new InvalidOperationException(
                    "Envelope was not found.");
            }

            var recipients = await _recipientRepository.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            var userIds = recipients
                .Where(x => x.UserId.HasValue)
                .Select(x => x.UserId!.Value)
                .Append(envelope.CreatedByUserId)
                .Distinct()
                .ToList();

            foreach (var userId in userIds)
            {
                await _notificationService.CreateAsync(
                    userId,
                    NotificationType.EnvelopeCompleted,
                    "Envelope completed",
                    $"\"{envelope.Subject}\" has been completed.",
                    envelope.Id,
                    cancellationToken);
            }
        }

        public async Task NotifyEnvelopeDeclinedAsync(
            Guid envelopeId,
            Guid recipientId,
            CancellationToken cancellationToken = default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null)
            {
                throw new InvalidOperationException(
                    "Envelope was not found.");
            }

            var recipients = await _recipientRepository.FindAsync(
                x => x.Id == recipientId &&
                     x.EnvelopeId == envelopeId,
                cancellationToken);

            var recipient = recipients.FirstOrDefault();

            if (recipient is null)
            {
                throw new InvalidOperationException(
                    "Envelope recipient was not found.");
            }

            var recipientName =
                !string.IsNullOrWhiteSpace(recipient.Name)
                    ? recipient.Name
                    : recipient.Email;

            await _notificationService.CreateAsync(
                envelope.CreatedByUserId,
                NotificationType.EnvelopeDeclined,
                "Envelope declined",
                $"{recipientName} declined \"{envelope.Subject}\".",
                envelope.Id,
                cancellationToken);
        }

        public async Task NotifyEnvelopeVoidedAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null)
            {
                throw new InvalidOperationException(
                    "Envelope was not found.");
            }

            var recipients = await _recipientRepository.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            foreach (var recipient in recipients)
            {
                if (recipient.UserId is null)
                {
                    continue;
                }

                await _notificationService.CreateAsync(
                    recipient.UserId.Value,
                    NotificationType.EnvelopeVoided,
                    "Envelope voided",
                    $"\"{envelope.Subject}\" has been voided.",
                    envelope.Id,
                    cancellationToken);
            }
        }
    }
}
