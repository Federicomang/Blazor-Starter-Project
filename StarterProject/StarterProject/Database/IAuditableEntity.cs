namespace StarterProject.Database
{
    public interface IAuditableEntity
    {
        public string CreatedBy { get; set; }

        public string TableThatCreated { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? LastModifiedBy { get; set; }

        public string? TableThatModified { get; set; }

        public DateTime? LastModifiedOn { get; set; }
    }
}
