using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarterProject.Database.Entities
{
    public class Identifier
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public string Id { get; set; }

        public string IdentifierKey { get; set; }

        public string IdentifierId { get; set; }
    }
}
