using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Storage
{
    public sealed class StorageOptions
    {
        public const string SectionName = "Storage";

        public string Provider { get; set; } =
            "Local";

        public string? LocalRootPath { get; set; }

        public string? ServiceUri { get; set; }

        public string ContainerName { get; set; } =
            "lv-signflow";

        public string? ManagedIdentityClientId { get; set; }
    }
}
