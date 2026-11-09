using LV.SignFlow.Application.Notifications.Dtos;
using LV.SignFlow.Application.Notifications.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public interface INotificationPreferenceService
    {
        Task<NotificationPreferenceDto> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<NotificationPreferenceDto> UpdateAsync(
            Guid userId,
            UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken = default);
    }
}
