using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterProject.Database.Entities // cambia namespace come preferisci
{
    [Table("AuditLog", Schema = "History")]
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string EntityName { get; set; } = default!;

        [Required]
        [MaxLength(64)]
        public string EntityId { get; set; } = default!;

        [Required]
        [MaxLength(10)]
        public string Action { get; set; } = default!; // INSERT / UPDATE / DELETE

        [Required]
        public DateTime ChangedOn { get; set; }

        public string? ChangedBy { get; set; }

        public string? TableThatChanged { get; set; }

        public string? HostName { get; set; }

        public string? AppName { get; set; }

        public virtual ICollection<AuditLogDetail> Details { get; set; }
    }
}
