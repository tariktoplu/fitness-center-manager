using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // Bu sayfa sadece API'yi tüketmek için bir arayüz olacak
    public IActionResult Reports()
    {
        return View();
    }
}