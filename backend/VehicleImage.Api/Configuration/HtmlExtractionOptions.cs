using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class HtmlExtractionOptions
{
    public const string SectionName = "HtmlExtraction";

    [Range(1, 3)]
    public int MaxCandidates { get; set; } = 3;

    [Range(1, 2000)]
    public int MinDeclaredDimension { get; set; } = 80;

    public string[] IgnoreUrlPatterns { get; set; } =
    [
        "logo",
        "icon",
        "favicon",
        "sprite",
        "pixel",
        "tracking",
        "tracker",
        "banner",
        "badge",
        "avatar",
        "spinner",
        "loading",
        "placeholder",
        "1x1",
        "/ads/",
        "ad-",
        "ad_"
    ];

}
