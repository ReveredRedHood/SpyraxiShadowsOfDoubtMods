using SOD.Common.BepInEx.Configuration;

namespace HoursOfOperation;

public interface IConfigBindings
{
    [Binding(
        true,
        "If true, only show the opening hours if you've previously visited the business."
    )]
    bool OnlyShowIfPreviouslyVisited { get; set; }

    [Binding(
        true,
        "If true while OnlyShowIfPreviouslyVisited is true, then show the opening hours for all businesses regardless of whether you visited them while the ElGen-Beauty sync disk is installed."
    )]
    bool ShowAllIfBeautyInstalled { get; set; }
}
