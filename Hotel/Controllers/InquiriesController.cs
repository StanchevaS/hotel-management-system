using Hotel.Data;
using Hotel.Enums;
using Hotel.Helpers;
using Hotel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Controllers
{
    [Authorize(Roles = "Administrator,Receptionist")]
    public class InquiriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InquiriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            LoadPreferredRoomTypes();

            var inquiry = new Inquiry
            {
                CheckIn = DateTime.Today,
                CheckOut = DateTime.Today.AddDays(1),
                GuestsCount = 1
            };

            return View(inquiry);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inquiry inquiry)
        {
            ValidateInquiryDates(inquiry);
            LoadPreferredRoomTypes(inquiry.PreferredRoom);

            if (!ModelState.IsValid)
            {
                return View(inquiry);
            }

            inquiry.Status = "Ново";
            inquiry.CreatedOn = DateTime.Now;

            _context.Inquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Вашето запитване беше изпратено успешно.";
            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var inquiries = await _context.Inquiries
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedOn)
                .ToListAsync();

            return View(inquiries);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var inquiry = await _context.Inquiries
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inquiry == null)
            {
                return NotFound();
            }

            return View(inquiry);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var inquiry = await _context.Inquiries
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inquiry == null)
            {
                return NotFound();
            }

            ViewBag.StatusOptions = GetStatusOptions();
            LoadPreferredRoomTypes(inquiry.PreferredRoom);

            return View(inquiry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Inquiry inquiry)
        {
            ViewBag.StatusOptions = GetStatusOptions();
            LoadPreferredRoomTypes(inquiry.PreferredRoom);

            ValidateInquiryDates(inquiry);

            if (!ModelState.IsValid)
            {
                return View(inquiry);
            }

            var existingInquiry = await _context.Inquiries
                .FirstOrDefaultAsync(i => i.Id == inquiry.Id);

            if (existingInquiry == null)
            {
                return NotFound();
            }

            existingInquiry.Name = inquiry.Name;
            existingInquiry.Phone = inquiry.Phone;
            existingInquiry.CheckIn = inquiry.CheckIn;
            existingInquiry.CheckOut = inquiry.CheckOut;
            existingInquiry.GuestsCount = inquiry.GuestsCount;
            existingInquiry.PreferredRoom = inquiry.PreferredRoom;
            existingInquiry.Message = inquiry.Message;
            existingInquiry.Status = inquiry.Status;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Запитването е обновено успешно.";
            return RedirectToAction(nameof(Index));
        }

        private static List<string> GetStatusOptions() => new()
        {
            "Ново",
            "Обработва се",
            "Обработено",
            "Отказано",
            "Превърнато в резервация"
        };

        private void LoadPreferredRoomTypes(string? selectedValue = null)
        {
            ViewBag.PreferredRoomTypes = Enum.GetValues<RoomType>()
                .Select(t => new SelectListItem
                {
                    Value = RoomUiHelper.GetRoomTypeText(t),
                    Text = RoomUiHelper.GetRoomTypeText(t),
                    Selected = selectedValue == RoomUiHelper.GetRoomTypeText(t)
                })
                .ToList();
        }

        private void ValidateInquiryDates(Inquiry inquiry)
        {
            if (inquiry.CheckOut <= inquiry.CheckIn)
            {
                ModelState.AddModelError(string.Empty, "Датата на напускане трябва да е след датата на настаняване.");
            }
        }
    }
}