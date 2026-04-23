namespace WebHS.Services
{
    public interface ISeoService
    {
        Task<string> GenerateMetaTitleAsync(string pageType, object? data = null);
        Task<string> GenerateMetaDescriptionAsync(string pageType, object? data = null);
        Task<string> GenerateMetaKeywordsAsync(string pageType, object? data = null);
        Task<string> GenerateCanonicalUrlAsync(string pageType, object? data = null);
        Task<Dictionary<string, string>> GenerateStructuredDataAsync(string pageType, object? data = null);
    }
}
