using EMSMVC.Models;
using EMSMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace EMSMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        
        
            private readonly ILogger<RolesController> _logger;
            private readonly HttpClient _httpClient;

            public RolesController(ILogger<RolesController> logger, IHttpClientFactory httpClientFactory)
            {
                _logger = logger;
                _httpClient = httpClientFactory.CreateClient();
                _httpClient.BaseAddress = new Uri("https://emsmvc.runasp.net/api/");
            }

            private async Task<HttpClient> GetAuthenticatedClient()
            {
                var client = _httpClient;

                var jwtToken = Request.Cookies["JWT_TOKEN"];
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
                }

                return client;
            }

            // GET: Admin/Roles
            public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
            {
                try
                {
                    var client = await GetAuthenticatedClient();
                    var response = await client.GetAsync("Role/All", cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<RequestResponse<List<ApplicationRole>>>(content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result?.IsSuccess == true)
                        {
                            return View(result.Data);
                        }
                    }

                    TempData["Error"] = "حدث خطأ أثناء تحميل الأدوار";
                    return View(new List<ApplicationRole>());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading roles");
                    TempData["Error"] = "حدث خطأ أثناء تحميل الأدوار";
                    return View(new List<ApplicationRole>());
                }
            }

            // GET: Admin/Roles/Create
            public IActionResult Create()
            {
                return View();
            }

            // POST: Admin/Roles/Create
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(AddRoleRequest request, CancellationToken cancellationToken = default)
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

                    var response = await client.PostAsync("Role/Add", content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<RequestResponse<ApplicationRole>>(responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result?.IsSuccess == true)
                        {
                            TempData["Success"] = "تم إنشاء الدور بنجاح";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    ModelState.AddModelError(string.Empty, "حدث خطأ أثناء إنشاء الدور");
                    return View(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating role");
                    ModelState.AddModelError(string.Empty, "حدث خطأ أثناء إنشاء الدور");
                    return View(request);
                }
            }

            // GET: Admin/Roles/Edit/5
            public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        return NotFound();
                    }

                    var client = await GetAuthenticatedClient();
                    var response = await client.GetAsync($"Role/Get/{id}", cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<RequestResponse<ApplicationRole>>(content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result?.IsSuccess == true && result.Data != null)
                        {
                            var editRequest = new EditRoleRequest
                            {
                                RoleId = result.Data.Id,
                                NewRoleName = result.Data.Name
                            };

                            return View(editRequest);
                        }
                    }

                    TempData["Error"] = "الدور غير موجود";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading role for edit: {RoleId}", id);
                    TempData["Error"] = "حدث خطأ أثناء تحميل بيانات الدور";
                    return RedirectToAction(nameof(Index));
                }
            }

            // POST: Admin/Roles/Edit/5
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(EditRoleRequest request, CancellationToken cancellationToken = default)
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

                    var response = await client.PutAsync("Role/Update", content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<RequestResponse>(responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result?.IsSuccess == true)
                        {
                            TempData["Success"] = "تم تحديث الدور بنجاح";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تحديث الدور");
                    return View(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error editing role: {RoleId}", request.RoleId);
                    ModelState.AddModelError(string.Empty, "حدث خطأ أثناء تحديث الدور");
                    return View(request);
                }
            }

            [HttpGet]
            public async Task<IActionResult> Permissions(string id)
            {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "معرف الدور غير صالح";
                    return RedirectToAction("Index");
                }

                var client = await GetAuthenticatedClient();

                // استدعاء الـ API الخاص بالـ permissions مباشرة
                var response = await client.GetAsync($"https://emsmvc.runasp.net/api/Permission/{id}/permissions");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = $"فشل في جلب الصلاحيات. الكود: {response.StatusCode}";
                    return RedirectToAction("Index");
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {content}");

                var apiResult = JsonSerializer.Deserialize<RequestResponse<RolePermissionsResponse>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResult?.IsSuccess != true || apiResult.Data == null)
                {
                    TempData["ErrorMessage"] = "لم يتم العثور على بيانات الدور أو الصلاحيات";
                    return RedirectToAction("Index");
                }

                // تجهيز الـ ViewModel من البيانات القادمة من الـ API
                var viewModel = new RolePermissionsViewModel
                {
                    RoleId = apiResult.Data.RoleId,
                    RoleName = apiResult.Data.RoleName,
                    CurrentPermissions = apiResult.Data.CurrentPermissions ?? new List<PermissionDTO>(),
                    AllPermissions = apiResult.Data.AllPermissions ?? new List<PermissionDTO>(),
                    SelectedPermissionIds = apiResult.Data.CurrentPermissions?.Select(p => p.Id).ToList()
                };

                return View(viewModel);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error for role: {RoleId}", id);
                TempData["ErrorMessage"] = "خطأ في تحليل البيانات من الخادم";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading permissions for role: {RoleId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الصلاحيات";
                return RedirectToAction("Index");
            }
        }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Permissions(RolePermissionsViewModel model)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        // إعادة تعبئة النموذج في حالة الخطأ
                        await ReloadPermissionsModel(model);
                        return View(model);
                    }

                    var client = await GetAuthenticatedClient();

                    // تحديث صلاحيات الدور باستخدام الـ endpoint الصحيح
                    var updateRequest = new UpdateRolePermissionsRequest
                    {
                        RoleId = model.RoleId,
                        SelectedPermissionIds = model.SelectedPermissionIds ?? new List<int>()
                    };

                    var json = JsonSerializer.Serialize(updateRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync($"Permission/{model.RoleId}/permissions", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "تم تحديث صلاحيات الدور بنجاح";
                        return RedirectToAction("Permissions", new { id = model.RoleId });
                    }

                    ModelState.AddModelError("", "فشل تحديث الصلاحيات");
                    await ReloadPermissionsModel(model);
                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating permissions for role: {RoleId}", model.RoleId);
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث الصلاحيات");
                    await ReloadPermissionsModel(model);
                    return View(model);
                }
            }

            private async Task ReloadPermissionsModel(RolePermissionsViewModel model)
            {
                try
                {
                    var client = await GetAuthenticatedClient();
                    var permissionsResponse = await client.GetAsync($"Permission/{model.RoleId}/permissions");
                    if (permissionsResponse.IsSuccessStatusCode)
                    {
                        var permissionsContent = await permissionsResponse.Content.ReadAsStringAsync();
                        var permissionsResult = JsonSerializer.Deserialize<RequestResponse<RolePermissionsResponse>>(permissionsContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (permissionsResult?.IsSuccess == true)
                        {
                            model.CurrentPermissions = permissionsResult.Data?.CurrentPermissions ?? new List<PermissionDTO>();
                            model.AllPermissions = permissionsResult.Data?.AllPermissions ?? new List<PermissionDTO>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reloading permissions for model");
                }
            }

            // POST: Admin/Roles/RemovePermissionAjax
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<JsonResult> RemovePermissionAjax(string roleId, int permissionId, CancellationToken cancellationToken = default)
            {
                try
                {
                    if (string.IsNullOrEmpty(roleId) || permissionId <= 0)
                    {
                        return Json(new { success = false, message = "بيانات غير صالحة" });
                    }

                    var client = await GetAuthenticatedClient();
                    var response = await client.DeleteAsync($"Permission/{roleId}/permissions/{permissionId}", cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, message = "تم إزالة الصلاحية بنجاح" });
                    }

                    return Json(new { success = false, message = "فشل إزالة الصلاحية" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing permission: {PermissionId} from role: {RoleId}", permissionId, roleId);
                    return Json(new { success = false, message = "حدث خطأ أثناء إزالة الصلاحية" });
                }
            }

            // POST: Admin/Roles/Delete/5
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        return NotFound();
                    }

                    var client = await GetAuthenticatedClient();
                    var response = await client.DeleteAsync($"Role/{id}", cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<RequestResponse>(content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result?.IsSuccess == true)
                        {
                            TempData["Success"] = "تم حذف الدور بنجاح";
                        }
                        else
                        {
                            TempData["Error"] = result?.Message ?? "حدث خطأ أثناء حذف الدور";
                        }
                    }
                    else
                    {
                        TempData["Error"] = "حدث خطأ أثناء حذف الدور";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting role: {RoleId}", id);
                    TempData["Error"] = "حدث خطأ أثناء حذف الدور";
                    return RedirectToAction(nameof(Index));
                }
            }

            [HttpGet]
            public async Task<IActionResult> ManageUserRoles(string userId)
            {
                try
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        TempData["ErrorMessage"] = "معرف المستخدم غير صالح";
                        return RedirectToAction("Index", "Users");
                    }

                    var client = await GetAuthenticatedClient();

                    // جلب بيانات المستخدم
                    var userResponse = await client.GetAsync($"User/{userId}");
                    if (!userResponse.IsSuccessStatusCode)
                    {
                        TempData["ErrorMessage"] = "المستخدم غير موجود";
                        return RedirectToAction("Index", "Users");
                    }

                    var userContent = await userResponse.Content.ReadAsStringAsync();
                    var userResult = JsonSerializer.Deserialize<RequestResponse<UserResponse>>(userContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (userResult?.Data == null)
                    {
                        TempData["ErrorMessage"] = "المستخدم غير موجود";
                        return RedirectToAction("Index", "Users");
                    }

                    // جلب جميع الأدوار
                    var rolesResponse = await client.GetAsync("Role/All");
                    var allRoles = new List<ApplicationRole>();
                    if (rolesResponse.IsSuccessStatusCode)
                    {
                        var rolesContent = await rolesResponse.Content.ReadAsStringAsync();
                        var rolesResult = JsonSerializer.Deserialize<RequestResponse<List<ApplicationRole>>>(rolesContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        allRoles = rolesResult?.Data ?? new List<ApplicationRole>();
                    }

                    // جلب أدوار المستخدم الحالية
                    var userRoles = new List<string>();
                    var userRolesResponse = await client.GetAsync($"Role/{userId}/roles");
                    if (userRolesResponse.IsSuccessStatusCode)
                    {
                        var userRolesContent = await userRolesResponse.Content.ReadAsStringAsync();
                        var userRolesResult = JsonSerializer.Deserialize<RequestResponse<List<string>>>(userRolesContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        userRoles = userRolesResult?.Data ?? new List<string>();
                    }

                    var viewModel = new UserRolesViewModel
                    {
                        UserId = userId,
                        UserName = userResult.Data.UserName,
                        Email = userResult.Data.Email,
                        UserCurrentRoles = userRoles,
                        AllRoles = allRoles.Select(role => new UserRoleInfo
                        {
                            RoleId = role.Id,
                            RoleName = role.Name,
                            IsSelected = userRoles.Contains(role.Name)
                        }).ToList()
                    };

                    return View(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading user roles for user: {UserId}", userId);
                    TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الصفحة";
                    return RedirectToAction("Index", "Users");
                }
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesDTO request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "بيانات غير صالحة";
                    return RedirectToAction("ManageUserRoles", new { userId = request.UserId });
                }

                var client = await GetAuthenticatedClient();

                // تجهيز JSON
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // استدعاء الـ API
                var response = await client.PostAsync("Role/ManageRoles", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "تم تحديث أدوار المستخدم بنجاح";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("API Error while updating roles for user {UserId}: {Error}", request.UserId, errorContent);

                    TempData["ErrorMessage"] = "فشل تحديث أدوار المستخدم";
                }

                return RedirectToAction("ManageUserRoles", new { userId = request.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user roles for user: {UserId}", request.UserId);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث الأدوار";
                return RedirectToAction("ManageUserRoles", new { userId = request.UserId });
            }
        }

        // إضافة صلاحية للدور
        [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<JsonResult> AddPermissionToRole(string roleId, int permissionId)
            {
                try
                {
                    var client = await GetAuthenticatedClient();
                    var response = await client.PostAsync($"Permission/{roleId}/AddPermissionToRole/{permissionId}", null);

                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, message = "تم إضافة الصلاحية بنجاح" });
                    }

                    return Json(new { success = false, message = "فشل إضافة الصلاحية" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding permission to role");
                    return Json(new { success = false, message = "حدث خطأ أثناء إضافة الصلاحية" });
                }
            }
        
    }
}
