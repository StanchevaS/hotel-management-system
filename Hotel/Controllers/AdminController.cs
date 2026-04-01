using Hotel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();

            var result = new List<(ApplicationUser User, IList<string> Roles)>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add((user, roles));
            }

            return View(result);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Имейлът е задължителен.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Паролата е задължителна.");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError(string.Empty, "Ролята е задължителна.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Role = role;
                return View();
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Вече съществува потребител с този имейл.");
                ViewBag.Role = role;
                return View();
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                ModelState.AddModelError(string.Empty, "Избраната роля не съществува.");
                ViewBag.Role = role;
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ViewBag.Role = role;
                return View();
            }

            await _userManager.AddToRoleAsync(user, role);

            TempData["SuccessMessage"] = "Потребителят е създаден успешно.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Невалиден потребител.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Потребителят не е намерен.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            TempData["SuccessMessage"] = "Потребителят е изтрит успешно.";
            return RedirectToAction(nameof(Users));
        }
    }
}