using Microsoft.AspNetCore.Mvc; using RaceDay.MVC.Services;
namespace RaceDay.MVC.Controllers;
public class HomeController(RaceDayApiClient api):Controller
{
 public async Task<IActionResult> Index(){ViewBag.Events=await api.Get("api/events");return View();}
 public async Task<IActionResult> Details(int id){ViewBag.Event=await api.Get($"api/events/{id}");if(ViewBag.Event is null)return NotFound();return View();}
 public IActionResult Error()=>View();
}
