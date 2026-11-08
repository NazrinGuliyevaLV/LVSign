using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Policies
{
    public static class EnvelopeStatusTransitionPolicy
    {
        public static bool CanTransition(
            EnvelopeStatus current,
            EnvelopeStatus next)
        {
            if (current == next)
            {
                return true;
            }

            return current switch
            {
                EnvelopeStatus.Draft =>
                    next == EnvelopeStatus.Sent,

                EnvelopeStatus.Sent =>
                    next is
                        EnvelopeStatus.InProgress or
                        EnvelopeStatus.Completed or
                        EnvelopeStatus.Declined or
                        EnvelopeStatus.Voided or
                        EnvelopeStatus.Expired,

                EnvelopeStatus.InProgress =>
                    next is
                        EnvelopeStatus.Completed or
                        EnvelopeStatus.Declined or
                        EnvelopeStatus.Voided or
                        EnvelopeStatus.Expired,

                /*
                 * Terminal states.
                 */
                EnvelopeStatus.Completed =>
                    false,

                EnvelopeStatus.Declined =>
                    false,

                EnvelopeStatus.Voided =>
                    false,

                EnvelopeStatus.Expired =>
                    false,

                _ =>
                    false
            };
        }

        public static void EnsureCanTransition(
            EnvelopeStatus current,
            EnvelopeStatus next)
        {
            if (CanTransition(
                    current,
                    next))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Envelope status cannot transition from '{current}' to '{next}'.");
        }

        public static bool IsTerminal(
            EnvelopeStatus status)
        {
            return status is
                EnvelopeStatus.Completed or
                EnvelopeStatus.Declined or
                EnvelopeStatus.Voided or
                EnvelopeStatus.Expired;
        }

        public static bool CanEdit(
            EnvelopeStatus status)
        {
            return status ==
                EnvelopeStatus.Draft;
        }

        public static bool CanSign(
            EnvelopeStatus status)
        {
            return status is
                EnvelopeStatus.Sent or
                EnvelopeStatus.InProgress;
        }

        public static bool CanVoid(
            EnvelopeStatus status)
        {
            return status is
                EnvelopeStatus.Sent or
                EnvelopeStatus.InProgress;
        }

        public static bool CanDelete(
    EnvelopeStatus status)
        {
            return status ==
                EnvelopeStatus.Draft;
        }

        public static bool CanExpire(
            EnvelopeStatus status)
        {
            return status is
                EnvelopeStatus.Sent or
                EnvelopeStatus.InProgress;
        }
    }
}
