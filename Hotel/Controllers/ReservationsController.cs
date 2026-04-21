using Hotel.Data;
using Hotel.Enums;
using Hotel.Helpers;
using Hotel.Models;
using Hotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    [Authorize(Roles = "Administrator,Receptionist")]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public ReservationsController(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, DateTime? checkIn, DateTime? checkOut, int? roomId, string? sortOrder)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.RoomId = roomId;
            ViewBag.SortOrder = sortOrder;

            var rooms = await _context.Rooms.OrderBy(r => r.Number).ToListAsync();
            ViewBag.RoomList = new SelectList(rooms, "Id", "Number", roomId);

            var query = _context.Reservations
                .Include(r => r.Room)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(r =>
                    r.GuestName.Contains(term) ||
                    r.Phone.Contains(term));
            }

            if (checkIn.HasValue)
            {
                query = query.Where(r => r.CheckIn.Date >= checkIn.Value.Date);
            }

            if (checkOut.HasValue)
            {
                query = query.Where(r => r.CheckOut.Date <= checkOut.Value.Date);
            }

            if (roomId.HasValue)
            {
                query = query.Where(r => r.RoomId == roomId.Value);
            }

            query = sortOrder switch
            {
                "room_asc" => query.OrderBy(r => r.Room != null ? r.Room.Number : ""),
                "room_desc" => query.OrderByDescending(r => r.Room != null ? r.Room.Number : ""),
                "guest_asc" => query.OrderBy(r => r.GuestName),
                "guest_desc" => query.OrderByDescending(r => r.GuestName),
                "checkin_asc" => query.OrderBy(r => r.CheckIn),
                "checkin_desc" => query.OrderByDescending(r => r.CheckIn),
                _ => query.OrderByDescending(r => r.CheckIn)
                          .ThenBy(r => r.Room != null ? r.Room.Number : "")
            };

            var reservations = await query.ToListAsync();
            return View(reservations);
        }

        [HttpGet]
        public async Task<IActionResult> Create(
            int? roomId,
            int? inquiryId,
            string? guestName,
            string? phone,
            DateTime? checkIn,
            DateTime? checkOut,
            int? guestsCount,
            string? preferredRoomNumber)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var reservation = new Reservation
            {
                CheckIn = checkIn?.Date ?? DateTime.Today,
                CheckOut = checkOut?.Date ?? DateTime.Today.AddDays(1),
                Status = ReservationStatus.Confirmed,
                GuestName = guestName ?? string.Empty,
                Phone = phone ?? string.Empty,
                GuestsCount = guestsCount ?? 1
            };

            reservation.Status = GetAutomaticReservationStatus(reservation);

            var rooms = await _context.Rooms
                .OrderBy(r => r.Number)
                .ToListAsync();

            int? selectedRoomId = roomId;

            if (!selectedRoomId.HasValue && !string.IsNullOrWhiteSpace(preferredRoomNumber))
            {
                var preferredRoom = rooms.FirstOrDefault(r => r.Number == preferredRoomNumber);
                if (preferredRoom != null)
                {
                    selectedRoomId = preferredRoom.Id;
                }
            }

            if (selectedRoomId.HasValue)
            {
                reservation.RoomId = selectedRoomId.Value;
            }

            ViewBag.InquiryId = inquiryId;
            ViewBag.PreferredRoomNumber = preferredRoomNumber;
            ViewBag.HasSearchedRooms = false;

            LoadReservationLists(rooms, reservation.RoomId, reservation.Status);

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAvailability(Reservation reservation, int? inquiryId, string? preferredRoomNumber)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            ViewBag.InquiryId = inquiryId;
            ViewBag.PreferredRoomNumber = preferredRoomNumber;
            ViewBag.HasSearchedRooms = true;

            reservation.Status = GetAutomaticReservationStatus(reservation);

            ValidateReservationDates(reservation);

            if (!ModelState.IsValid)
            {
                await LoadAllReservationLists(reservation.RoomId, reservation.Status);
                return View("Create", reservation);
            }

            var availableRooms = await _hotelService.GetAvailableRoomsAsync(
                reservation.CheckIn,
                reservation.CheckOut);

            if (reservation.GuestsCount > 0)
            {
                availableRooms = availableRooms
                    .Where(r => r.Capacity >= reservation.GuestsCount)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(preferredRoomNumber))
            {
                var preferredRoom = availableRooms.FirstOrDefault(r => r.Number == preferredRoomNumber);
                if (preferredRoom != null)
                {
                    reservation.RoomId = preferredRoom.Id;
                }
            }

            LoadReservationLists(availableRooms, reservation.RoomId, reservation.Status);

            if (!availableRooms.Any())
            {
                ModelState.AddModelError(string.Empty, "Няма свободни стаи за избрания период.");
            }

            return View("Create", reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation, int? inquiryId, string? preferredRoomNumber)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            ViewBag.InquiryId = inquiryId;
            ViewBag.PreferredRoomNumber = preferredRoomNumber;
            ViewBag.HasSearchedRooms = true;

            reservation.Status = GetAutomaticReservationStatus(reservation);

            ValidateReservationDates(reservation);

            var availableRooms = await _hotelService.GetAvailableRoomsAsync(
                reservation.CheckIn,
                reservation.CheckOut);

            if (reservation.GuestsCount > 0)
            {
                availableRooms = availableRooms
                    .Where(r => r.Capacity >= reservation.GuestsCount)
                    .ToList();
            }

            LoadReservationLists(availableRooms, reservation.RoomId, reservation.Status);

            if (!ModelState.IsValid)
            {
                return View(reservation);
            }

            var selectedRoomId = reservation.RoomId!.Value;

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == selectedRoomId);

            if (room == null)
            {
                ModelState.AddModelError(string.Empty, "Избраната стая не съществува.");
                return View(reservation);
            }

            if (room.Status == RoomStatus.Maintenance)
            {
                ModelState.AddModelError(string.Empty, "Стая, която е в ремонт, не може да бъде резервирана.");
                return View(reservation);
            }

            if (reservation.GuestsCount > room.Capacity)
            {
                ModelState.AddModelError(string.Empty, "Броят гости надвишава капацитета на избраната стая.");
                return View(reservation);
            }

            bool isAvailable = await _hotelService.IsRoomAvailableAsync(
                selectedRoomId,
                reservation.CheckIn,
                reservation.CheckOut);

            if (!isAvailable)
            {
                ModelState.AddModelError(string.Empty, "Стаята е вече заета за избрания период.");
                return View(reservation);
            }

            reservation.TotalAmount = _hotelService.CalculateTotalAmount(
                reservation.CheckIn,
                reservation.CheckOut,
                room.PricePerNight);

            reservation.Status = GetAutomaticReservationStatus(reservation);

            _context.Reservations.Add(reservation);

            if (inquiryId.HasValue)
            {
                var inquiry = await _context.Inquiries.FirstOrDefaultAsync(i => i.Id == inquiryId.Value);
                if (inquiry != null)
                {
                    inquiry.Status = "Превърнато в резервация";
                }
            }

            await _context.SaveChangesAsync();
            await _hotelService.RecalculateRoomStatusAsync(room.Id);

            TempData["SuccessMessage"] = "Резервацията е създадена успешно.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var reservation = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = GetAutomaticReservationStatus(reservation);

            await LoadAllReservationLists(reservation.RoomId, reservation.Status);
            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Reservation reservation)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            reservation.Status = GetAutomaticReservationStatus(reservation);

            await LoadAllReservationLists(reservation.RoomId, reservation.Status);

            ValidateReservationDates(reservation);

            var existingReservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservation.Id);

            if (existingReservation == null)
            {
                return NotFound();
            }

            var oldRoomId = existingReservation.RoomId;
            var selectedRoomId = reservation.RoomId!.Value;

            var newRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == selectedRoomId);
            if (newRoom == null)
            {
                ModelState.AddModelError(string.Empty, "Избраната стая не съществува.");
                return View(reservation);
            }

            if (newRoom.Status == RoomStatus.Maintenance)
            {
                ModelState.AddModelError(string.Empty, "Стая, която е в ремонт, не може да бъде резервирана.");
                return View(reservation);
            }

            if (reservation.GuestsCount > newRoom.Capacity)
            {
                ModelState.AddModelError(string.Empty, "Броят гости надвишава капацитета на избраната стая.");
                return View(reservation);
            }

            bool isAvailable = await _hotelService.IsRoomAvailableAsync(
                selectedRoomId,
                reservation.CheckIn,
                reservation.CheckOut,
                reservation.Id);

            if (!isAvailable)
            {
                ModelState.AddModelError(string.Empty, "Стаята е вече заета за избрания период.");
                return View(reservation);
            }

            if (!ModelState.IsValid)
            {
                return View(reservation);
            }

            existingReservation.GuestName = reservation.GuestName;
            existingReservation.Phone = reservation.Phone;
            existingReservation.CheckIn = reservation.CheckIn;
            existingReservation.CheckOut = reservation.CheckOut;
            existingReservation.GuestsCount = reservation.GuestsCount;
            existingReservation.RoomId = selectedRoomId;
            existingReservation.IsPaid = reservation.IsPaid;
            existingReservation.TotalAmount = _hotelService.CalculateTotalAmount(
                reservation.CheckIn,
                reservation.CheckOut,
                newRoom.PricePerNight);

            if (existingReservation.Status != ReservationStatus.Cancelled)
            {
                existingReservation.Status = GetAutomaticReservationStatus(existingReservation);
            }

            await _context.SaveChangesAsync();

            await _hotelService.RecalculateRoomStatusAsync(existingReservation.RoomId.Value);

            if (oldRoomId != existingReservation.RoomId)
            {
                await _hotelService.RecalculateRoomStatusAsync(oldRoomId.Value);
            }

            TempData["SuccessMessage"] = "Резервацията е редактирана успешно.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Резервацията не е намерена.";
                return RedirectToAction(nameof(Index));
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Резервацията вече е отказана.";
                return RedirectToAction(nameof(Index));
            }

            if (reservation.Status == ReservationStatus.CheckedOut)
            {
                TempData["ErrorMessage"] = "Приключена резервация не може да бъде отказана.";
                return RedirectToAction(nameof(Index));
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();
            await _hotelService.RecalculateRoomStatusAsync(reservation.RoomId!.Value);

            TempData["SuccessMessage"] = "Резервацията е отказана успешно.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var roomId = reservation.RoomId!.Value;

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            await _hotelService.RecalculateRoomStatusAsync(roomId);

            TempData["SuccessMessage"] = "Резервацията е изтрита успешно.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Calendar(DateTime? startDate, int days = 7, int? roomId = null)
        {
            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await NormalizeReservationStatusesAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var start = (startDate ?? DateTime.Today).Date;

            if (days <= 0) days = 7;
            if (days > 31) days = 31;

            var end = start.AddDays(days);

            ViewBag.StartDate = start;
            ViewBag.Days = days;
            ViewBag.RoomId = roomId;
            ViewBag.Dates = Enumerable.Range(0, days).Select(i => start.AddDays(i)).ToList();

            var roomsQuery = _context.Rooms.OrderBy(r => r.Number).AsQueryable();

            var allRooms = await roomsQuery.ToListAsync();

            ViewBag.RoomList = allRooms
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Number} - {RoomUiHelper.GetRoomTypeText(r.Type)}",
                    Selected = roomId.HasValue && r.Id == roomId.Value
                })
                .ToList();

            var roomsToShow = allRooms.AsEnumerable();

            if (roomId.HasValue)
            {
                roomsToShow = roomsToShow.Where(r => r.Id == roomId.Value);
            }

            ViewBag.Rooms = roomsToShow.ToList();

            var reservationsQuery = _context.Reservations
                .Include(r => r.Room)
                .Where(r =>
                    r.Status != ReservationStatus.Cancelled &&
                    r.CheckIn < end &&
                    r.CheckOut > start);

            if (roomId.HasValue)
            {
                reservationsQuery = reservationsQuery.Where(r => r.RoomId == roomId.Value);
            }

            var reservations = await reservationsQuery
                .OrderBy(r => r.Room != null ? r.Room.Number : "")
                .ThenBy(r => r.CheckIn)
                .ToListAsync();

            return View(reservations);
        }

        private void LoadReservationLists(List<Room> rooms, int? selectedRoomId, ReservationStatus selectedStatus)
        {
            ViewBag.RoomList = rooms
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Number} - {RoomUiHelper.GetRoomTypeText(r.Type)}",
                    Selected = selectedRoomId.HasValue && r.Id == selectedRoomId.Value
                })
                .ToList();

            ViewBag.ReservationStatuses = EnumSelectListHelper.CreateSelectList<ReservationStatus>(
                ReservationUiHelper.GetReservationStatusText, selectedStatus);
        }

        private async Task LoadAllReservationLists(int? selectedRoomId, ReservationStatus selectedStatus)
        {
            var rooms = await _context.Rooms.OrderBy(r => r.Number).ToListAsync();
            LoadReservationLists(rooms, selectedRoomId, selectedStatus);
        }

        private void ValidateReservationDates(Reservation reservation)
        {
            var today = DateTime.Today;
            var oneMonthBack = today.AddMonths(-1);

            if (reservation.CheckOut <= reservation.CheckIn)
            {
                ModelState.AddModelError(string.Empty, "Датата на напускане трябва да е след датата на настаняване.");
            }

            if (reservation.CheckOut.Date < today)
            {
                if (reservation.CheckIn.Date < oneMonthBack || reservation.CheckOut.Date < oneMonthBack)
                {
                    ModelState.AddModelError(string.Empty,
                        "Не може да се създава или редактира изцяло минала резервация, ако датите са по-стари от 1 месец назад.");
                }
            }
        }

        private ReservationStatus GetAutomaticReservationStatus(Reservation reservation)
        {
            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ReservationStatus.Cancelled;
            }

            var today = DateTime.Today;

            if (today >= reservation.CheckOut.Date)
            {
                return ReservationStatus.CheckedOut;
            }

            if (today >= reservation.CheckIn.Date)
            {
                return ReservationStatus.CheckedIn;
            }

            return ReservationStatus.Confirmed;
        }

        private async Task NormalizeReservationStatusesAsync()
        {
            var reservations = await _context.Reservations
                .Where(r => r.Status != ReservationStatus.Cancelled)
                .ToListAsync();

            var hasChanges = false;

            foreach (var reservation in reservations)
            {
                var newStatus = GetAutomaticReservationStatus(reservation);

                if (reservation.Status != newStatus)
                {
                    reservation.Status = newStatus;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}