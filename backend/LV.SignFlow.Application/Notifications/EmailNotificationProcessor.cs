using LV.SignFlow.Application.Common.Email;
using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Domain.Entities.Notifications;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public class EmailNotificationProcessor
        : IEmailNotificationProcessor
    {
        private const int MaxAttempts = 3;

        private readonly IRepository<NotificationDelivery>
            _deliveryRepository;

        private readonly IRepository<Notification>
            _notificationRepository;

        private readonly IRepository<User>
            _userRepository;

        private readonly IEmailSender
            _emailSender;

        private readonly IUnitOfWork
            _unitOfWork;

        public EmailNotificationProcessor(
            IRepository<NotificationDelivery> deliveryRepository,
            IRepository<Notification> notificationRepository,
            IRepository<User> userRepository,
            IEmailSender emailSender,
            IUnitOfWork unitOfWork)
        {
            _deliveryRepository =
                deliveryRepository;

            _notificationRepository =
                notificationRepository;

            _userRepository =
                userRepository;

            _emailSender =
                emailSender;

            _unitOfWork =
                unitOfWork;
        }

        public async Task ProcessPendingAsync(
            CancellationToken cancellationToken = default)
        {
            var deliveries =
                await _deliveryRepository.FindAsync(
                    x =>
                        x.Channel ==
                            NotificationChannel.Email &&
                        (
                            x.Status ==
                                NotificationStatus.Pending ||
                            x.Status ==
                                NotificationStatus.Failed
                        ) &&
                        x.AttemptCount < MaxAttempts,
                    cancellationToken);

            foreach (var delivery in deliveries)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                await ProcessDeliveryAsync(
                    delivery,
                    cancellationToken);
            }
        }

        private async Task ProcessDeliveryAsync(
            NotificationDelivery delivery,
            CancellationToken cancellationToken)
        {
            try
            {
                var notification =
                    await _notificationRepository.GetByIdAsync(
                        delivery.NotificationId,
                        cancellationToken);

                if (notification is null)
                {
                    delivery.Status =
                        NotificationStatus.Failed;

                    delivery.AttemptCount++;

                    delivery.ErrorMessage =
                        "Notification was not found.";

                    _deliveryRepository.Update(
                        delivery);

                    await _unitOfWork.SaveChangesAsync(
                        cancellationToken);

                    return;
                }

                var user =
                    await _userRepository.GetByIdAsync(
                        notification.UserId,
                        cancellationToken);

                if (user is null ||
                    string.IsNullOrWhiteSpace(user.Email))
                {
                    delivery.Status =
                        NotificationStatus.Failed;

                    delivery.AttemptCount++;

                    delivery.ErrorMessage =
                        "Recipient email address was not found.";

                    _deliveryRepository.Update(
                        delivery);

                    await _unitOfWork.SaveChangesAsync(
                        cancellationToken);

                    return;
                }

                delivery.AttemptCount++;

                await _emailSender.SendAsync(
                    new EmailMessage
                    {
                        To = user.Email,
                        Subject =
                            notification.Title,
                        Body =
                            BuildEmailBody(
                                notification),
                        IsHtml = true
                    },
                    cancellationToken);

                delivery.Status =
                    NotificationStatus.Sent;

                delivery.SentAtUtc =
                    DateTime.UtcNow;

                delivery.ErrorMessage =
                    null;

                _deliveryRepository.Update(
                    delivery);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                delivery.AttemptCount++;

                delivery.Status =
                    NotificationStatus.Failed;

                delivery.ErrorMessage =
                    ex.Message.Length > 2000
                        ? ex.Message[..2000]
                        : ex.Message;

                _deliveryRepository.Update(
                    delivery);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }

        private static string BuildEmailBody(
            Notification notification)
        {
            return $"""
                <html>
                    <body style="font-family:Arial,sans-serif;">
                        <h2>{notification.Title}</h2>

                        <p>{notification.Message}</p>

                        <p style="color:#666;font-size:12px;">
                            LV SignFlow
                        </p>
                    </body>
                </html>
                """;
        }
    }
}
