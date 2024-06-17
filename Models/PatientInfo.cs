using HMS.Models.PatientInfoViewModel;
using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Models
{
    public class PatientInfo : EntityBase
    {
        public Int64 Id { get; set; }

        public string ApplicationUserId { get; set; }
        public string PatientCode { get; set; }

        [Display(Name = "Marital Status")]
        public string MaritalStatus { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Spouse Name")]
        public string SpouseName { get; set; }
        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; }
        [Display(Name = "Father Name")]
        public string FatherName { get; set; }
        [Display(Name = "Mother Name")]
        public string MotherName { get; set; }
        [Display(Name = "Registration Fee")]
        public double? RegistrationFee { get; set; }

        public string Remarks { get; set; }
        public bool? Agreement { get; set; }

        public static implicit operator PatientInfo(PatientInfoCRUDViewModel v)
        {
            throw new NotImplementedException();
        }
    }
}
