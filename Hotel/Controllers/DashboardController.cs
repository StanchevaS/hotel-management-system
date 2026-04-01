using Hotel.Data;
using Hotel.Enums;
using Hotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    [Authorize(Roles = "Administrator,Receptionist")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public DashboardController(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        public async Task<IActionResult> Today()
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var today = DateTime.Today;

            ViewBag.TodayArrivals = await _context.Reservations
                .Include(r => r.Room)
                .Where(r =>
                    r.CheckIn.Date == today &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.Status != ReservationStatus.CheckedOut)
                .OrderBy(r => r.Room != null ? r.Room.Number : "")
                .ToListAsync();

            ViewBag.TodayDepartures = await _context.Reservations
                .Include(r => r.Room)
                .Where(r =>
                    r.CheckOut.Date == today &&
                    r.Status != ReservationStatus.Cancelled)
                .OrderBy(r => r.Room != null ? r.Room.Number : "")
                .ToListAsync();

            ViewBag.OccupiedRooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Occupied)
                .OrderBy(r => r.Number)
                .ToListAsync();

            ViewBag.ReservedRooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Reserved)
                .OrderBy(r => r.Number)
                .ToListAsync();

            return View();
        }
    }
}