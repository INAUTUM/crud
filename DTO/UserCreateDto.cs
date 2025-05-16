using System.ComponentModel.DataAnnotations;

public class UserCreateDto
{
    [Required(ErrorMessage = "Login is required")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$",
        ErrorMessage = "The login should contain only Latin letters, numbers and _")]
    public required string Login { get; set; }
    public required string Password { get; set; }
    public required string Name { get; set; }
    public int Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public bool Admin { get; set; }
}  