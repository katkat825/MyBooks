namespace MyBooks.AuthService.Dtos
{
    public class UpdateProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool AcceptedAup {  get; set; }
        public DateTime LastAcceptedAup { get; set; }
    }
}
