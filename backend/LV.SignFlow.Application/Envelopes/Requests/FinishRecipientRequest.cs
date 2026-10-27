using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class FinishRecipientRequest
    {
        public List<FinishFieldValueRequest> Values { get; set; } = new();

        public List<FinishAppliedMarkRequest> AppliedMarks { get; set; } = new();
    }

    public class FinishFieldValueRequest
    {
        public Guid FieldId { get; set; }

        public string? ValueText { get; set; }

        public decimal? ValueNumber { get; set; }

        public bool? ValueBoolean { get; set; }
    }

    public class FinishAppliedMarkRequest
    {
        public Guid FieldId { get; set; }

        public Guid? SignatureId { get; set; }

        public Guid? StampId { get; set; }
    }
}
