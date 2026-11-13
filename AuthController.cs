using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.API.Data; using RaceDay.API.DTOs; using RaceDay.API.Models; using RaceDay.API.Services;
namespace RaceDay.API.Controllers;
[ApiController,Route("api/auth")]
public class AuthController(RaceDayContext db,IPasswordHasher<User> hasher,ITokenService tokens):ControllerBase
{
 /// <summary>Registers an account with an Organiser or Participant role. Passwords are hashed before storage.</summary>
 [HttpPost("register"),ProducesResponseType(StatusCodes.Status201Created),ProducesResponseType(StatusCodes.Status400BadRequest),ProducesResponseType(StatusCodes.Status409Conflict)]
 public async Task<IActionResult> Register(RegisterRequest request)
 {
  var roleName=request.Role.Trim(); if(roleName is not ("Organiser" or "Participant")) return BadRequest(new{message="Role must be Organiser or Participant."});
  var email=request.Email.Trim().ToLowerInvariant(); if(await db.Users.AnyAsync(x=>x.Email==email)) return Conflict(new{message="An account with this email already exists."});
  var role=await db.Roles.SingleOrDefaultAsync(x=>x.RoleName==roleName); if(role is null) return Problem("Required role has not been seeded.",statusCode:500);
  var user=new User{Email=email,FirstName=request.FirstName.Trim(),LastName=request.LastName.Trim(),Phone=request.Phone,RoleId=role.RoleId}; user.PasswordHash=hasher.HashPassword(user,request.Password); db.Users.Add(user); await db.SaveChangesAsync();
  return Created("/api/users/me",new{user.UserId,user.Email,user.FirstName,user.LastName,user.Phone,Role=role.RoleName,user.CreatedAt});
 }
 /// <summary>Validates credentials and returns a signed JWT bearer token.</summary>
 [HttpPost("login"),ProducesResponseType(StatusCodes.Status200OK),ProducesResponseType(StatusCodes.Status401Unauthorized)]
 public async Task<IActionResult> Login(LoginRequest request)
 {
  var user=await db.Users.Include(x=>x.Role).SingleOrDefaultAsync(x=>x.Email==request.Email.Trim().ToLowerInvariant()); if(user is null || hasher.VerifyHashedPassword(user,user.PasswordHash,request.Password)==PasswordVerificationResult.Failed) return Unauthorized(new{message="Invalid email or password."});
  return Ok(new{token=tokens.CreateToken(user,user.Role!.RoleName),tokenType="Bearer",expiresInMinutes=60,user=new{user.UserId,user.Email,user.FirstName,user.LastName,role=user.Role.RoleName}});
 }
}