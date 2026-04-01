using Hotel.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Номер на стая")]
        public string Number { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Тип стая")]
        public RoomType Type { get; set; }

        [Range(1, 20)]
        [Display(Name = "Капацитет")]
        public int Capacity { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Цена на нощувка")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "Статус")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        public ICollection<Reservation>? Reservations { get; set; }
    }
}