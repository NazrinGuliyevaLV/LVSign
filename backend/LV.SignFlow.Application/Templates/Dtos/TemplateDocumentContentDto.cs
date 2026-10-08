using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateDocumentContentDto
    {
        public Stream Stream { get; set; } = Stream.Null;

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = "application/pdf";
    }
}
