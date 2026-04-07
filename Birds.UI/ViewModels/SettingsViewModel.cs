using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Birds.UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IAppPreferencesService _preferences;

        public SettingsViewModel(IAppPreferencesService preferences)
        {
            _preferences = preferences;
            AvailableLanguages = new ReadOnlyCollection<string>(
                new List<string> { "Русский", "English" });

            ReloadFromPreferences();
            _preferences.PropertyChanged += OnPreferencesChanged;
        }

        public ReadOnlyCollection<string> AvailableLanguages { get; }

        [ObservableProperty]
        private string selectedLanguage = AppPreferencesState.DefaultLanguage;

        [ObservableProperty]
        private bool showNotificationBadge = true;

        [ObservableProperty]
        private bool reduceMotion;

        public string LanguageHint =>
            SelectedLanguage == "Русский"
                ? "Пока язык сохраняется как предпочтение и готовит основу под будущую локализацию."
                : "Выбор языка уже сохраняется, а полноценный перевод интерфейса можно будет подключить позже.";

        public string NotificationsHint =>
            ShowNotificationBadge
                ? "Индикатор новых уведомлений будет показываться рядом с кнопкой центра уведомлений."
                : "Красный индикатор скрыт, но сама история уведомлений остаётся доступной.";

        public string MotionHint =>
            ReduceMotion
                ? "Сдержанный режим анимации сохранён как предпочтение и готов для будущих экранов."
                : "Сейчас приложение использует стандартные мягкие анимации интерфейса.";

        [RelayCommand]
        private void ResetPreferences()
        {
            _preferences.ResetToDefaults();
            ReloadFromPreferences();
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            if (_preferences.SelectedLanguage != value)
                _preferences.SelectedLanguage = value;

            OnPropertyChanged(nameof(LanguageHint));
        }

        partial void OnShowNotificationBadgeChanged(bool value)
        {
            if (_preferences.ShowNotificationBadge != value)
                _preferences.ShowNotificationBadge = value;

            OnPropertyChanged(nameof(NotificationsHint));
        }

        partial void OnReduceMotionChanged(bool value)
        {
            if (_preferences.ReduceMotion != value)
                _preferences.ReduceMotion = value;

            OnPropertyChanged(nameof(MotionHint));
        }

        private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IAppPreferencesService.SelectedLanguage)
                or nameof(IAppPreferencesService.ShowNotificationBadge)
                or nameof(IAppPreferencesService.ReduceMotion))
            {
                ReloadFromPreferences();
            }
        }

        private void ReloadFromPreferences()
        {
            SelectedLanguage = _preferences.SelectedLanguage;
            ShowNotificationBadge = _preferences.ShowNotificationBadge;
            ReduceMotion = _preferences.ReduceMotion;

            OnPropertyChanged(nameof(LanguageHint));
            OnPropertyChanged(nameof(NotificationsHint));
            OnPropertyChanged(nameof(MotionHint));
        }
    }
}
