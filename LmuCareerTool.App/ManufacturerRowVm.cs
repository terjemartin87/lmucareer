using System.ComponentModel;
using System.Runtime.CompilerServices;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

public class ManufacturerRowVm : INotifyPropertyChanged
{
    private bool _isSelected;

    public ManufacturerRowVm(ManufacturerStatus status, bool canAfford)
    {
        Name = status.Name;
        Unlocked = status.Unlocked;
        StatusText = status.Unlocked
            ? "Opplåst"
            : $"Krever Rating {status.RatingRequired}";
        BuyText = status.Unlocked ? "" : $"Kjøp for {status.UnlockCost} cr";
        UnlockCost = status.UnlockCost;
        CanAfford = canAfford;
    }

    public string Name { get; }
    public bool Unlocked { get; }
    public string StatusText { get; }
    public string BuyText { get; }
    public int UnlockCost { get; }
    public bool CanAfford { get; }
    public bool ShowBuyButton => !Unlocked;

    /// <summary>Bindes TwoWay mot RadioButton.IsChecked, slik at valget faktisk vises i UI-en.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
