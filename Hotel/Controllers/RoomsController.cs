using Hotel.Data;
using Hotel.Enums;
using Hotel.Helpers;
using Hotel.Models;
using Hotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    [Authorize(Roles = "Administrator,Receptionist")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public RoomsController(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var rooms = await _context.Rooms
                .AsNoTracking()
                .OrderBy(r => r.Number)
                .ToListAsync();

            return View(rooms);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public IActionResult Create()
        {
            var room = new Room
            {
                Status = RoomStatus.Available,
                Type = RoomType.Standard
            };

            LoadRoomLists(room.Type, room.Status);
            return View(room);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            room.Number = (room.Number ?? string.Empty).Trim();

            room.Capacity = RoomUiHelper.GetDefaultCapacity(room.Type);
            room.PricePerNight = RoomUiHelper.GetDefaultPrice(room.Type);

            ModelState.Remove(nameof(Room.Capacity));
            ModelState.Remove(nameof(Room.PricePerNight));

            if (string.IsNullOrWhiteSpace(room.Number))
            {
                ModelState.AddModelError(nameof(room.Number), "Номерът на стаята е задължителен.");
            }

            bool duplicateExists = await _context.Rooms
                .AnyAsync(r => r.Number.ToLower() == room.Number.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(room.Number), "Вече съществува стая с този номер.");
            }

            if (!ModelState.IsValid)
            {
                LoadRoomLists(room.Type, room.Status);
                return View(room);
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Стаята е създадена успешно.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            LoadRoomLists(room.Type, room.Status);
            return View(room);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Room room)
        {
            room.Number = (room.Number ?? string.Empty).Trim();

            room.Capacity = RoomUiHelper.GetDefaultCapacity(room.Type);
            room.PricePerNight = RoomUiHelper.GetDefaultPrice(room.Type);

            ModelState.Remove(nameof(Room.Capacity));
            ModelState.Remove(nameof(Room.PricePerNight));

            if (string.IsNullOrWhiteSpace(room.Number))
            {
                ModelState.AddModelError(nameof(room.Number), "Номерът на стаята е задължителен.");
            }

            bool duplicateExists = await _context.Rooms
                .AnyAsync(r => r.Id != room.Id &&
                               r.Number.ToLower() == room.Number.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(room.Number), "Вече съществува друга стая с този номер.");
            }

            if (!ModelState.IsValid)
            {
                LoadRoomLists(room.Type, room.Status);
                return View(room);
            }

            var existingRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == room.Id);
            if (existingRoom == null)
            {
                return NotFound();
            }

            existingRoom.Number = room.Number;
            existingRoom.Type = room.Type;
            existingRoom.Status = room.Status;
            existingRoom.Capacity = room.Capacity;
            existingRoom.PricePerNight = room.PricePerNight;

            await _context.SaveChangesAsync();

            if (existingRoom.Status != RoomStatus.Maintenance)
            {
                await _hotelService.RecalculateRoomStatusAsync(existingRoom.Id);
            }

            TempData["SuccessMessage"] = "Стаята е редактирана успешно.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var hasReservations = await _context.Reservations.AnyAsync(r => r.RoomId == id);
            if (hasReservations)
            {
                TempData["ErrorMessage"] = "Стаята не може да бъде изтрита, защото има свързани резервации.";
                return RedirectToAction(nameof(Index));
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Стаята е изтрита успешно.";
            return RedirectToAction(nameof(Index));
        }

        private void LoadRoomLists(RoomType? selectedType = null, RoomStatus? selectedStatus = null)
        {
            ViewBag.RoomTypes = EnumSelectListHelper.CreateSelectList<RoomType>(
                RoomUiHelper.GetRoomTypeText,
                selectedType);

            ViewBag.RoomStatuses = EnumSelectListHelper.CreateSelectList<RoomStatus>(
                RoomUiHelper.GetRoomStatusText,
                selectedStatus);
        }
    }
}