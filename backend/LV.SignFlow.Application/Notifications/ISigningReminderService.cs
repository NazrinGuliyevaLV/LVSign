using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public interface ISigningReminderService
    {
        Task ProcessDueRemindersAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken = default);
    }
}
