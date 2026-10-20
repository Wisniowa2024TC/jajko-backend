using System.ComponentModel.DataAnnotations;

namespace JajkoBackend.Models.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required, MaxLength(64)] string Name,
    [Required, MaxLength(64)] string Surname);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    bool RememberMe = false);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword);

public record UserDto(int Id, string Email, string Name, string Surname);

public record LoginResponse(string AccessToken, DateTime ExpiresAt, string TokenType = "Bearer");
