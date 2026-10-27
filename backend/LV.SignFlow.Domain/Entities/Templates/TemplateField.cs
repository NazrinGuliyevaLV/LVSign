using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Templates
{
    public class TemplateField
    {
        public Guid Id { get; set; }

        public Guid TemplateDocumentId { get; set; }

        public Guid? RecipientRoleId { get; set; }

        public FieldType FieldType { get; set; }

        public int PageNumber { get; set; }

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public bool IsRequired { get; set; } = true;


        public TemplateDocument TemplateDocument { get; set; } = null!;

        public TemplateRecipientRole? RecipientRole { get; set; } = null!;
        public FieldOwnerType OwnerType { get; set; }= FieldOwnerType.Recipient;
        public ICollection<TemplateRoutingCondition> TemplateRoutingConditions { get; set; }
    = new List<TemplateRoutingCondition>();
    }
}
