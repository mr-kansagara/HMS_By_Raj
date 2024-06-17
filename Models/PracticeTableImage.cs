using NuGet.Protocol;
using System.ComponentModel.DataAnnotations;

namespace HMS.Models
{
    public class PracticeTableImage
    {
        [Key]
        public Guid ImageId {  get; set; }

        public string PictureName   { get; set; }

        public string ImagePath { get; set; }

    }
}
    