using MyBooks.TenantService.Models;

namespace MyBooks.TenantService.Dtos
{
    public class TenantReadDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }

        // integration flags
        public bool AllowExternalIntegrations { get; set; } = false;

        public int MaxStorageMb { get; set; }
    }
}
