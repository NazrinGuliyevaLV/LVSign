using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public class TemplateFieldRequest
    {
        public Guid TemplateDocumentId { get; set; }

        public Guid? RecipientRoleId { get; set; }

        public FieldType FieldType { get; set; }

        public int PageNumber { get; set; }

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public bool IsRequired { get; set; } = true;
        public FieldOwnerType OwnerType { get; set; }
    = FieldOwnerType.Recipient;
    }
}
