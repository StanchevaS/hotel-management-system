using Hotel.Data;
using Hotel.Enums;
using Hotel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public HomeController(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        public async Task<IActionResult> Index()
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.TotalRooms = await _context.Rooms.CountAsync();

                ViewBag.ActiveReservations = await _context.Reservations
                    .CountAsync(r =>
                        r.Status != ReservationStatus.Cancelled &&
                        r.Status != ReservationStatus.CheckedOut);

                ViewBag.FreeRooms = await _context.Rooms
                    .CountAsync(r => r.Status == RoomStatus.Available);

                ViewBag.OccupiedRooms = await _context.Rooms
                    .CountAsync(r =>
                        r.Status == RoomStatus.Occupied ||
                        r.Status == RoomStatus.Reserved);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult About()
        {
            return View("~/Views/Home/About.cshtml");
        }

        public IActionResult Contacts()
        {
            return View("~/Views/Home/Contacts.cshtml");
        }

        public IActionResult Rules()
        {
            return View("~/Views/Home/Rules.cshtml");
        }

        public IActionResult Accommodation()
        {
            return View("~/Views/Home/Accommodation.cshtml");
        }
    }
}