namespace PingPongStats.App;

/// <summary>Maps the persisted UiScale setting ("Small"/"Medium"/"Large") to an
/// actual LayoutTransform scale factor applied to MainWindow's root content.</summary>
public static class UiScaleManager
{
    public const double SmallFactor = 1.0;
    public const double MediumFactor = 1.15;
    public const double LargeFactor = 1.3;

    public static double ToScaleFactor(string? uiScale) => uiScale switch
    {
        "Small" => SmallFactor,
        "Large" => LargeFactor,
        _ => MediumFactor,
    };
}
