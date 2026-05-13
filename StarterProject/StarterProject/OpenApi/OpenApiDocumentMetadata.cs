namespace StarterProject.OpenApi
{
    public sealed class OpenApiDocumentMetadata(params string[] documentNames)
    {
        public IReadOnlySet<string> DocumentNames { get; } = documentNames
            .Where(documentName => !string.IsNullOrWhiteSpace(documentName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
