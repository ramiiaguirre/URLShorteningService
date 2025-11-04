public class URLShorted()
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int? Clicks { get; set; }
}
