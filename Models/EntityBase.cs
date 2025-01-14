﻿using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Models
{
    public class EntityBase
    {
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool Cancelled { get; set; }
        public Int64 HospitalId { get; set; }
    }
}
