namespace StarterProject.Client.Features.Identity.Models
{
    public class UserInfoNoId
    {
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public IEnumerable<string> Roles { get; set; } = [];

        public static UserInfoNoId Empty => new()
        {
            Name = string.Empty,
            Surname = string.Empty,
            Email= string.Empty
        };

        public string FullName => $"{Name} {Surname}";
    }

    public class UserInfo : UserInfoNoId
    {
        public required string Id { get; set; }
    }
}
