using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Email
{
    public interface IEmailSender
    {
        Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default);
    }
}
