namespace MyBooks.Common.Dtos;

public class OwnerUserDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string? Role { get; set; }
}