using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Notifications;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public class SigningReminderService
         : ISigningReminderService
    {
        private readonly IRepository<Envelope>
            _envelopeRepository;

        private readonly IRepository<Recipients>
            _recipientRepository;

        private readonly IRepository<Notification>
            _notificationRepository;

        private readonly INotificationService
            _notificationService;

        public SigningReminderService(
            IRepository<Envelope> envelopeRepository,
            IRepository<Recipients> recipientRepository,
            IRepository<Notification> notificationRepository,
            INotificationService notificationService)
        {
            _envelopeRepository =
                envelopeRepository;

            _recipientRepository =
                recipientRepository;

            _notificationRepository =
                notificationRepository;

            _notificationService =
                notificationService;
        }

        public async Task ProcessDueRemindersAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            var envelopes =
                await _envelopeRepository.FindAsync(
                    x =>
                        !x.IsDeleted &&
                        x.ReminderIntervalDays.HasValue &&
                        x.ReminderIntervalDays.Value > 0 &&
                        (
                            x.Status == EnvelopeStatus.Sent ||
                            x.Status == EnvelopeStatus.InProgress
                        ),
                    cancellationToken);

            foreach (var envelope in envelopes)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                await ProcessEnvelopeAsync(
                    envelope,
                    now,
                    cancellationToken);
            }
        }

        private async Task ProcessEnvelopeAsync(
            Envelope envelope,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (!envelope.ReminderIntervalDays.HasValue)
            {
                return;
            }

            var interval =
                TimeSpan.FromDays(
                    envelope.ReminderIntervalDays.Value);

            var recipients =
                await _recipientRepository.FindAsync(
                    x =>
                        x.EnvelopeId == envelope.Id &&
                        (
                            x.Status == RecipientStatus.Active ||
                            x.Status == RecipientStatus.Viewed
                        ),
                    cancellationToken);

            foreach (var recipient in recipients)
            {
                if (!recipient.UserId.HasValue)
                {
                    continue;
                }

                if (!recipient.ActivatedAt.HasValue)
                {
                    continue;
                }

                var notifications =
                    await _notificationRepository.FindAsync(
                        x =>
                            x.UserId == recipient.UserId.Value &&
                            x.EnvelopeId == envelope.Id &&
                            x.Type ==
                                NotificationType.SigningReminder,
                        cancellationToken);

                var lastReminder =
                    notifications
                        .OrderByDescending(
                            x => x.CreatedAtUtc)
                        .FirstOrDefault();

                DateTimeOffset referenceTime;

                if (lastReminder is not null)
                {
                    referenceTime =
                        new DateTimeOffset(
                            lastReminder.CreatedAtUtc,
                            TimeSpan.Zero);
                }
                else
                {
                    referenceTime =
                        recipient.ActivatedAt.Value;
                }

                if (referenceTime + interval > now)
                {
                    continue;
                }

                await _notificationService.CreateAsync(
                    recipient.UserId.Value,
                    NotificationType.SigningReminder,
                    "Signing reminder",
                    $"Reminder: \"{envelope.Subject}\" is waiting for your action.",
                    envelope.Id,
                    cancellationToken);
            }
        }

    }
    }
