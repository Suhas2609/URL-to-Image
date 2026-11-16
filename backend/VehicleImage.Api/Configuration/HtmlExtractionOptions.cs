using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class HtmlExtractionOptions
{
    public const string SectionName = "HtmlExtraction";

    [Range(1, 10)]
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

    public int OgImageWeight { get; set; } = 100;
    public int TwitterImageWeight { get; set; } = 90;
    public int SrcsetWeight { get; set; } = 70;
    public int DataSrcWeight { get; set; } = 65;
    public int ImgSrcWeight { get; set; } = 60;
    public int VehicleKeywordBonus { get; set; } = 25;
    public int LargeDimensionBonus { get; set; } = 20;
    public int BoilerplateContainerPenalty { get; set; } = 50;
}
