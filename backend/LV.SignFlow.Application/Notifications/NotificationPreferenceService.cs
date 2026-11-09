using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Notifications.Dtos;
using LV.SignFlow.Application.Notifications.Requests;
using LV.SignFlow.Domain.Entities.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications
{
    public class NotificationPreferenceService
          : INotificationPreferenceService
    {
        private readonly IRepository<NotificationPreference>
            _preferenceRepository;

        private readonly IUnitOfWork
            _unitOfWork;

        public NotificationPreferenceService(
            IRepository<NotificationPreference> preferenceRepository,
            IUnitOfWork unitOfWork)
        {
            _preferenceRepository =
                preferenceRepository;

            _unitOfWork =
                unitOfWork;
        }

        public async Task<NotificationPreferenceDto> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var preferences =
                await _preferenceRepository.FindAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            var preference =
                preferences.FirstOrDefault();

            if (preference is null)
            {
                preference =
                    CreateDefault(userId);

                await _preferenceRepository.AddAsync(
                    preference,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }

            return Map(preference);
        }

        public async Task<NotificationPreferenceDto> UpdateAsync(
            Guid userId,
            UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            var preferences =
                await _preferenceRepository.FindAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            var preference =
                preferences.FirstOrDefault();

            if (preference is null)
            {
                preference =
                    CreateDefault(userId);

                await _preferenceRepository.AddAsync(
                    preference,
                    cancellationToken);
            }

            preference.InAppEnabled =
                request.InAppEnabled;

            preference.EmailEnabled =
                request.EmailEnabled;

            preference.EnvelopeSentEnabled =
                request.EnvelopeSentEnabled;

            preference.SigningReminderEnabled =
                request.SigningReminderEnabled;

            preference.EnvelopeCompletedEnabled =
                request.EnvelopeCompletedEnabled;

            preference.EnvelopeDeclinedEnabled =
                request.EnvelopeDeclinedEnabled;

            preference.EnvelopeVoidedEnabled =
                request.EnvelopeVoidedEnabled;

            preference.RecipientSignedEnabled =
                request.RecipientSignedEnabled;

            _preferenceRepository.Update(
                preference);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return Map(preference);
        }

        private static NotificationPreference CreateDefault(
            Guid userId)
        {
            return new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,

                InAppEnabled = true,
                EmailEnabled = true,

                EnvelopeSentEnabled = true,
                SigningReminderEnabled = true,
                EnvelopeCompletedEnabled = true,
                EnvelopeDeclinedEnabled = true,
                EnvelopeVoidedEnabled = true,
                RecipientSignedEnabled = true
            };
        }

        private static NotificationPreferenceDto Map(
            NotificationPreference preference)
        {
            return new NotificationPreferenceDto
            {
                InAppEnabled =
                    preference.InAppEnabled,

                EmailEnabled =
                    preference.EmailEnabled,

                EnvelopeSentEnabled =
                    preference.EnvelopeSentEnabled,

                SigningReminderEnabled =
                    preference.SigningReminderEnabled,

                EnvelopeCompletedEnabled =
                    preference.EnvelopeCompletedEnabled,

                EnvelopeDeclinedEnabled =
                    preference.EnvelopeDeclinedEnabled,

                EnvelopeVoidedEnabled =
                    preference.EnvelopeVoidedEnabled,

                RecipientSignedEnabled =
                    preference.RecipientSignedEnabled
            };
        }
    }
}