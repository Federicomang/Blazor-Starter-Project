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
        [MaxLength(128)] // sysname in SQL Server è max 128
        public string EntityName { get; set; } = default!;

        [Required]
        [MaxLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string EntityId { get; set; } = default!;

        [Required]
        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string Action { get; set; } = default!; // INSERT / UPDATE / DELETE

        [Required]
        [Column(TypeName = "datetime2(3)")]
        public DateTime ChangedOn { get; set; } // default in DB: sysutcdatetime()

        [MaxLength(128)]
        [Column(TypeName = "varchar(128)")]
        public string? ChangedBy { get; set; }

        [MaxLength(128)]
        [Column(TypeName = "varchar(128)")]
        public string? HostName { get; set; }

        [MaxLength(128)]
        [Column(TypeName = "varchar(128)")]
        public string? AppName { get; set; }

        // Navigation
        public virtual ICollection<AuditLogDetail> Details { get; set; }
    }
}
