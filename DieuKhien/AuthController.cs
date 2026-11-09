using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySieuThi.DTO;
using QuanLySieuThi.Models;
using QuanLySieuThi.Service;

namespace QuanLySieuThi.Controllers
{
    public class AuthController(IAuthService authService) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole(User.FindFirstValue(ClaimTypes.Role));
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await authService.AuthenticateAsync(model.Username, model.Password, cancellationToken);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.Name)
            };
            if (user.EmployeeId.HasValue)
                claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = model.RememberMe, AllowRefresh = true });

            if (Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl!);
            return RedirectByRole(user.Role.Name);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return View(model);
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Forbid();

            if (model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
                return View(model);
            }

            if (!await authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword, cancellationToken))
            {
                ModelState.AddModelError(string.Empty, "Không đổi được mật khẩu. Hãy kiểm tra mật khẩu hiện tại.");
                return View(model);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["PasswordChanged"] = "Đã đổi mật khẩu. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult Denied() => View();

        private IActionResult RedirectByRole(string? role) => role switch
        {
            RoleNames.Admin => RedirectToAction("Dashboard", "Admin"),
            RoleNames.Cashier => RedirectToAction("Sales", "Screens"),
            RoleNames.StockStaff => RedirectToAction("Inventory", "Screens"),
            RoleNames.Customer => RedirectToAction("CustomerPortal", "Screens"),
            _ => RedirectToAction(nameof(Denied))
        };
    }
}
