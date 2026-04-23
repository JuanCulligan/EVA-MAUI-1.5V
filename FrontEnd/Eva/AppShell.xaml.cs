namespace Eva
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(ActivateAccountPage), typeof(ActivateAccountPage));
            Routing.RegisterRoute(nameof(TermsPage), typeof(TermsPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
            Routing.RegisterRoute(nameof(MyVehiclePage), typeof(MyVehiclePage));
            Routing.RegisterRoute(nameof(SavedPlacesPage), typeof(SavedPlacesPage));
            Routing.RegisterRoute(nameof(ReportBugPage), typeof(ReportBugPage));
            Routing.RegisterRoute(nameof(ViajesHistoryPage), typeof(ViajesHistoryPage));
            Routing.RegisterRoute(nameof(ConsumosHistoryPage), typeof(ConsumosHistoryPage));
        }
    }
}
