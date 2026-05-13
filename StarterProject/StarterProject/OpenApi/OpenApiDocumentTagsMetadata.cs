namespace StarterProject.OpenApi
{
    public sealed class OpenApiDocumentTagsMetadata(string documentName, params string[] tags)
    {
        public string DocumentName { get; } = documentName;

        public IReadOnlyList<string> Tags { get; } = [.. tags.Where(tag => !string.IsNullOrWhiteSpace(tag))];
    }
}
