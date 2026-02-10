namespace StarterProject.Attributes.ExtendedAudit
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AuditPropAttribute : Attribute
    {
        public bool Exclude { get; set; } = false;
    }
}
