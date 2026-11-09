using LV.SignFlow.Application.Notifications.Dtos;
using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{

    public interface INotificationService
    {
        Task<Guid> CreateAsync(
            Guid userId,
            NotificationType type,
            string title,
            string message,
            Guid? envelopeId = null,
            CancellationToken cancellationToken = default);

        Task MarkAsReadAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
    Guid userId,
    int take = 50,
    CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
