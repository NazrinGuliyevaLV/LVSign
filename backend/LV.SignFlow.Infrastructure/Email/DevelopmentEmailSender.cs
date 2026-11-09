using LV.SignFlow.Application.Common.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Email
{
      public class DevelopmentEmailSender : IEmailSender
        {
            private readonly ILogger<DevelopmentEmailSender> _logger;

            public DevelopmentEmailSender(
                ILogger<DevelopmentEmailSender> logger)
            {
                _logger = logger;
            }

            public Task SendAsync(
                EmailMessage message,
                CancellationToken cancellationToken = default)
            {
                _logger.LogInformation(
                    """
                EMAIL
                To: {To}
                Subject: {Subject}
                Body:
                {Body}
                """,
                    message.To,
                    message.Subject,
                    message.Body);

                return Task.CompletedTask;
            }
        }
    }
