using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
namespace RaceDay.MVC.Controllers;
public class OrganiserController(RaceDayApiClient api):Controller
{
 private bool Allowed()=>HttpContext.Session.GetString("Role")=="Organiser";
 private IActionResult Login()=>RedirectToAction("Login","Account");
 public async Task<IActionResult> Index(){if(!Allowed())return Login();ViewBag.Events=await api.Get("api/events");return View();}
 [HttpGet] public IActionResult Create(){if(!Allowed())return Login();return View(new EventViewModel());}
 [HttpPost] public async Task<IActionResult> Create(EventViewModel m){if(!Allowed())return Login();if(!ModelState.IsValid)return View(m);var r=await api.Post("api/events",new{m.Title,m.Description,m.Location,m.EventDate,m.EventType,m.Status});if(!r.ok){ModelState.AddModelError("",r.message);return View(m);}if(m.BannerImage is not null && m.BannerImage.Length>0){if(r.data is null || !r.data.Value.TryGetProperty("eventId",out var eventId)){ModelState.AddModelError("","Event was created, but its ID could not be read for the banner upload.");return View(m);}var upload=await api.UploadFile($"api/events/{eventId.GetInt32()}/banner",m.BannerImage);if(!upload.ok){TempData["Message"]="Event created, but banner upload failed: "+upload.message;return RedirectToAction(nameof(Index));}}TempData["Message"]="Event created successfully.";return RedirectToAction(nameof(Index));}
 [HttpPost] public async Task<IActionResult> Delete(int id){if(!Allowed())return Login();var ok=await api.Delete($"api/events/{id}");TempData["Message"]=ok?"Event cancelled.":"The event could not be cancelled.";return RedirectToAction(nameof(Index));}
 [HttpGet] public async Task<IActionResult> Categories(int id){if(!Allowed())return Login();ViewBag.EventId=id;ViewBag.Event=await api.Get($"api/events/{id}");ViewBag.Categories=await api.Get($"api/events/{id}/categories");return View();}
 [HttpPost] public async Task<IActionResult> AddCategory(CategoryViewModel m){if(!Allowed())return Login();var r=await api.Post($"api/events/{m.EventId}/categories",m);TempData["Message"]=r.ok?"Category added.":r.message;return RedirectToAction(nameof(Categories),new{id=m.EventId});}
 [HttpPost] public async Task<IActionResult> DeleteCategory(int id,int eventId){if(!Allowed())return Login();var ok=await api.Delete($"api/categories/{id}");TempData["Message"]=ok?"Category deleted.":"Category could not be deleted. It may have enrolments.";return RedirectToAction(nameof(Categories),new{id=eventId});}
 [HttpGet] public async Task<IActionResult> Enrolments(int id){if(!Allowed())return Login();ViewBag.EventId=id;ViewBag.Event=await api.Get($"api/events/{id}");ViewBag.Enrolments=await api.Get($"api/events/{id}/enrolments");return View();}
 [HttpPost] public async Task<IActionResult> UpdateStatus(StatusViewModel m,int eventId){if(!Allowed())return Login();var r=await api.Put($"api/enrolments/{m.EnrolmentId}/status",new{status=m.Status});TempData["Message"]=r.ok?"Enrolment status updated.":r.message;return RedirectToAction(nameof(Enrolments),new{id=eventId});}
 [HttpGet] public async Task<IActionResult> Results(int id){if(!Allowed())return Login();ViewBag.EventId=id;ViewBag.Event=await api.Get($"api/events/{id}");ViewBag.Enrolments=await api.Get($"api/events/{id}/enrolments");ViewBag.Results=await api.Get($"api/events/{id}/results");return View();}
 [HttpPost] public async Task<IActionResult> AddResult(ResultViewModel m,int eventId){if(!Allowed())return Login();var r=await api.Post("api/results",m);TempData["Message"]=r.ok?"Race result published.":r.message;return RedirectToAction(nameof(Results),new{id=eventId});}
}
