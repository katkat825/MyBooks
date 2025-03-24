namespace MyBooks.Common.BaseClasses
{
    public abstract class AuditableEntity
    {
        public string CreatedBy { get; set; } = "System";
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }
}
