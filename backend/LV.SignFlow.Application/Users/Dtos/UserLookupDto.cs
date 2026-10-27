using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Users.Dtos
{
    public class UserLookupDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }= string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? JobTitle { get; set; }
    }
}
