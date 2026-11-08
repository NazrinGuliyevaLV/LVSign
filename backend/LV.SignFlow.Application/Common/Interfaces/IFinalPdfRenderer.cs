using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Domain.Entities.Envelopes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Interfaces
{
    public interface IFinalPdfRenderer
    {
        //    Task<byte[]> RenderAsync(
        //byte[] originalPdf,
        //IReadOnlyList<Fields> fields,
        //IReadOnlyList<EnvelopeFieldValue> values,
        //IReadOnlyList<EnvelopeAppliedSignature> appliedMarks,
        //CancellationToken cancellationToken = default);

        Task<byte[]> RenderAsync(
       Stream originalPdf,
       IReadOnlyCollection<FinalPdfRenderField> fields,
       CancellationToken cancellationToken = default);
    }
}
