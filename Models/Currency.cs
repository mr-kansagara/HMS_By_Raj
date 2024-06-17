﻿using System;

namespace HMS.Models
{
    public class Currency : EntityBase
    {
        public Int64 Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Symbol { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
    }
}
