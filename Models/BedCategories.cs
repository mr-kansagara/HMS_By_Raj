﻿using System;

namespace HMS.Models
{
    public class BedCategories : EntityBase
    {
        public Int64 Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double? BedPrice { get; set; }
        public double? OldBedPrice { get; set; }
    }
}
