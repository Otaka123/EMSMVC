using EMSMVC.Models;
using EMSMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace EMSMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly HttpClient _httpClient;

        public AdminController(ILogger<AdminController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://emsmvc.runasp.net/api/");
        }

        private async Task<HttpClient> GetAuthenticatedClient()
        {
            var client = _httpClient;

            // إضافة التوكن إذا كان موجوداً
            var jwtToken = Request.Cookies["JWT_TOKEN"];
            if (!string.IsNullOrEmpty(jwtToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            }

            return client;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region إدارة المستخدمين - Views

        [HttpGet]
        public IActionResult Users()
        {
            return View();
        }

        [HttpGet]
        public IActionResult UserDetails(string id)
        {
            ViewBag.UserId = id;
            return View();
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        #endregion

        #region API Actions لإدارة المستخدمين

        /// <summary>
        /// جلب جميع المستخدمين مع Pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            bool? isActive = null,
            string sortBy = "CreatedAt",
            bool sortDescending = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = await GetAuthenticatedClient();

                var queryParams = new List<string>
                {
                    $"pageNumber={pageNumber}",
                    $"pageSize={pageSize}",
                    $"sortBy={Uri.EscapeDataString(sortBy)}",
                    $"sortDescending={sortDescending}"
                };

                if (!string.IsNullOrEmpty(searchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
                if (isActive.HasValue)
                    queryParams.Add($"isActive={isActive.Value}");

                var queryString = "?" + string.Join("&", queryParams);
                var response = await client.GetAsync($"User/All{queryString}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse<PagedResult<UserResponse>>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }

                return BadRequest(new { message = "Failed to retrieve users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// جلب مستخدم بواسطة ID
        /// </summary>
        [HttpGet("GetUserById/{id}")]
        public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var client = await GetAuthenticatedClient();
                var response = await client.GetAsync($"User/{id}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse<UserResponse>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = "User not found" });
                }

                return BadRequest(new { message = "Failed to retrieve user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// إنشاء مستخدم جديد
        /// </summary>
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid data", errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var client = await GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("User/Create", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse<UserRegistrationResponse>>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return Conflict(new { message = "User already exists" });
                }

                return BadRequest(new { message = "Failed to create user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        ///// <summary>
        ///// تحديث مستخدم
        ///// </summary>
        //[HttpPut("UpdateUser/{id}")]
        //public async Task<IActionResult> UpdateUser(string id, [FromBody] update request, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid || string.IsNullOrEmpty(id))
        //        {
        //            return BadRequest(new { message = "Invalid data" });
        //        }

        //        var client = await GetAuthenticatedClient();
        //        var json = JsonSerializer.Serialize(request);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await client.PutAsync($"User/{id}", content, cancellationToken);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            var result = JsonSerializer.Deserialize<RequestResponse>(responseContent,
        //                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //            return Ok(result);
        //        }
        //        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        //        {
        //            return NotFound(new { message = "User not found" });
        //        }

        //        return BadRequest(new { message = "Failed to update user" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
        //        return StatusCode(500, new { message = "Internal server error" });
        //    }
        //}

        /// <summary>
        /// تحديث بيانات المستخدم
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(
            string userId,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = await GetAuthenticatedClient();

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"User/{userId}", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse<UserResponse>>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }

                return BadRequest(new { message = "Failed to update user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        /// <summary>
        /// حذف مستخدم (Soft Delete)
        /// </summary>
        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var client = await GetAuthenticatedClient();
                var response = await client.DeleteAsync($"User/{id}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = "User not found" });
                }

                return BadRequest(new { message = "Failed to delete user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// تعطيل مستخدم (Soft Delete)
        /// </summary>
        [HttpPatch("SoftDeleteUser/{id}")]
        public async Task<IActionResult> SoftDeleteUser(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var client = await GetAuthenticatedClient();
                var response = await client.PatchAsync($"User/{id}/soft-delete", null, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = "User not found" });
                }

                return BadRequest(new { message = "Failed to soft delete user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting user with ID: {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// استعادة مستخدم معطل
        /// </summary>
        [HttpPatch("RestoreUser/{id}")]
        public async Task<IActionResult> RestoreUser(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var client = await GetAuthenticatedClient();
                var response = await client.PatchAsync($"User/{id}/restore", null, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = "User not found" });
                }

                return BadRequest(new { message = "Failed to restore user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user with ID: {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// التحقق من حالة المستخدم
        /// </summary>
        [HttpGet("CheckUserStatus/{id}")]
        public async Task<IActionResult> CheckUserStatus(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var client = await GetAuthenticatedClient();
                var response = await client.GetAsync($"User/{id}/status", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RequestResponse<object>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(result);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = "User not found" });
                }

                return BadRequest(new { message = "Failed to check user status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status for user with ID: {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        #endregion
    }
}

