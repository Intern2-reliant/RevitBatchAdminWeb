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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            string? token = await _apiClient.AdminLoginAsync(username, password);

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Invalid admin username or password.";
                return View();
            }

            HttpContext.Session.SetString("AdminToken", token);
            HttpContext.Session.SetString("AdminUsername", username);

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