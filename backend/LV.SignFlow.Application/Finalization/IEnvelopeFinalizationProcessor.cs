using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Finalization
{
    public interface IEnvelopeFinalizationProcessor
    {
        Task ProcessAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default);
    }
}
