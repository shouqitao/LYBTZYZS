# LYBT.UI.WPF

Desktop WPF client that consumes the backend modules via the Web API.
It is built with Prism and Material Design in XAML. The application boots via
`PrismApplication` and shows a `ShellView` containing a drawer based navigation.
At startup the application now displays a modal `LoginWindow` to authenticate
the user. After a successful login the main `ShellView` is launched. The shell
contains a left drawer with navigation buttons that are generated according to
the roles of the logged in user.

