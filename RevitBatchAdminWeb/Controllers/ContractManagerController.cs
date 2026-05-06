using Microsoft.AspNetCore.Mvc;
using RevitBatchAdminWeb.Services;

namespace RevitBatchAdminWeb.Controllers
{
    public class ContractManagerController : Controller
    {
        private readonly ApiClient _apiClient;

        public ContractManagerController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                ViewBag.Error = "License key is required.";
                return View();
            }

            dynamic result = await _apiClient.ContractManagerLoginAsync(licenseKey);

            bool success = result.success;
            if (!success)
            {
                ViewBag.Error = "Invalid contract manager license.";
                return View();
            }

            string token = Convert.ToString(result.token);
            int userId = Convert.ToInt32(result.userId);
            string username = Convert.ToString(result.username);
            string licenseType = Convert.ToString(result.licenseType);

            if (!licenseType.Equals("MainCM", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Only main contract manager licenses can access this panel.";
                return View();
            }

            HttpContext.Session.SetString("CmToken", token);
            HttpContext.Session.SetInt32("CmUserId", userId);
            HttpContext.Session.SetString("CmUsername", username);
            HttpContext.Session.SetString("CmLicenseKey", licenseKey);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("CmToken");
            var userId = HttpContext.Session.GetInt32("CmUserId");

            if (string.IsNullOrWhiteSpace(token) || userId == null)
            {
                return RedirectToAction("Login");
            }

            _apiClient.SetBearerToken(token);

            var licenses = await _apiClient.GetContractManagerLicensesAsync(userId.Value);

            ViewBag.CmUsername = HttpContext.Session.GetString("CmUsername");
            ViewBag.CmLicenseKey = HttpContext.Session.GetString("CmLicenseKey");

            return View(licenses);
        }


        [HttpPost]
        public async Task<IActionResult> ResetDevice(int licenseId)
        {
            var token = HttpContext.Session.GetString("CmToken");
            var userId = HttpContext.Session.GetInt32("CmUserId");

            if (string.IsNullOrWhiteSpace(token) || userId == null)
            {
                return RedirectToAction("Login");
            }

            _apiClient.SetBearerToken(token);

            bool success = await _apiClient.ContractManagerResetDeviceAsync(licenseId);

            if (!success)
            {
                TempData["Error"] = "Device reset failed. You can only reset your own sub-license devices.";
            }
            else
            {
                TempData["Success"] = "Device reset successfully.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLicense(int licenseId)
        {
            var token = HttpContext.Session.GetString("CmToken");
            var userId = HttpContext.Session.GetInt32("CmUserId");

            if (string.IsNullOrWhiteSpace(token) || userId == null)
            {
                return RedirectToAction("Login");
            }

            _apiClient.SetBearerToken(token);

            bool success = await _apiClient.ContractManagerToggleLicenseAsync(licenseId);

            if (!success)
            {
                TempData["Error"] = "Toggle failed. You can only toggle your own sub-licenses.";
            }
            else
            {
                TempData["Success"] = "Sub-license status updated successfully.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CmToken");
            HttpContext.Session.Remove("CmUserId");
            HttpContext.Session.Remove("CmUsername");
            HttpContext.Session.Remove("CmLicenseKey");

            return RedirectToAction("Login");
        }
    }
}