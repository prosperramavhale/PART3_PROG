using Microsoft.AspNetCore.Mvc; using RaceDay.MVC.Models; using RaceDay.MVC.Services;
namespace RaceDay.MVC.Controllers;
public class AccountController(RaceDayApiClient api):Controller
{
 [HttpGet] public IActionResult Login()=>View();
 [HttpPost] public async Task<IActionResult> Login(LoginViewModel m){if(!ModelState.IsValid)return View(m);var r=await api.Post("api/auth/login",new{m.Email,m.Password});if(!r.ok||r.data is null){ModelState.AddModelError("",r.message);return View(m);}var d=r.data.Value;HttpContext.Session.SetString("Token",d.GetProperty("token").GetString()!);var u=d.GetProperty("user");HttpContext.Session.SetString("Role",u.GetProperty("role").GetString()!);HttpContext.Session.SetString("Name",u.GetProperty("firstName").GetString()+" "+u.GetProperty("lastName").GetString());HttpContext.Session.SetInt32("UserId",u.GetProperty("userId").GetInt32());return RedirectToAction("Index",u.GetProperty("role").GetString()=="Organiser"?"Organiser":"Participant");}
 [HttpGet] public IActionResult Register()=>View();
 [HttpPost] public async Task<IActionResult> Register(RegisterViewModel m){if(!ModelState.IsValid)return View(m);var r=await api.Post("api/auth/register",m);if(!r.ok){ModelState.AddModelError("",r.message);return View(m);}TempData["Message"]="Account created. Please sign in.";return RedirectToAction(nameof(Login));}
 public IActionResult Logout(){HttpContext.Session.Clear();return RedirectToAction("Index","Home");}
}
