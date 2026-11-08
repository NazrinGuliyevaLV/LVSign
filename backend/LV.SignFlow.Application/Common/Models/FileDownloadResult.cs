using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Models
{
    public class FileDownloadResult
    {
        public Stream Content { get; set; } = Stream.Null;

        public string ContentType { get; set; } =
            "application/octet-stream";

        public string FileName { get; set; } =
            string.Empty;
    }
}
