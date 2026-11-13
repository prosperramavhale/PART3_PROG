using System.ComponentModel.DataAnnotations;
namespace RaceDay.MVC.Models;
public class CategoryViewModel { public int EventId {get;set;} [Required] public string Name {get;set;}=""; [Range(0.1,500)] public decimal DistanceKm {get;set;} public int? MaxParticipants {get;set;} [Range(0,100000)] public decimal EntryFee {get;set;} }
public class StatusViewModel { public int EnrolmentId {get;set;} [Required] public string Status {get;set;}="Pending"; }
public class ResultViewModel { public int EnrolmentId {get;set;} public TimeSpan? FinishTime {get;set;} [Range(1,int.MaxValue)] public int? Position {get;set;} public string? Notes {get;set;} }
public class ProfileViewModel { [Required] public string FirstName {get;set;}=""; [Required] public string LastName {get;set;}=""; public string? Phone {get;set;} public string? Email {get;set;} public string? ProfilePictureUrl {get;set;} public IFormFile? ProfilePicture {get;set;} }
