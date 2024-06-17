﻿namespace HMS.Models
{
    public class Hospital : EntityBaseWithoutHospital
    {
        public Int64 Id { get; set; }
        public string HospitalName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public Int64 ImageId { get; set; }
    }
}
