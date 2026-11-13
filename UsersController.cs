using System.Security.Claims; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.API.Data; using RaceDay.API.DTOs;
namespace RaceDay.API.Controllers;
[ApiController,Route("api/users")]
public class UsersController(RaceDayContext db):ControllerBase
{
 /// <summary>Returns the authenticated user profile.</summary>
 [Authorize,HttpGet("me"),ProducesResponseType(StatusCodes.Status200OK),ProducesResponseType(StatusCodes.Status401Unauthorized)] public async Task<IActionResult> Me(){var id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var u=await db.Users.Include(x=>x.Role).FirstOrDefaultAsync(x=>x.UserId==id);return u is null?NotFound():Ok(new{u.UserId,u.Email,u.FirstName,u.LastName,u.Phone,u.ProfilePictureUrl,u.CreatedAt,role=u.Role!.RoleName});}
 /// <summary>Updates the authenticated user first name, last name and phone number.</summary>
 [Authorize,HttpPut("me"),ProducesResponseType(StatusCodes.Status200OK),ProducesResponseType(StatusCodes.Status401Unauthorized)] public async Task<IActionResult> Update(UpdateProfileRequest request){var id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var u=await db.Users.FindAsync(id);if(u is null)return NotFound();u.FirstName=request.FirstName.Trim();u.LastName=request.LastName.Trim();u.Phone=request.Phone;await db.SaveChangesAsync();return Ok(new{u.UserId,u.Email,u.FirstName,u.LastName,u.Phone});}
}