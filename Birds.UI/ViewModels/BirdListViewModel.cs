using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> birds = new();

        [RelayCommand]
        private void Load()
        {
            // Здесь должен быть вызов Application слоя (Mediator → Query)
            // Для примера — просто заглушка:
            Birds = new ObservableCollection<string>
        {
            "Воробей",
            "Щегол",
            "Амадин"
        };
        }
    }
}
