using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitBatchAdminWeb.Models;
using System.Text;

namespace RevitBatchAdminWeb.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://revitbatchapi.onrender.com/api";

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void SetBearerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<AdminLoginResult> AdminLoginAsync(string username, string password)
        {
            var json = JsonConvert.SerializeObject(new
            {
                username = username,
                password = password
            });

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // First attempt
                var response = await _httpClient.PostAsync(
                    $"{BaseUrl}/Auth/admin-login",
                    content
                );

                var responseJson = await response.Content.ReadAsStringAsync();

                // Render free API may sleep. Retry once if API returns 502/503.
                if ((int)response.StatusCode == 502 || (int)response.StatusCode == 503)
                {
                    await Task.Delay(3000);

                    var retryContent = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                    response = await _httpClient.PostAsync(
                        $"{BaseUrl}/Auth/admin-login",
                        retryContent
                    );

                    responseJson = await response.Content.ReadAsStringAsync();
                }

                if (!response.IsSuccessStatusCode)
                {
                    string friendlyMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Invalid admin username or password.",
                        System.Net.HttpStatusCode.BadGateway => "API is temporarily unavailable. Please try again in a few seconds.",
                        System.Net.HttpStatusCode.ServiceUnavailable => "API is waking up. Please try again in a few seconds.",
                        _ => "Admin login failed. Please try again."
                    };

                    return new AdminLoginResult
                    {
                        Success = false,
                        StatusCode = (int)response.StatusCode,
                        ErrorMessage = friendlyMessage
                    };
                }

                var obj = JObject.Parse(responseJson);
                string token = obj["token"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AdminLoginResult
                    {
                        Success = false,
                        StatusCode = (int)response.StatusCode,
                        ErrorMessage = "Login succeeded, but no token was returned."
                    };
                }

                return new AdminLoginResult
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    Token = token
                };
            }
            catch
            {
                return new AdminLoginResult
                {
                    Success = false,
                    StatusCode = 0,
                    ErrorMessage = "Unable to connect to the API. Please try again."
                };
            }
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            var json = await _httpClient.GetStringAsync($"{BaseUrl}/Users");
            return JsonConvert.DeserializeObject<List<UserDto>>(json) ?? new List<UserDto>();
        }

        public async Task<List<LicenseDto>> GetLicensesAsync()
        {
            var json = await _httpClient.GetStringAsync($"{BaseUrl}/License");
            return JsonConvert.DeserializeObject<List<LicenseDto>>(json) ?? new List<LicenseDto>();
        }

        public async Task<List<LicenseDto>> GetContractManagerLicensesAsync(int contractManagerId)
        {
            var json = await _httpClient.GetStringAsync($"{BaseUrl}/License/contract-manager/{contractManagerId}");
            return JsonConvert.DeserializeObject<List<LicenseDto>>(json) ?? new List<LicenseDto>();
        }

        public async Task<bool> ToggleLicenseAsync(int id)
        {
            var response = await _httpClient.PutAsync($"{BaseUrl}/License/{id}/toggle", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetDeviceAsync(int id)
        {
            var response = await _httpClient.PutAsync($"{BaseUrl}/License/{id}/reset-device", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateLicenseAsync(CreateLicenseViewModel model)
        {
            var payload = new
            {
                userId = model.UserId,
                expiryDate = model.ExpiryDate,
                role = "user",
                quantity = model.Quantity
            };

            var json = JsonConvert.SerializeObject(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/License/create",
                content
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateUserAsync(CreateUserDto user)
        {
            var json = JsonConvert.SerializeObject(user);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/Users/create",
                content
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserRoleAsync(int id, string role)
        {
            var json = JsonConvert.SerializeObject(new UpdateRoleDto
            {
                Role = role
            });

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PutAsync(
                $"{BaseUrl}/Users/{id}/role",
                content
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/Users/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLicenseExpiryAsync(int id, DateTime expiryDate)
        {
            var json = JsonConvert.SerializeObject(new
            {
                expiryDate = expiryDate
            });

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PutAsync(
                $"{BaseUrl}/License/{id}/expiry",
                content
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<dynamic> ContractManagerLoginAsync(string licenseKey)
        {
            var json = JsonConvert.SerializeObject(new
            {
                licenseKey = licenseKey
            });

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/Auth/contract-manager-login",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                return new
                {
                    success = false,
                    token = "",
                    userId = 0,
                    username = "",
                    role = "",
                    licenseType = "",
                    licenseKey = ""
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject(responseJson) ?? new
            {
                success = false,
                token = "",
                userId = 0,
                username = "",
                role = "",
                licenseType = "",
                licenseKey = ""
            };

            return result;
        }

        public async Task<bool> ContractManagerResetDeviceAsync(int licenseId)
        {
            var response = await _httpClient.PutAsync(
                $"{BaseUrl}/License/contract-manager/reset-device/{licenseId}",
                null
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ContractManagerToggleLicenseAsync(int licenseId)
        {
            var response = await _httpClient.PutAsync(
                $"{BaseUrl}/License/contract-manager/toggle/{licenseId}",
                null
            );

            return response.IsSuccessStatusCode;
        }
    }

    public class UserDto
    {
        public int id { get; set; }
        public string username { get; set; } = "";
        public string role { get; set; } = "";
        public int licenseCount { get; set; }
    }

    public class LicenseDto
    {
        public int id { get; set; }
        public int userId { get; set; }
        public string licenseKey { get; set; } = "";
        public bool isActive { get; set; }
        public object? deviceId { get; set; }
        public object? activatedAt { get; set; }
        public object? lastSeenAt { get; set; }
        public string expiryDate { get; set; } = "";
        public bool subscriptionEnabled { get; set; }
        public string username { get; set; } = "";
        public string role { get; set; } = "";
        public string? licenseType { get; set; }
        public int? parentContractManagerId { get; set; }
    }

    public class AdminLoginResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = "";
        public string token { get; set; } = "";
        public int userId { get; set; }
        public string username { get; set; } = "";
        public string role { get; set; } = "";
        public string licenseType { get; set; } = "";
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = "";
        public string Role { get; set; } = "user";
        public string PasswordHash { get; set; } = "";
        public int LicenseCount { get; set; } = 1;
        public DateOnly ExpiryDate { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Role { get; set; } = "user";
    }

    public class AdminLoginResult
    {
        public bool Success { get; set; }
        public string Token { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public int StatusCode { get; set; }
    }
}