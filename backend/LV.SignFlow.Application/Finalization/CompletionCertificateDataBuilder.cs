using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Organizations;
using LV.SignFlow.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Finalization
{
    public sealed class CompletionCertificateDataBuilder
        : ICompletionCertificateDataBuilder
    {
        private readonly IRepository<Recipients>
            _recipientRepository;

        private readonly IRepository<User>
            _userRepository;

        private readonly IRepository<Organization>
            _organizationRepository;

        public CompletionCertificateDataBuilder(
            IRepository<Recipients> recipientRepository,
            IRepository<User> userRepository,
            IRepository<Organization> organizationRepository)
        {
            _recipientRepository =
                recipientRepository;

            _userRepository =
                userRepository;

            _organizationRepository =
                organizationRepository;
        }

        public async Task<CompletionCertificateData> BuildAsync(
            Envelope envelope,
            IReadOnlyCollection<Documents> documents,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                envelope);

            if (
                envelope.Status !=
                Domain.Enums.EnvelopeEnums.EnvelopeStatus.Completed ||
                !envelope.CompletedAt.HasValue)
            {
                throw new InvalidOperationException(
                    "Certificate can only be generated for a completed envelope.");
            }

            var recipients =
                (
                    await _recipientRepository.FindAsync(
                        x =>
                            x.EnvelopeId ==
                            envelope.Id,
                        cancellationToken)
                )
                .OrderBy(x =>
                    x.RoutingOrder)
                .ToList();

            var sender =
                await _userRepository.GetByIdAsync(
                    envelope.CreatedByUserId,
                    cancellationToken);

            var organization =
                await _organizationRepository.GetByIdAsync(
                    envelope.OrganizationId,
                    cancellationToken);

            if (sender is null)
            {
                throw new InvalidOperationException(
                    "Envelope sender could not be found.");
            }

            if (organization is null)
            {
                throw new InvalidOperationException(
                    "Envelope organization could not be found.");
            }

            var documentData =
                documents
                    .OrderBy(x => x.Id)
                    .Select(x =>
                    {
                        if (
                            string.IsNullOrWhiteSpace(
                                x.FinalHash) ||
                            !x.FinalizedAt.HasValue)
                        {
                            throw new InvalidOperationException(
                                $"Document '{x.Id}' has not been finalized.");
                        }

                        return new CompletionCertificateDocumentData
                        {
                            DocumentId =
                                x.Id,

                            FileName =
                                x.OriginalFileName,

                            FinalHash =
                                x.FinalHash,

                            FinalizedAt =
                                x.FinalizedAt.Value
                        };
                    })
                    .ToList();

            var recipientData =
                recipients
                    .Select(x =>
                        new CompletionCertificateRecipientData
                        {
                            RecipientId =
                                x.Id,

                            Name =
                                x.Name,

                            Email =
                                x.Email,

                            RecipientType =
                                x.RecipientType
                                    .ToString(),

                            RoutingOrder =
                                x.RoutingOrder,

                            Status =
                                x.Status
                                    .ToString(),

                            CompletedAt =
                                x.CompletedAt
                        })
                    .ToList();

            /*
             * Audit timeline-i mövcud,
             * etibarlı lifecycle timestamps-dan qururuq.
             *
             * Events entity-nin internal schema-sına
             * generator-u bağlamırıq.
             */
            var events =
                new List<CompletionCertificateEventData>
                {
                    new()
                    {
                        OccurredAt =
                            envelope.CreatedAt,

                        EventType =
                            "Envelope Created",

                        Description =
                            "The envelope was created."
                    }
                };

            if (envelope.SentAt.HasValue)
            {
                events.Add(
                    new CompletionCertificateEventData
                    {
                        OccurredAt =
                            envelope.SentAt.Value,

                        EventType =
                            "Envelope Sent",

                        Description =
                            "The envelope was sent to recipients."
                    });
            }

            foreach (
                var recipient in
                recipients
                    .Where(x =>
                        x.CompletedAt.HasValue)
                    .OrderBy(x =>
                        x.CompletedAt))
            {
                events.Add(
                    new CompletionCertificateEventData
                    {
                        OccurredAt =
                            recipient.CompletedAt!.Value,

                        EventType =
                            "Recipient Completed",

                        Description =
                            $"{recipient.Name} ({recipient.Email}) completed their required action.",

                        RecipientId =
                            recipient.Id
                    });
            }

            events.Add(
                new CompletionCertificateEventData
                {
                    OccurredAt =
                        envelope.CompletedAt.Value,

                    EventType =
                        "Envelope Completed",

                    Description =
                        "All required recipient actions were completed and the envelope reached Completed status."
                });

            var senderName =
                string.Join(
                    " ",
                    new[]
                    {
                        sender.DisplayName,
                        sender.Email
                    }
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x)));

            return new CompletionCertificateData
            {
                EnvelopeId =
                    envelope.Id,

                Subject =
                    envelope.Subject,

                OrganizationName =
                    organization.Name,

                SenderName =
                    senderName,

                SenderEmail =
                    sender.Email ?? string.Empty,

                CreatedAt =
                    envelope.CreatedAt,

                SentAt =
                    envelope.SentAt,

                CompletedAt =
                    envelope.CompletedAt.Value,

                Recipients =
                    recipientData,

                Documents =
                    documentData,

                Events =
                    events
            };
        }
    }
}
