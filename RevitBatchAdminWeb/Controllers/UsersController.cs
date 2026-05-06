using Microsoft.AspNetCore.Mvc;
using RevitBatchAdminWeb.Services;

namespace RevitBatchAdminWeb.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApiClient _apiClient;

        public UsersController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool SetAdminToken()
        {
            var token = HttpContext.Session.GetString("AdminToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            _apiClient.SetBearerToken(token);
            return true;
        }

        public async Task<IActionResult> Index()
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _apiClient.GetUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(new CreateUserDto
            {
                Role = "user",
                LicenseCount = 1,
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1))
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserDto model)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError("Username", "Username is required.");
                return View(model);
            }

            bool success = await _apiClient.CreateUserAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to create user.");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, string role)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            await _apiClient.UpdateUserRoleAsync(id, role);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            await _apiClient.DeleteUserAsync(id);
            return RedirectToAction("Index");
        }
    }
}