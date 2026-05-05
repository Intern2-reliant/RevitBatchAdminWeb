using RevitBatchAdminWeb.Services;

namespace RevitBatchAdminWeb.Models
{
    public class CreateLicenseViewModel
    {
        public int UserId { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int Quantity { get; set; } = 1;

        public List<UserDto> Users { get; set; } = new();
    }
}