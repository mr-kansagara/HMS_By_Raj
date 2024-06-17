using HMS.Models.ManageUserRolesVM;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel.DataAnnotations;

namespace HMS.Models.PracticeTable
{
    public class PracticeTableCRUDModel
    {

        public Guid Id { get; set; }

        public Guid ImageId { get; set; }

        public IFormFile ProfileImage { get; set; }

        [Required(ErrorMessage ="Enter the valid title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Enter the valid Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Enter the valid Short Description")]
        public string ShortDescription { get; set; }

        public string IsActive { get; set; }

        public DateTime? AddedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string ProfilePath = "/upload/blank-person.png";
        //public string ProfilePath { get; set; }

        public long HospitalId { get; set; }

        public string Hospital { get; set; }


        public static implicit operator PracticeTableCRUDModel(PracticeTableModel vm)
        {
            return new PracticeTableCRUDModel
            {
                Id = vm.Id,
                //ProfileImage = vm.ProfileImage,
                Title = vm.Title,
                Description = vm.Description,
                ShortDescription = vm.ShortDescription,
                IsActive = vm.IsActive,
                AddedOn = vm.AddedOn,
                ModifiedOn = vm.ModifiedOn,
                ImageId = vm.ImageId,
                

               
            };
        }

        public static implicit operator PracticeTableModel(PracticeTableCRUDModel vm)
        {
            return new PracticeTableModel
            {
                Id = vm.Id,
                //ProfileImage = vm.ProfileImage,
                Title = vm.Title,
                Description = vm.Description,
                ShortDescription = vm.ShortDescription,
                IsActive = vm.IsActive,
                ImageId = vm.ImageId,
                ModifiedOn = vm.ModifiedOn,
                AddedOn = vm.AddedOn,
            };
        }
    }
}
