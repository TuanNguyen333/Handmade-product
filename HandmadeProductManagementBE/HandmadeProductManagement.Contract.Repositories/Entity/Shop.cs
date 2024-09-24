﻿using HandmadeProductManagement.Core.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandmadeProductManagement.Contract.Repositories.Entity
{
    public class Shop : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Rating { get; set; }
        public string UserId { get; set; } = string.Empty;

        //[ForeignKey("UserId")]
        //public virtual User User { get; set; }
    }
}