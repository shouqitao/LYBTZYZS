using LYBT.Module.Users.Dtos;
using LYBT.UI.WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using Refit;

namespace LYBT.UI.WPF.ViewModels {
    public class AdminViewModel : BindableBase, INavigationAware {


        public ObservableCollection<UserDto> Users { get; } = new();

        public DelegateCommand AddUserCommand { get; }
        public DelegateCommand<UserDto?> EditUserCommand { get; }
        public DelegateCommand<UserDto?> ToggleUserStatusCommand { get; }



        private async void LoadUsers() {
        }

        private async void AddUser() {

        }

        private async void EditUser(UserDto? user) {

        }

        private async void ToggleUserStatus(UserDto? user) {

        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            LoadUsers();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}
