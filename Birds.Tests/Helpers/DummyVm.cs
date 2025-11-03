using Birds.UI.Services.Navigation.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.Tests.Helpers
{
    internal sealed class DummyVm : ObservableObject, IAsyncNavigatedTo
    {
        public int Calls { get; private set; }

        public Task OnNavigatedToAsync()
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}