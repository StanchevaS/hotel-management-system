using Hotel.Data;
using Hotel.Enums;
using Hotel.Models;
using Hotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHotelService _hotelService;

        public ReportsController(ApplicationDbContext context, IHotelService hotelService)
        {
            _context = context;
            _hotelService = hotelService;
        }

        private sealed class RevenueReportRow
        {
            public Reservation Reservation { get; set; } = null!;
            public decimal PeriodRevenue { get; set; }
        }

        private sealed class PopularRoomTypeRow
        {
            public string RoomType { get; set; } = string.Empty;
            public int Count { get; set; }
            public decimal Revenue { get; set; }
        }

        [HttpGet]
        public IActionResult Occupancy()
        {
            var filter = new ReportFilter
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            };

            return View(filter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Occupancy(ReportFilter filter)
        {
            if (!ValidateFilter(filter))
            {
                return View(filter);
            }

            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var start = filter.StartDate!.Value.Date;
            var endExclusive = filter.EndDate!.Value.Date.AddDays(1);

            var allowedRoomIds = await _context.Rooms
                .Where(r => r.Status != RoomStatus.Maintenance)
                .Select(r => r.Id)
                .ToListAsync();

            var totalRooms = allowedRoomIds.Count;

            var occupiedReservations = await _context.Reservations
                .Where(r =>
                    r.RoomId.HasValue &&
                    allowedRoomIds.Contains(r.RoomId.Value) &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.CheckIn < endExclusive &&
                    r.CheckOut > start)
                .ToListAsync();

            int totalDays = (endExclusive - start).Days;
            int roomDays = totalRooms * totalDays;

            int occupiedRoomDays = occupiedReservations.Sum(r =>
            {
                var overlapStart = r.CheckIn.Date < start ? start : r.CheckIn.Date;
                var overlapEnd = r.CheckOut.Date > endExclusive ? endExclusive : r.CheckOut.Date;
                return overlapEnd > overlapStart ? (overlapEnd - overlapStart).Days : 0;
            });

            ViewBag.TotalRooms = totalRooms;
            ViewBag.TotalDays = totalDays;
            ViewBag.RoomDays = roomDays;
            ViewBag.OccupiedRoomDays = occupiedRoomDays;
            ViewBag.OccupancyPercent = roomDays == 0 ? 0 : (decimal)occupiedRoomDays * 100m / roomDays;

            return View(filter);
        }

        [HttpGet]
        public IActionResult Revenue()
        {
            var filter = new ReportFilter
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            };

            return View(filter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revenue(ReportFilter filter)
        {
            if (!ValidateFilter(filter))
            {
                return View(filter);
            }

            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var start = filter.StartDate!.Value.Date;
            var endExclusive = filter.EndDate!.Value.Date.AddDays(1);

            var reservations = await _context.Reservations
                .Include(r => r.Room)
                .Where(r =>
                    r.RoomId.HasValue &&
                    r.Room != null &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.CheckIn < endExclusive &&
                    r.CheckOut > start)
                .OrderByDescending(r => r.CheckIn)
                .ToListAsync();

            var revenueRows = new List<RevenueReportRow>();
            decimal totalRevenue = 0m;
            decimal paidRevenue = 0m;
            decimal unpaidRevenue = 0m;

            foreach (var reservation in reservations)
            {
                var periodRevenue = await _hotelService.CalculateRevenueForPeriodAsync(
                    reservation.CheckIn,
                    reservation.CheckOut,
                    reservation.TotalAmount,
                    start,
                    endExclusive);

                revenueRows.Add(new RevenueReportRow
                {
                    Reservation = reservation,
                    PeriodRevenue = periodRevenue
                });

                totalRevenue += periodRevenue;

                if (reservation.IsPaid)
                    paidRevenue += periodRevenue;
                else
                    unpaidRevenue += periodRevenue;
            }

            ViewBag.Reservations = revenueRows;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PaidRevenue = paidRevenue;
            ViewBag.UnpaidRevenue = unpaidRevenue;

            return View(filter);
        }

        [HttpGet]
        public IActionResult PopularRoomTypes()
        {
            var filter = new ReportFilter
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            };

            return View(filter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PopularRoomTypes(ReportFilter filter)
        {
            if (!ValidateFilter(filter))
            {
                return View(filter);
            }

            await _hotelService.AutoCompleteExpiredReservationsAsync();
            await _hotelService.RecalculateAllRoomStatusesAsync();

            var start = filter.StartDate!.Value.Date;
            var endExclusive = filter.EndDate!.Value.Date.AddDays(1);

            var reservations = await _context.Reservations
                .Include(r => r.Room)
                .Where(r =>
                    r.RoomId.HasValue &&
                    r.Room != null &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.CheckIn < endExclusive &&
                    r.CheckOut > start)
                .ToListAsync();

            var grouped = new List<PopularRoomTypeRow>();

            foreach (var group in reservations.GroupBy(r => r.Room!.Type))
            {
                decimal revenue = 0m;

                foreach (var reservation in group)
                {
                    revenue += await _hotelService.CalculateRevenueForPeriodAsync(
                        reservation.CheckIn,
                        reservation.CheckOut,
                        reservation.TotalAmount,
                        start,
                        endExclusive);
                }

                grouped.Add(new PopularRoomTypeRow
                {
                    RoomType = group.Key.ToString(),
                    Count = group.Count(),
                    Revenue = revenue
                });
            }

            ViewBag.PopularTypes = grouped
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Revenue)
                .ToList();

            return View(filter);
        }

        private bool ValidateFilter(ReportFilter filter)
        {
            if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Моля, изберете начален и краен период.");
                return false;
            }

            if (filter.EndDate.Value.Date < filter.StartDate.Value.Date)
            {
                ModelState.AddModelError(string.Empty, "Крайната дата трябва да е след началната.");
                return false;
            }

            return true;
        }
    }
}