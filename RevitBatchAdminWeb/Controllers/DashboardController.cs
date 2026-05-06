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
            var token = HttpContext.Session.GetString("AdminToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            _apiClient.SetBearerToken(token);

            var users = await _apiClient.GetUsersAsync();
            var licenses = await _apiClient.GetLicensesAsync();

            var model = new DashboardViewModel
            {
                TotalUsers = users.Count,
                TotalLicenses = licenses.Count,
                ActiveLicenses = licenses.Count(l => l.isActive),
                InactiveLicenses = licenses.Count(l => !l.isActive),
                ContractManagers = users.Count(u =>
                    u.role.Equals("contract manager", StringComparison.OrdinalIgnoreCase))
            };

            return View(model);
        }
    }

    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalLicenses { get; set; }
        public int ActiveLicenses { get; set; }
        public int InactiveLicenses { get; set; }
        public int ContractManagers { get; set; }
    }
}