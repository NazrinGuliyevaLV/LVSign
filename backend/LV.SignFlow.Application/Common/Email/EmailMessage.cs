using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Email
{
    public sealed class EmailMessage
    {
        public string To { get; init; } = null!;

        public string Subject { get; init; } = null!;

        public string Body { get; init; } = null!;

        public bool IsHtml { get; init; } = true;
    }
}
