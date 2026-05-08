using Microsoft.AspNetCore.Mvc;
using RevitBatchAdminWeb.Services;

namespace RevitBatchAdminWeb.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApiClient _apiClient;

        public DashboardController(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            string? token = HttpContext.Session.GetString("AdminToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            _apiClient.SetBearerToken(token);

            var users = await _apiClient.GetUsersAsync();
            var licenses = await _apiClient.GetLicensesAsync();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalLicenses = licenses.Count;
            ViewBag.ActiveLicenses = licenses.Count(l => l.isActive);
            ViewBag.InactiveLicenses = licenses.Count(l => !l.isActive);
            ViewBag.ContractManagers = users.Count(u =>
                !string.IsNullOrWhiteSpace(u.role) &&
                u.role.Trim().ToLower() == "contract manager"
            );

            return View();
        }
    }
}