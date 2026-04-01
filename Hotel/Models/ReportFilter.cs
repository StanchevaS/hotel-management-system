using System.ComponentModel.DataAnnotations;

namespace Hotel.Models
{
    public class ReportFilter
    {
        [DataType(DataType.Date)]
        [Display(Name = "От дата")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "До дата")]
        public DateTime? EndDate { get; set; }
    }
}