using EMSMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EMSMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

            public IActionResult Index()
            {


                // ?????? ?? ???????? ???? ???? ??? ??
                if (User.Identity.IsAuthenticated)
                {
                    if (Request.Cookies.TryGetValue("JWT_TOKEN", out var token) && !string.IsNullOrEmpty(token))
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(token);

                        var roles = jwt.Claims
                                              .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                                              .Select(c => c.Value)
                                              .ToList();

                        // ?? ???? ??? Admin
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                    }

                    // ??? ???? ??????? ?????? ?????????


                    // ?? ?????? ???? ???? ??? ?????? ???????
                    return View();
                }


                return View();

            }
            public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
