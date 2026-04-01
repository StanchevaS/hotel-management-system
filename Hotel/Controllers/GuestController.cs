using Hotel.Data;
using Hotel.Enums;
using Hotel.Models;
using Hotel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    public class GuestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public GuestController(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        [HttpGet]
        public async Task<IActionResult> Rooms(RoomType? typeFilter, int? guestsCount, decimal? maxPrice)
        {
            var query = _context.Rooms.AsQueryable();

            if (typeFilter.HasValue)
            {
                query = query.Where(r => r.Type == typeFilter.Value);
            }

            if (guestsCount.HasValue && guestsCount.Value > 0)
            {
                query = query.Where(r => r.Capacity >= guestsCount.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(r => r.PricePerNight <= maxPrice.Value);
            }

            query = query.Where(r => r.Status != RoomStatus.Maintenance);

            var rooms = await query
                .OrderBy(r => r.PricePerNight)
                .ThenBy(r => r.Number)
                .ToListAsync();

            ViewBag.TypeFilter = typeFilter;
            ViewBag.GuestsCount = guestsCount;
            ViewBag.MaxPrice = maxPrice;

            return View(rooms);
        }

        [HttpGet]
        public IActionResult Availability()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Availability(DateTime checkIn, DateTime checkOut, int guestsCount)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            if (checkOut <= checkIn)
            {
                ModelState.AddModelError(string.Empty, "Датата на напускане трябва да е след датата на настаняване.");
                return View();
            }

            var availableRooms = await _hotelService.GetAvailableRoomsAsync(checkIn, checkOut);

            if (guestsCount > 0)
            {
                availableRooms = availableRooms
                    .Where(r => r.Capacity >= guestsCount)
                    .ToList();
            }

            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.GuestsCount = guestsCount;
            ViewBag.HasSearched = true;

            return View(availableRooms);
        }

        [HttpGet]
        public IActionResult InquiryCreate()
        {
            var inquiry = new Inquiry
            {
                CheckIn = DateTime.Today,
                CheckOut = DateTime.Today.AddDays(1)
            };

            return View("~/Views/Inquiries/Create.cshtml", inquiry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InquiryCreate(Inquiry inquiry)
        {
            if (inquiry.CheckOut <= inquiry.CheckIn)
            {
                ModelState.AddModelError(string.Empty, "Датата на напускане трябва да е след датата на настаняване.");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Inquiries/Create.cshtml", inquiry);
            }

            inquiry.Status = "Ново";
            inquiry.CreatedOn = DateTime.Now;

            _context.Inquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Вашето запитване беше изпратено успешно.";
            return RedirectToAction(nameof(InquiryCreate));
        }
    }
}