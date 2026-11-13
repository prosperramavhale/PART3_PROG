using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class ImagesController(RaceDayContext db, IBlobStorageService storage) : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    /// <summary>Uploads or replaces the banner image for an event owned by the authenticated Organiser.</summary>
    [Authorize(Roles = "Organiser")]
    [HttpPost("events/{eventId:int}/banner")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<IActionResult> UploadEventBanner(int eventId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose an image to upload." });
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var raceEvent = await db.Events.FirstOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new { message = "Event not found." });
        if (raceEvent.OrganiserId != organiserId) return Forbid();
        try
        {
            raceEvent.BannerImageUrl = await storage.UploadAsync(file, "event-banners", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Event banner uploaded.", imageUrl = raceEvent.BannerImageUrl });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception) { return StatusCode(503, new { message = "Image storage is unavailable. Check the Azure Blob Storage configuration." }); }
    }

    /// <summary>Uploads or replaces the authenticated user's profile picture.</summary>
    [Authorize]
    [HttpPost("users/me/profile-picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose an image to upload." });
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        try
        {
            user.ProfilePictureUrl = await storage.UploadAsync(file, "profile-pictures", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Profile picture uploaded.", imageUrl = user.ProfilePictureUrl });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception) { return StatusCode(503, new { message = "Image storage is unavailable. Check the Azure Blob Storage configuration." }); }
    }
}
