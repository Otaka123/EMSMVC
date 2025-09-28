using EMSMVC.Models;
using EMSMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace EMSMVC.Controllers
{
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly HttpClient _httpClient;

        public UsersController(ILogger<UsersController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://emsmvc.runasp.net/api/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
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

        // GET: Admin/Users
        [HttpGet]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            bool? isActive = null,
            string gender = "",
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = await GetAuthenticatedClient(); // بدون await

                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(searchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

                if (isActive.HasValue)
                    queryParams.Add($"isActive={isActive.Value}");

                if (!string.IsNullOrEmpty(gender))
                    queryParams.Add($"gender={Uri.EscapeDataString(gender)}");

                queryParams.Add($"pageNumber={pageNumber}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";

                _logger.LogInformation($"Calling API: User/All{queryString}");

                var response = await client.GetAsync($"User/All{queryString}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response: {content}");

                    var apiResponse = JsonSerializer.Deserialize<RequestResponse<PagedResult<UserResponse>>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                    {
                        // حفظ معايير البحث في ViewBag
                        ViewBag.SearchTerm = searchTerm;
                        ViewBag.IsActive = isActive;
                        ViewBag.Gender = gender;
                        ViewBag.PageNumber = pageNumber;
                        ViewBag.PageSize = pageSize;

                        return View(apiResponse.Data);
                    }
                    else
                    {
                        _logger.LogWarning($"API returned success but with issues: {apiResponse?.Message}");
                        TempData["Error"] = apiResponse?.Message ?? "حدث خطأ في تحميل البيانات";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    TempData["Error"] = $"خطأ في الخادم: {response.StatusCode}";
                }

                return RedirectToAction(nameof(Index), "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["Error"] = "حدث خطأ أثناء تحميل بيانات المستخدمين";
                return View(new PagedResult<UserResponse>());
            }
        }
        /// <summary>
        /// Manage user roles
        /// </summary>
        [HttpGet("ManageRoles/{userId}")]
        public async Task<IActionResult> ManageRoles(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "معرف المستخدم غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();

                _logger.LogInformation($"Calling API: User/manage-roles/{userId}");

                var response = await client.GetAsync($"Role/ManageRoles/{userId}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response: {content}");

                    var apiResponse = JsonSerializer.Deserialize<RequestResponse<ManageUserRolesDTO>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                    {
                        return View(apiResponse.Data);
                    }
                    else
                    {
                        _logger.LogWarning($"API returned success but with issues: {apiResponse?.Message}");
                        TempData["Error"] = apiResponse?.Message ?? "حدث خطأ في تحميل بيانات الأدوار";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    TempData["Error"] = $"خطأ في الخادم: {response.StatusCode}";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing roles for user: {UserId}", userId);
                TempData["Error"] = "حدث خطأ أثناء تحميل أدوار المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Update user roles
        /// </summary>
        [HttpPost("ManageRoles/{userId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles( ManageUserRolesDTO model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.UserId) || model.UserId != model.UserId)
                {
                    TempData["Error"] = "معرف المستخدم غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();

                // تحضير البيانات للإرسال
                var jsonContent = JsonSerializer.Serialize(model);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Calling API to update roles for user: {model.UserId}");

                var response = await client.PostAsync($"Role/ManageRoles", httpContent, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        TempData["Success"] = "تم تحديث أدوار المستخدم بنجاح";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = apiResponse?.Message ?? "فشل تحديث الأدوار";

                        // إعادة تحميل البيانات لعرض النموذج مرة أخرى
                        var dataResponse = await client.GetAsync($"User/ManageRoles/{model.UserId}", cancellationToken);
                        if (dataResponse.IsSuccessStatusCode)
                        {
                            var dataContent = await dataResponse.Content.ReadAsStringAsync();
                            var rolesResponse = JsonSerializer.Deserialize<RequestResponse<ManageUserRolesDTO>>(dataContent,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (rolesResponse?.IsSuccess == true && rolesResponse.Data != null)
                            {
                                rolesResponse.Data.SelectedRoles = model.SelectedRoles;
                                return View(rolesResponse.Data);
                            }
                        }

                        return View(model);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    TempData["Error"] = $"خطأ في الخادم: {response.StatusCode}";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for user: {UserId}", model.UserId);
                TempData["Error"] = "حدث خطأ أثناء تحديث أدوار المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }
        // GET: Admin/Users/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Users/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                var client = await GetAuthenticatedClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("User/Create", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse<UserRegistrationResponse>>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        TempData["Success"] = apiResponse.Message ?? "تم إنشاء المستخدم بنجاح";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        foreach (var error in apiResponse?.Errors ?? new List<string>())
                        {
                            ModelState.AddModelError(string.Empty, error);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "حدث خطأ أثناء إنشاء المستخدم");
                }

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                TempData["Error"] = "حدث خطأ أثناء إنشاء المستخدم";
                return View(request);
            }
        }

        // GET: Admin/Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] ="UserIdRequired";
                    return RedirectToAction(nameof(Index), "Home");
                }

                var client = await GetAuthenticatedClient();
                var response = await client.GetAsync($"User/{id}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse<UserResponse>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        var viewModel = new UpdateUserRequest
                        {
                            UserId = apiResponse.Data.Id,
                            FirstName = apiResponse.Data.FirstName,
                            LastName = apiResponse.Data.LastName,
                            Address = apiResponse.Data.Address,
                            DOB=apiResponse.Data.DateOfBirth,
                            Gender = apiResponse.Data.Gender,
                            
                    
                        };

                        return View(viewModel);
                    }
                }

                TempData["Error"] = "UserNotFound";
                return RedirectToAction(nameof(Index),"Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for user: {UserId}", id);
                TempData["Error"] = "LoadEditPageError";
                return RedirectToAction(nameof(Index),"Home");
            }
        }
        //// POST: Admin/Users/Edit
        ////[Authorize(Roles = "Admin")]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(UpdateUserRequest request, IFormFile? ProfilePicture, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return View(request);
        //        }

        //        // لو فيه صورة جديدة، نرفعها
        //        //if (ProfilePicture != null && ProfilePicture.Length > 0)
        //        //{
        //        //    var uploadsFolder = Path.Combine(_httpClient.BaseAddress.OriginalString, "uploads/profile");
        //        //    if (!Directory.Exists(uploadsFolder))
        //        //        Directory.CreateDirectory(uploadsFolder);

        //        //    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfilePicture.FileName)}";
        //        //    var filePath = Path.Combine(uploadsFolder, fileName);

        //        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //        //    {
        //        //        await ProfilePicture.CopyToAsync(stream, cancellationToken);
        //        //    }

        //        //    // نخزن المسار
        //        //    request.ProfilePictureUrl = $"/uploads/profile/{fileName}";


        //        var client = await GetAuthenticatedClient();
        //        var json = JsonSerializer.Serialize(request);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await client.PutAsync($"User/{request.UserId}/Update", content, cancellationToken);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        //            var apiResponse = JsonSerializer.Deserialize<RequestResponse<UserResponse>>(responseContent,
        //                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //            if (apiResponse?.IsSuccess == true)
        //            {
        //                TempData["Success"] = apiResponse.Message ?? "تم تحديث المستخدم بنجاح";
        //                return RedirectToAction(nameof(Index));
        //            }
        //            else
        //            {
        //                foreach (var error in apiResponse?.Errors ?? new List<string>())
        //                {
        //                    ModelState.AddModelError(string.Empty, error);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تحديث المستخدم");
        //        }

        //        return View(request);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating user {UserId}", request.UserId);
        //        TempData["Error"] = "حدث خطأ أثناء تعديل المستخدم";
        //        return View(request);
        //    }
        //}
        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UpdateUserRequest model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (id != model.UserId)
                {
                    TempData["Error"] = "UserMismatch";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();
                var response = await client.PutAsJsonAsync($"User/{id}", model, cancellationToken);

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        TempData["Success"] = "UserUpdatedSuccessfully";
                        return RedirectToAction(nameof(Index), "Home");
                    }
                }

                // في حالة وجود أخطاء من API
                _logger.LogWarning("UpdateUser failed with Status: {Status}, Content: {Content}",
                    response.StatusCode, content);

                TempData["Error"] = "UpdateFailed";
                ModelState.AddModelError("", "فشل في تحديث البيانات: " + content);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                TempData["Error"] = "UpdateError";
                return View(model);
            }
        }

        //// POST: Admin/Users/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(UpdateUserRequestViewModel model, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return View(model);
        //        }

        //        var updateRequest = new UpdateUserRequestViewModel
        //        {
        //            FirstName = model.FirstName,
        //            LastName = model.LastName,
        //            CurrentPassword = model.CurrentPassword,

        //        };

        //        var client = await GetAuthenticatedClient();
        //        var json = JsonSerializer.Serialize(updateRequest);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await client.PutAsync($"User/{model.UserId}", content, cancellationToken);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseContent = await response.Content.ReadAsStringAsync();
        //            var apiResponse = JsonSerializer.Deserialize<RequestResponse>(responseContent,
        //                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //            if (apiResponse?.IsSuccess == true)
        //            {
        //                TempData["Success"] = apiResponse.Message ?? "UpdateSuccessful";
        //                return RedirectToAction(nameof(Index));
        //            }
        //        }

        //        ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تحديث المستخدم");
        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating user: {UserId}", model.UserId);
        //        TempData["Error"] = "UpdateError";
        //        return RedirectToAction(nameof(Edit), new { id = model.UserId });
        //    }
        //}

        // GET: Admin/Users/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "UserIdRequired";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();
                var response = await client.GetAsync($"User/{id}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse<UserResponse>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        var viewModel = new UserResponseViewModel
                        {
                            Id = apiResponse.Data.Id,
                            FirstName = apiResponse.Data.FirstName,
                            LastName = apiResponse.Data.LastName,
                            Email = apiResponse.Data.Email,
                            UserName = apiResponse.Data.UserName,
                            PhoneNumber = apiResponse.Data.PhoneNumber,
                            IsActive = apiResponse.Data.IsActive,
                            EmailConfirmed = apiResponse.Data.EmailConfirmed,
                            PhoneNumberConfirmed = apiResponse.Data.PhoneNumberConfirmed,
                            CreatedAt = apiResponse.Data.CreatedAt,
                            LastLogin = apiResponse.Data.LastLoginDate
                        };

                        return View(viewModel);
                    }
                }

                TempData["Error"] = "UserNotFound";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details: {UserId}", id);
                TempData["Error"] = "LoadDetailsError";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/SoftDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "معرف المستخدم غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();
                var response = await client.PatchAsync($"User/{userId}/soft-delete", null, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        TempData["Success"] = apiResponse.Message;
                    }
                    else
                    {
                        TempData["Error"] = apiResponse?.Message;
                    }
                }
                else
                {
                    TempData["Error"] = "حدث خطأ أثناء تعطيل المستخدم";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting user: {UserId}", userId);
                TempData["Error"] = "حدث خطأ أثناء تعطيل المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "معرف المستخدم غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();
                var response = await client.PatchAsync($"User/{userId}/restore", null, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        TempData["Success"] = apiResponse.Message;
                    }
                    else
                    {
                        TempData["Error"] = apiResponse?.Message;
                    }
                }
                else
                {
                    TempData["Error"] = "حدث خطأ أثناء استعادة المستخدم";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user: {UserId}", userId);
                TempData["Error"] = "حدث خطأ أثناء استعادة المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "معرف المستخدم غير صالح";
                    return RedirectToAction(nameof(Index));
                }

                var client = await GetAuthenticatedClient();
                var response = await client.DeleteAsync($"User/{userId}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true)
                    {
                        TempData["Success"] = apiResponse.Message;
                    }
                    else
                    {
                        TempData["Error"] = apiResponse?.Message;
                    }
                }
                else
                {
                    TempData["Error"] = "حدث خطأ أثناء حذف المستخدم";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                TempData["Error"] = "حدث خطأ أثناء حذف المستخدم";
                return RedirectToAction(nameof(Index));
            }
        }

     

  

        [HttpGet]
        public IActionResult Login()
        {
            var model = new Models.LoginRequest();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Models.LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(request);
                }

                var client = _httpClient;
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("User/login", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<RequestResponse<LoginDTO>>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                    {
                        await SignInUserWithJwt(apiResponse.Data.AccessToken, apiResponse.Data.RefreshToken);
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة");
                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء عملية الدخول");
                return View(request);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = await GetAuthenticatedClient();
                await client.PostAsync("User/signout", null, cancellationToken);

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Response.Cookies.Delete("JWT_TOKEN");
                Response.Cookies.Delete("REFRESH_TOKEN");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sign-out error");
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task SignInUserWithJwt(string jwtToken, string refreshToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            var userId = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var userName = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var roles = token.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Name, userName ?? userId),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim("JWT_TOKEN", jwtToken)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2),
                    AllowRefresh = true
                });

            Response.Cookies.Append("JWT_TOKEN", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(2)
            });

            Response.Cookies.Append("REFRESH_TOKEN", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30)
            });
        }
    }
}

