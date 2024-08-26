using System;
using System.ComponentModel.DataAnnotations;

namespace API;

public class RegisterDto
{
    [Required]    
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MinLength(4)]
    public string Password { get; set; } = string.Empty;
}
