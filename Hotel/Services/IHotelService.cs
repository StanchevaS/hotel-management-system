using Hotel.Models;

namespace Hotel.Services
{
    public interface IHotelService
    {
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
        Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);

        decimal CalculateTotalAmount(DateTime checkIn, DateTime checkOut, decimal pricePerNight);

        Task<bool> CheckInRoomAsync(int roomId);
        Task<bool> ReleaseRoomAsync(int roomId);
        Task<bool> CancelRoomReservationAsync(int roomId);

        Task RecalculateRoomStatusAsync(int roomId);
        Task RecalculateAllRoomStatusesAsync();
        Task AutoCompleteExpiredReservationsAsync();

        Task<decimal> CalculateRevenueForPeriodAsync(
            DateTime reservationCheckIn,
            DateTime reservationCheckOut,
            decimal totalAmount,
            DateTime periodStart,
            DateTime periodEnd);
    }
}