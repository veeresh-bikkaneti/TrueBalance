namespace Infrastructure.Options;

public class UploadOptions
{
    public const string SectionName = "Uploads";

    public string Directory { get; set; } = "data/user-assets";

    public long MaxBytes { get; set; } = 20 * 1024 * 1024;

    public List<string> AllowedExtensions { get; set; } =
        new() { ".csv", ".ofx", ".qfx", ".pdf", ".png", ".jpg", ".jpeg" };

    public List<string> AllowedContentTypes { get; set; } =
        new()
        {
            "text/csv",
            "application/ofx",
            "application/x-ofx",
            "application/qfx",
            "application/pdf",
            "image/png",
            "image/jpeg"
        };
}
