using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public interface IEmailNotificationProcessor
    {
        Task ProcessPendingAsync(
            CancellationToken cancellationToken = default);
    }
}
