using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Domain.Entities.Envelopes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Mapping
{
    public static class FinalPdfRenderFieldMapper
    {
        public static List<FinalPdfRenderField> Map(
            IEnumerable<Fields> fields,
            IEnumerable<EnvelopeFieldValue> values,
            IEnumerable<EnvelopeAppliedSignature> appliedMarks)
        {
            var valuesByFieldId =
                values
                    .GroupBy(x => x.EnvelopeFieldId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Last());

            var marksByFieldId =
                appliedMarks
                    .GroupBy(x => x.EnvelopeFieldId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Last());

            var result =
                new List<FinalPdfRenderField>();

            foreach (var field in fields)
            {
                valuesByFieldId.TryGetValue(
                    field.Id,
                    out var value);

                marksByFieldId.TryGetValue(
                    field.Id,
                    out var mark);

                result.Add(
                    new FinalPdfRenderField
                    {
                        FieldId =
                            field.Id,

                        PageNumber =
                            field.PageNumber,

                        X =
                            field.X,

                        Y =
                            field.Y,

                        Width =
                            field.Width,

                        Height =
                            field.Height,

                        FieldType =
                            field.FieldType,

                        ValueText =
                            value?.ValueText,

                        ValueNumber =
                            value?.ValueNumber,

                        ValueDate =
                            value?.ValueDate,

                        ValueBoolean =
                            value?.ValueBoolean,

                        AppliedBlobPath =
                            mark?.AppliedBlobPath,

                        AppliedContentType =
                            mark?.AppliedContentType,

                        SignatureTypeSnapshot =
                            mark?.SignatureTypeSnapshot,

                        TypedTextSnapshot =
                            mark?.TypedTextSnapshot,

                        FontKeySnapshot =
                            mark?.FontKeySnapshot
                    });
            }

            return result;
        }
    }
}
