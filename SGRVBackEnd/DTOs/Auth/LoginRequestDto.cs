using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}