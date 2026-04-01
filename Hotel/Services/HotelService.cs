using Hotel.Data;
using Hotel.Enums;
using Hotel.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Services
{
    public class HotelService : IHotelService
    {
        private readonly ApplicationDbContext _context;

        public HotelService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsRoomAvailableAsync(
            int roomId,
            DateTime checkIn,
            DateTime checkOut,
            int? excludeReservationId = null)
        {
            checkIn = checkIn.Date;
            checkOut = checkOut.Date;

            if (checkOut <= checkIn)
            {
                return false;
            }

            var query = _context.Reservations.Where(r =>
                r.RoomId == roomId &&
                checkIn < r.CheckOut &&
                checkOut > r.CheckIn &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.CheckedOut);

            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservationId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<List<Room>> GetAvailableRoomsAsync(
            DateTime checkIn,
            DateTime checkOut,
            int? excludeReservationId = null)
        {
            checkIn = checkIn.Date;
            checkOut = checkOut.Date;

            if (checkOut <= checkIn)
            {
                return new List<Room>();
            }

            var busyReservations = _context.Reservations.Where(r =>
                checkIn < r.CheckOut &&
                checkOut > r.CheckIn &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.CheckedOut);

            if (excludeReservationId.HasValue)
            {
                busyReservations = busyReservations.Where(r => r.Id != excludeReservationId.Value);
            }

            var busyRoomIds = await busyReservations
                .Select(r => r.RoomId)
                .Distinct()
                .ToListAsync();

            return await _context.Rooms
                .Where(r =>
                    !busyRoomIds.Contains(r.Id) &&
                    r.Status != RoomStatus.Maintenance)
                .OrderBy(r => r.Number)
                .ToListAsync();
        }

        public decimal CalculateTotalAmount(DateTime checkIn, DateTime checkOut, decimal pricePerNight)
        {
            var nights = (checkOut.Date - checkIn.Date).Days;

            if (nights <= 0)
            {
                return 0m;
            }

            return nights * pricePerNight;
        }

        public async Task<bool> CheckInRoomAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return false;
            }

            var today = DateTime.Today;

            var reservation = await _context.Reservations
                .Where(r =>
                    r.RoomId == roomId &&
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                    r.CheckIn.Date <= today &&
                    r.CheckOut.Date > today)
                .OrderBy(r => r.CheckIn)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.CheckedIn;
            await _context.SaveChangesAsync();

            await RecalculateRoomStatusAsync(roomId);
            return true;
        }

        public async Task<bool> ReleaseRoomAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return false;
            }

            var reservation = await _context.Reservations
                .Where(r => r.RoomId == roomId && r.Status == ReservationStatus.CheckedIn)
                .OrderByDescending(r => r.CheckIn)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.CheckedOut;
            await _context.SaveChangesAsync();

            await RecalculateRoomStatusAsync(roomId);
            return true;
        }

        public async Task<bool> CancelRoomReservationAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return false;
            }

            var today = DateTime.Today;

            var reservation = await _context.Reservations
                .Where(r =>
                    r.RoomId == roomId &&
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                    r.CheckOut.Date > today)
                .OrderBy(r => r.CheckIn)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                var checkedInReservation = await _context.Reservations
                    .Where(r =>
                        r.RoomId == roomId &&
                        r.Status == ReservationStatus.CheckedIn &&
                        r.CheckIn.Date <= today &&
                        r.CheckOut.Date > today)
                    .OrderByDescending(r => r.CheckIn)
                    .FirstOrDefaultAsync();

                if (checkedInReservation == null)
                {
                    return false;
                }

                checkedInReservation.Status = ReservationStatus.Cancelled;
                await _context.SaveChangesAsync();

                await RecalculateRoomStatusAsync(roomId);
                return true;
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            await RecalculateRoomStatusAsync(roomId);
            return true;
        }

        public async Task AutoCompleteExpiredReservationsAsync()
        {
            var today = DateTime.Today;

            var expiredCheckedIn = await _context.Reservations
                .Where(r =>
                    r.Status == ReservationStatus.CheckedIn &&
                    r.CheckOut.Date <= today)
                .ToListAsync();

            foreach (var reservation in expiredCheckedIn)
            {
                reservation.Status = ReservationStatus.CheckedOut;
            }

            var expiredPendingOrConfirmed = await _context.Reservations
                .Where(r =>
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                    r.CheckOut.Date <= today)
                .ToListAsync();

            foreach (var reservation in expiredPendingOrConfirmed)
            {
                reservation.Status = ReservationStatus.CheckedOut;
            }

            if (expiredCheckedIn.Count > 0 || expiredPendingOrConfirmed.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task RecalculateRoomStatusAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return;
            }

            if (room.Status == RoomStatus.Maintenance || room.Status == RoomStatus.Cleaning)
            {
                return;
            }

            var today = DateTime.Today;

            var reservations = await _context.Reservations
                .Where(r =>
                    r.RoomId == roomId &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.Status != ReservationStatus.CheckedOut)
                .OrderBy(r => r.CheckIn)
                .ToListAsync();

            var activeCheckedIn = reservations.FirstOrDefault(r =>
                r.Status == ReservationStatus.CheckedIn &&
                r.CheckIn.Date <= today &&
                r.CheckOut.Date > today);

            if (activeCheckedIn != null)
            {
                room.Status = RoomStatus.Occupied;
                await _context.SaveChangesAsync();
                return;
            }

            var hasUpcomingReservation = reservations.Any(r =>
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                r.CheckOut.Date > today);

            room.Status = hasUpcomingReservation
                ? RoomStatus.Reserved
                : RoomStatus.Available;

            await _context.SaveChangesAsync();
        }

        public async Task RecalculateAllRoomStatusesAsync()
        {
            var roomIds = await _context.Rooms
                .Select(r => r.Id)
                .ToListAsync();

            foreach (var roomId in roomIds)
            {
                await RecalculateRoomStatusAsync(roomId);
            }
        }

        public Task<decimal> CalculateRevenueForPeriodAsync(
            DateTime reservationCheckIn,
            DateTime reservationCheckOut,
            decimal totalAmount,
            DateTime periodStart,
            DateTime periodEnd)
        {
            var reservationStart = reservationCheckIn.Date;
            var reservationEnd = reservationCheckOut.Date;
            var reportStart = periodStart.Date;
            var reportEndExclusive = periodEnd.Date;

            var overlapStart = reservationStart > reportStart ? reservationStart : reportStart;
            var overlapEnd = reservationEnd < reportEndExclusive ? reservationEnd : reportEndExclusive;

            var totalNights = (reservationEnd - reservationStart).Days;
            var overlapNights = (overlapEnd - overlapStart).Days;

            if (totalNights <= 0 || overlapNights <= 0)
            {
                return Task.FromResult(0m);
            }

            var nightlyRate = totalAmount / totalNights;
            var periodRevenue = nightlyRate * overlapNights;

            return Task.FromResult(periodRevenue);
        }
    }
}