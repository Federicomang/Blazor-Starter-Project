namespace StarterProject.Attributes.ExtendedAudit
{
    [Flags]
    public enum AuditTrackType
    {
        Added = 1,
        Modified = 2,
        Deleted = 4
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AuditAttribute : Attribute
    {
        public bool IncludeAllProperties { get; set; } = true;

        public AuditTrackType TrackType { get; set; } = AuditTrackType.Added | AuditTrackType.Modified | AuditTrackType.Deleted;
    }
}
