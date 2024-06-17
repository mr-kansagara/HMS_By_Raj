using System;
using System.ComponentModel.DataAnnotations;

namespace HMS.Models.PatientInfoViewModel
{
    public class PatientInfoEditViewModel : EntityBase
    {
        [Display(Name = "SL")]
        [Required]
        public Int64? Id { get; set; }
        [Display(Name = "Application User Id")]
        public string? ApplicationUserId { get; set; }
        public string? Gender { get; set; }
        [Display(Name = "Spouse Name")]
        public string? SpouseName { get; set; }
        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }
        [Display(Name = "Father Name")]
        public string? FatherName { get; set; }
        [Display(Name = "Mother Name")]
        public string? MotherName { get; set; }
        [Display(Name = "Registration Fee")]
        public double? RegistrationFee { get; set; }
        public string? MaritalStatus { get; set; }
        public bool? Agreement { get; set; }
        public string? Remarks { get; set; }
        public string? PatientCode { get; private set; }

        public static implicit operator PatientInfoEditViewModel(PatientInfo _PatientInfo)
        {
            return new PatientInfoEditViewModel
            {
                Id = (long)_PatientInfo.Id,
                ApplicationUserId = _PatientInfo.ApplicationUserId,
                PatientCode = _PatientInfo.PatientCode,
                HospitalId = _PatientInfo.HospitalId,
                FatherName = _PatientInfo.FatherName,
                MotherName = _PatientInfo.MotherName,
                RegistrationFee = _PatientInfo.RegistrationFee,
                MaritalStatus = _PatientInfo.MaritalStatus,
                Agreement = _PatientInfo.Agreement,
                Remarks = _PatientInfo.Remarks,
                SpouseName = _PatientInfo.SpouseName,
                Gender = _PatientInfo.Gender,
                BloodGroup = _PatientInfo.BloodGroup,

                CreatedDate = _PatientInfo.CreatedDate,
                ModifiedDate = _PatientInfo.ModifiedDate,
                CreatedBy = _PatientInfo.CreatedBy,
                ModifiedBy = _PatientInfo.ModifiedBy,
            };
        }

        public static implicit operator PatientInfo(PatientInfoEditViewModel vm)
        {
            return new PatientInfo
            {                
                Id = (long)vm.Id,
                ApplicationUserId = vm.ApplicationUserId,
                PatientCode = vm.PatientCode,
                HospitalId = vm.HospitalId,
                FatherName = vm.FatherName,
                MotherName = vm.MotherName,
                RegistrationFee = vm.RegistrationFee,
                MaritalStatus = vm.MaritalStatus,
                Agreement = vm.Agreement,
                Remarks = vm.Remarks, 
                SpouseName = vm.SpouseName,
                Gender = vm.Gender,
                BloodGroup = vm.BloodGroup,

                CreatedDate = vm.CreatedDate,
                ModifiedDate = vm.ModifiedDate,
                CreatedBy = vm.CreatedBy,
                ModifiedBy = vm.ModifiedBy,
            };
        }
    }
}
