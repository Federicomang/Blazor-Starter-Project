namespace StarterProject.Database
{
    public interface IAuthEntity
    {
        public string AuthIdentifier { get; }

        public string? Id { get; }
    }
}
