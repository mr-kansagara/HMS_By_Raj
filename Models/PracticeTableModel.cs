using System.ComponentModel.DataAnnotations;

namespace HMS.Models
{
    public class PracticeTableModel
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }

        public Guid ImageId { get; set; }
        
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public string IsActive { get; set; }
        public DateTime? AddedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public Int64 HospitalId { get; set; }


    }
}
