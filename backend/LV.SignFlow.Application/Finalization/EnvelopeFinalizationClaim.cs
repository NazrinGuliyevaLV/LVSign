using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Finalization
{
    public sealed record EnvelopeFinalizationClaim(
         Guid FinalizationId,
         Guid EnvelopeId,
         Guid LeaseId,
         DateTimeOffset LeaseUntil);
}
