namespace MyBooks.AuthService.Dtos
{
    public class TenantLookupDto
    {
        public int Id { get; set; }
        public string Subdomain { get; set; }
        public bool IsActive { get; set; }
    }
}