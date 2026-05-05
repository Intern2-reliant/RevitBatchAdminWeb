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

        public async Task<IActionResult> Index()
        {
            var users = await _apiClient.GetUsersAsync();
            return View(users);
        }


        public IActionResult Create()
        {
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
            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError("Username", "Username is required");
                return View(model);
            }

            bool success = await _apiClient.CreateUserAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to create user");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, string role)
        {
            await _apiClient.UpdateUserRoleAsync(id, role);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _apiClient.DeleteUserAsync(id);
            return RedirectToAction("Index");
        }
    }
}