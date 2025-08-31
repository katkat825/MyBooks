using MyBooks.TenantService.Models;

namespace MyBooks.TenantService.Dtos
{
    public class TenantReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public bool IsActive { get; set; }

        // integration flags
        public bool AllowExternalIntegrations { get; set; }

        public bool AllowStorage { get; set; }
        public int MaxStorageMb { get; set; }
        
        public int MaxUsers { get; set; }
    }
}
