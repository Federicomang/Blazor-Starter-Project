using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterProject.Database.Entities
{
    [Table("AuditLogDetail", Schema = "History")]
    public class AuditLogDetail
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long AuditLogId { get; set; }

        [Required]
        [MaxLength(128)]
        public string FieldName { get; set; } = default!;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [ForeignKey(nameof(AuditLogId))]
        public AuditLog AuditLog { get; set; } = default!;
    }
}
