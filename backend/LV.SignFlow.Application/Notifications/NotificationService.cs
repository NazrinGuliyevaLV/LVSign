using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Notifications.Dtos;
using LV.SignFlow.Domain.Entities.Notifications;
using LV.SignFlow.Domain.Enums.NotificationEnums;

namespace LV.SignFlow.Application.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<NotificationDelivery>  _notificationDeliveryRepository;
        private readonly IRepository<NotificationPreference> _preferenceRepository;
        public NotificationService(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork,
            IRepository<NotificationDelivery> notificationDeliveryRepository,
            IRepository<NotificationPreference> preferenceRepository)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationDeliveryRepository = notificationDeliveryRepository;
            _preferenceRepository = preferenceRepository;
        }

        public async Task<Guid> CreateAsync(
     Guid userId,
     NotificationType type,
     string title,
     string message,
     Guid? envelopeId = null,
     CancellationToken cancellationToken = default)
        {
            var preferences =
                await _preferenceRepository.FindAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            var preference =
                preferences.FirstOrDefault();

            var typeEnabled =
                preference is null ||
                type switch
                {
                    NotificationType.EnvelopeSent =>
                        preference.EnvelopeSentEnabled,

                    NotificationType.SigningReminder =>
                        preference.SigningReminderEnabled,

                    NotificationType.EnvelopeCompleted =>
                        preference.EnvelopeCompletedEnabled,

                    NotificationType.EnvelopeDeclined =>
                        preference.EnvelopeDeclinedEnabled,

                    NotificationType.EnvelopeVoided =>
                        preference.EnvelopeVoidedEnabled,

                    NotificationType.RecipientSigned =>
                        preference.RecipientSignedEnabled,

                    _ => true
                };

            if (!typeEnabled)
            {
                return Guid.Empty;
            }

            var inAppEnabled =
                preference?.InAppEnabled ?? true;

            var emailEnabled =
                preference?.EmailEnabled ?? true;

            if (!inAppEnabled && !emailEnabled)
            {
                return Guid.Empty;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EnvelopeId = envelopeId,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(
                notification,
                cancellationToken);

            if (inAppEnabled)
            {
                var inAppDelivery =
                    new NotificationDelivery
                    {
                        Id = Guid.NewGuid(),
                        NotificationId = notification.Id,
                        Channel = NotificationChannel.InApp,
                        Status = NotificationStatus.Sent,
                        SentAtUtc = DateTime.UtcNow,
                        AttemptCount = 1
                    };

                await _notificationDeliveryRepository.AddAsync(
                    inAppDelivery,
                    cancellationToken);
            }

            if (emailEnabled)
            {
                var emailDelivery =
                    new NotificationDelivery
                    {
                        Id = Guid.NewGuid(),
                        NotificationId = notification.Id,
                        Channel = NotificationChannel.Email,
                        Status = NotificationStatus.Pending,
                        SentAtUtc = null,
                        AttemptCount = 0
                    };

                await _notificationDeliveryRepository.AddAsync(
                    emailDelivery,
                    cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return notification.Id;
        }

        public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
                Guid userId,
                int take = 50,
                CancellationToken cancellationToken = default)
        {
            if (take <= 0)
            {
                take = 50;
            }

            var notifications = await _notificationRepository.FindAsync(
                x => x.UserId == userId,
                cancellationToken);

            return notifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(take)
                .Select(x => new NotificationDto
                {
                    Id = x.Id,
                    EnvelopeId = x.EnvelopeId,
                    Type = x.Type,
                    Title = x.Title,
                    Message = x.Message,
                    IsRead = x.IsRead,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ReadAtUtc = x.ReadAtUtc
                })
                .ToList();
        }

        public async Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _notificationRepository.FindAsync(
                x => x.UserId == userId &&
                     !x.IsRead,
                cancellationToken);

            return notifications.Count;
        }

        public async Task MarkAsReadAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _notificationRepository.FindAsync(
               x => x.Id == notificationId &&
                    x.UserId == userId,
               cancellationToken);

            var notification = notifications.FirstOrDefault();

            if (notification is null)
            {
                throw new InvalidOperationException(
                    "Notification was not found.");
            }

            if (notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;

            _notificationRepository.Update(notification);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _notificationRepository.FindAsync(
               x => x.UserId == userId &&
                    !x.IsRead,
               cancellationToken);

            if (notifications.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = now;

                _notificationRepository.Update(notification);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}