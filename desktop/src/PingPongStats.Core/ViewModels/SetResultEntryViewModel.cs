using CommunityToolkit.Mvvm.ComponentModel;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.ViewModels;

/// <summary>One editable set-score row in the optional set-detail entry list
/// shared by MatchEditViewModel and DoublesViewModel.</summary>
public partial class SetResultEntryViewModel : ObservableObject
{
    [ObservableProperty] private int setNumber;
    [ObservableProperty] private int pointsA;
    [ObservableProperty] private int pointsB;

    public SetResult ToModel() => new() { SetNumber = SetNumber, PointsA = PointsA, PointsB = PointsB };

    public static SetResultEntryViewModel FromModel(SetResult set) => new()
    {
        SetNumber = set.SetNumber,
        PointsA = set.PointsA,
        PointsB = set.PointsB,
    };
}
