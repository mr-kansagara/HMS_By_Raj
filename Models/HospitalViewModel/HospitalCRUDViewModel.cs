using System.ComponentModel.DataAnnotations;

namespace HMS.Models.HospitalViewModel
{
    public class HospitalCRUDViewModel : EntityBase
    {
        public Int64 Id { get; set; }
        [Display(Name = "Hospital Name"), Required]
        public string HospitalName { get; set; }
        [Display(Name = "Description"), Required]
        public string Description { get; set; }
        [Display(Name = "Address"), Required]
        public string Address { get; set; }
        public Int64? ImageId { get; set; }
        [Display(Name = "Profile Picture")]
        public IFormFile ProfilePictureDetails { get; set; }
        public string ProfilePicture { get; set; } = "/upload/blank-person.png";

        public static implicit operator HospitalCRUDViewModel(Hospital _hospital)
        {
            return new HospitalCRUDViewModel
            {
                Id = _hospital.Id,
                HospitalName = _hospital.HospitalName,
                Description = _hospital.Description,
                Address = _hospital.Address,
                CreatedDate = _hospital.CreatedDate,
                ModifiedDate = _hospital.ModifiedDate,
                CreatedBy = _hospital.CreatedBy,
                ModifiedBy = _hospital.ModifiedBy,
                //HospitalLogoImagePath = _hospital.HospitalLogoImagePath
                ImageId = _hospital.ImageId,
            };
        }

        public static implicit operator Hospital(HospitalCRUDViewModel vm)
        {
            return new Hospital
            {
                Id = vm.Id,
                HospitalName = vm.HospitalName,
                Description = vm.Description,
                Address = vm.Address,
                CreatedDate = vm.CreatedDate,
                ModifiedDate = vm.ModifiedDate,
                CreatedBy = vm.CreatedBy,
                ModifiedBy = vm.ModifiedBy,
                Cancelled = vm.Cancelled,
                //HospitalLogoImagePath = vm.HospitalLogoImagePath
                ImageId= (long)vm.ImageId
            };
        }
    }
}
