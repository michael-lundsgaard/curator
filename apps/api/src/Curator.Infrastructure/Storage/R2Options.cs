namespace Curator.Infrastructure.Storage;

/// <summary>Cloudflare R2 bucket settings (section <c>R2</c>). Keep the keys in user secrets.</summary>
public sealed class R2Options
{
    public const string SectionName = "R2";

    public string AccountId { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
}
