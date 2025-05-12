using System.ComponentModel.DataAnnotations;

namespace QuickLearn.BE.Models;

public class GoogleLoginRequest
{
    [Required]
    public string Credential { get; set; } = string.Empty;
    
    [Required]
    public string ClientId { get; set; } = string.Empty;
} 