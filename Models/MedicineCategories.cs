﻿using System;

namespace HMS.Models
{
    public class MedicineCategories : EntityBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
