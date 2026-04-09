namespace DesktopSearchApp.Models;

public sealed class SearchRequest
{
    public string QueryText { get; set; } = "";
    public List<string> AllowedExtensions { get; set; } = new();
    public SearchScope Scope { get; set; } = SearchScope.AllFields;
}
