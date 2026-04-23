using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace Eva
{
    [Activity(Theme = "@style/EvaTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        static void ApplyDarkBars(Activity activity)
        {
            if (activity?.Window == null || Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
                return;
            WindowCompat.SetDecorFitsSystemWindows(activity.Window, false);
            activity.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            var darkColor = Android.Graphics.Color.ParseColor("#0D0D0D");
            activity.Window.SetNavigationBarColor(darkColor);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var flags = activity.Window.DecorView.SystemUiFlags;
                flags &= ~SystemUiFlags.LightStatusBar;
                activity.Window.DecorView.SystemUiFlags = flags;
            }
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            SetTheme(Resource.Style.EvaTheme);
            base.OnCreate(savedInstanceState);
            ApplyDarkBars(this);
            Window?.DecorView?.Post(() => ApplyDarkBars(this));
            new Handler(Looper.MainLooper!).PostDelayed(() => ApplyDarkBars(this), 100);
            new Handler(Looper.MainLooper!).PostDelayed(() => ApplyDarkBars(this), 400);
        }

        protected override void OnResume()
        {
            base.OnResume();
            ApplyDarkBars(this);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
                ApplyDarkBars(this);
        }
    }
}
