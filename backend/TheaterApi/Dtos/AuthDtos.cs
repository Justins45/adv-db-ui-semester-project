using System.ComponentModel.DataAnnotations;

namespace TheaterApi.Dtos;
public sealed record RegisterRequest([Required, StringLength(100)] string FirstName, [Required, StringLength(100)] string LastName, [Required, EmailAddress] string Email, [Required, StringLength(30)] string Phone, [Required, MinLength(8)] string Password);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record ResetRequest([Required, EmailAddress] string Email);
public sealed record ResetConfirmRequest([Required, EmailAddress] string Email, [Required] string Token, [Required, MinLength(8)] string NewPassword);
public sealed record AuthResponse(int Id, string Email, string Role, string Token);
