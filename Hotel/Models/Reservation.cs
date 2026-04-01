using Hotel.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Име на гост")]
        public string GuestName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Настаняване")]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Напускане")]
        public DateTime CheckOut { get; set; }

        [Range(1, 20)]
        [Display(Name = "Брой гости")]
        public int GuestsCount { get; set; }

        [Display(Name = "Платено")]
        public bool IsPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Обща сума")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Статус")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [Display(Name = "Стая")]
        public int RoomId { get; set; }

        public Room? Room { get; set; }
    }
}