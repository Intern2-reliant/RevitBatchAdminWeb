using Microsoft.AspNetCore.Mvc;
using RevitBatchAdminWeb.Services;

namespace RevitBatchAdminWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiClient _apiClient;

        public AuthController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var loginResult = await _apiClient.AdminLoginAsync(
                username.Trim(),
                password.Trim()
            );

            if (!loginResult.Success || string.IsNullOrWhiteSpace(loginResult.Token))
            {
                ViewBag.Error = loginResult.ErrorMessage;
                return View();
            }

            HttpContext.Session.SetString("AdminToken", loginResult.Token);
            HttpContext.Session.SetString("AdminUsername", username.Trim());

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}