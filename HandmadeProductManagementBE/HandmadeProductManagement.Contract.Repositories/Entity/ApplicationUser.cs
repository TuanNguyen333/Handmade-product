﻿using Microsoft.AspNetCore.Identity;
using HandmadeProductManagement.Contract.Repositories.Entity;
using HandmadeProductManagement.Core.Utils;

namespace HandmadeProductManagement.Repositories.Entity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string Password {  get; set; } = string.Empty;
        public virtual UserInfo? UserInfo { get; set; }
        public string? CreatedBy { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public DateTimeOffset? DeletedTime { get; set; }
        public ApplicationUser()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
        public Shop? Shop { get; set; }
        public ICollection<Order> Orders { get; set; } = [];
    }
}
