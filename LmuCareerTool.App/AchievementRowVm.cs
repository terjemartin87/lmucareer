using System.Windows;
using System.Windows.Media;
using LmuCareerTool.Content;

namespace LmuCareerTool.App;

public class AchievementRowVm
{
    public AchievementRowVm(AchievementDefinition definition, bool unlocked)
    {
        Icon = unlocked ? definition.Icon : "🔒";
        Name = definition.Name;
        Description = definition.Description;
        BadgeText = unlocked ? "LÅST OPP" : "LÅST";

        var badgeColorKey = unlocked ? "GoodColor" : "MutedTextColor";
        BadgeColor = (Brush)Application.Current.FindResource(badgeColorKey);

        NameOpacity = unlocked ? 1.0 : 0.5;
    }

    public string Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public string BadgeText { get; }
    public Brush BadgeColor { get; }
    public double NameOpacity { get; }
}
