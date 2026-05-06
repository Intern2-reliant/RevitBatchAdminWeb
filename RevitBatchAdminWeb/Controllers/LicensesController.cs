using Microsoft.AspNetCore.Mvc;
using RevitBatchAdminWeb.Services;
using RevitBatchAdminWeb.Models;

namespace RevitBatchAdminWeb.Controllers
{
    public class LicensesController : Controller
    {
        private readonly ApiClient _apiClient;

        public LicensesController(ApiClient apiClient)
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

            var licenses = await _apiClient.GetLicensesAsync();
            return View(licenses);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? userId)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            var users = await _apiClient.GetUsersAsync();

            var model = new CreateLicenseViewModel
            {
                Users = users,
                UserId = userId ?? 0,
                ExpiryDate = DateTime.Today.AddYears(1),
                Quantity = 1
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateLicenseViewModel model)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (model.UserId <= 0)
            {
                ModelState.AddModelError("UserId", "Please select a user.");
            }

            if (model.Quantity < 1)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }

            if (!ModelState.IsValid)
            {
                model.Users = await _apiClient.GetUsersAsync();
                return View(model);
            }

            bool success = await _apiClient.CreateLicenseAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to add license.");
                model.Users = await _apiClient.GetUsersAsync();
                return View(model);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ContractManager(int id)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            var licenses = await _apiClient.GetContractManagerLicensesAsync(id);

            ViewBag.ContractManagerId = id;

            return View(licenses);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int id, int? returnContractManagerId)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            await _apiClient.ToggleLicenseAsync(id);

            if (returnContractManagerId.HasValue)
            {
                return RedirectToAction("ContractManager", new { id = returnContractManagerId.Value });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ResetDevice(int id, int? returnContractManagerId)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            await _apiClient.ResetDeviceAsync(id);

            if (returnContractManagerId.HasValue)
            {
                return RedirectToAction("ContractManager", new { id = returnContractManagerId.Value });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateExpiry(int id, DateTime expiryDate, int? returnContractManagerId)
        {
            if (!SetAdminToken())
            {
                return RedirectToAction("Login", "Auth");
            }

            await _apiClient.UpdateLicenseExpiryAsync(id, expiryDate);

            if (returnContractManagerId.HasValue)
            {
                return RedirectToAction("ContractManager", new { id = returnContractManagerId.Value });
            }

            return RedirectToAction("Index");
        }
    }
}