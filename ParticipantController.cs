using Microsoft.AspNetCore.Mvc;using RaceDay.MVC.Models;using RaceDay.MVC.Services;
namespace RaceDay.MVC.Controllers;
public class ParticipantController(RaceDayApiClient api):Controller
{
 private bool Allowed()=>HttpContext.Session.GetString("Role")=="Participant";private IActionResult Login()=>RedirectToAction("Login","Account");
 public async Task<IActionResult> Index(){if(!Allowed())return Login();ViewBag.Enrolments=await api.Get("api/enrolments/me");return View();}
 public async Task<IActionResult> Enrolments(){if(!Allowed())return Login();ViewBag.Enrolments=await api.Get("api/enrolments/me");return View();}
 public async Task<IActionResult> Results(){if(!Allowed())return Login();ViewBag.Results=await api.Get("api/results/me");return View();}
 [HttpPost] public async Task<IActionResult> Enrol(int eventId,int categoryId){if(!Allowed())return Login();var r=await api.Post("api/enrolments",new{EventId=eventId,CategoryId=categoryId});TempData["Message"]=r.ok?"Your enrolment has been recorded.":r.message;return RedirectToAction("Details","Home",new{id=eventId});}
 [HttpGet] public async Task<IActionResult> Profile(){if(!Allowed())return Login();var p=await api.Get("api/users/me");if(p is null)return View(new ProfileViewModel());return View(new ProfileViewModel{FirstName=p.Value.GetProperty("firstName").GetString()??"",LastName=p.Value.GetProperty("lastName").GetString()??"",Phone=p.Value.TryGetProperty("phone",out var ph)?ph.GetString():null,Email=p.Value.GetProperty("email").GetString(),ProfilePictureUrl=p.Value.TryGetProperty("profilePictureUrl",out var pic)?pic.GetString():null});}
 [HttpPost] public async Task<IActionResult> Profile(ProfileViewModel m){if(!Allowed())return Login();if(!ModelState.IsValid)return View(m);var r=await api.Put("api/users/me",new{m.FirstName,m.LastName,m.Phone});if(!r.ok){ModelState.AddModelError("",r.message);return View(m);}if(m.ProfilePicture is not null && m.ProfilePicture.Length>0){var upload=await api.UploadFile("api/users/me/profile-picture",m.ProfilePicture);if(!upload.ok){TempData["Message"]="Profile updated, but picture upload failed: "+upload.message;return RedirectToAction(nameof(Profile));}}TempData["Message"]="Profile updated successfully.";return RedirectToAction(nameof(Profile));}
}
