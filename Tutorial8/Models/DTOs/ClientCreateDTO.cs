using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;

public class ClientCreateDTO
{
    [Required]
    public string FirstName { get; set; } = null!;
    
    [Required]
    public string LastName { get; set; } = null!;
    
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
    
    public string? Telephone { get; set; }
    
    public string? Pesel { get; set; }
}