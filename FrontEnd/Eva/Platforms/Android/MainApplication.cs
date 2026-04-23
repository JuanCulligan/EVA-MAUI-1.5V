using System;
using Android.App;
using Android.Runtime;
using Android.Util;

namespace Eva
{
    [Application]
    public class MainApplication : MauiApplication
    {
        const string LogTag = "EVA";

        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            Log.Info(LogTag, "MainApplication.OnCreate begin");

            AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
            {
                Log.Error(LogTag, "AndroidEnvironment unhandled: " + args.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Log.Error(LogTag, "AppDomain unhandled: " + args.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Log.Error(LogTag, "Unobserved task: " + args.Exception);
            };

            try
            {
                base.OnCreate();
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTag, "MainApplication base.OnCreate: " + ex);
                throw;
            }

            Log.Info(LogTag, "MainApplication.OnCreate end");
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
