# LYBT.UI.WPF

Desktop WPF client that consumes the backend modules via the Web API.
It is built with Prism and Material Design in XAML. The application boots via
`PrismApplication` and shows a `ShellView` containing a drawer based navigation.
At startup the application creates the main `ShellView` and loads a `LoginView`
into its content region. After successful authentication the navigation drawer
is built according to the roles of the logged in user.

For detailed guidance on the MVVM structure and the login flow, see
[docs/DevelopmentGuide.md](docs/DevelopmentGuide.md).
