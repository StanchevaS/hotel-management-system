using Hotel.Enums;

namespace Hotel.Helpers
{
    public static class ReservationUiHelper
    {
        public static string GetReservationStatusText(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Чакаща",
                ReservationStatus.Confirmed => "Потвърдена",
                ReservationStatus.CheckedIn => "Настанен",
                ReservationStatus.CheckedOut => "Напуснал",
                ReservationStatus.Cancelled => "Отказана",
                _ => status.ToString()
            };
        }
    }
}