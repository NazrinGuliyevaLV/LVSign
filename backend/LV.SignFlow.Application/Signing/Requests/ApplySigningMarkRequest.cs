using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Requests
{
    public class ApplySigningMarkRequest
    {
        public AppliedMarkKind Kind { get; set; }

        public Guid? SourceSignatureId { get; set; }

        public Guid? SourceStampId { get; set; }
    }
}
