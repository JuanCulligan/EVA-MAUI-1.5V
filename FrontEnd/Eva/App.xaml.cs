using Eva.Services;

namespace Eva
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnStart()
        {
            base.OnStart();
            _ = SesionStore.TryAutoNavigateToMapAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();
            SesionStore.TouchLastActivityIfLoggedIn();
        }
    }
}
