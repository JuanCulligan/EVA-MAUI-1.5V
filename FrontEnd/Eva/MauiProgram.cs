using Microsoft.Extensions.Logging;
#if ANDROID
using Android.OS;
using Android.Views;
using Android.Webkit;
using Eva.Platforms.Android;
using Microsoft.Maui.Handlers;
#endif

namespace Eva
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                  
                });
#if ANDROID
            WebViewHandler.Mapper.AppendToMapping(nameof(GeolocationWebChromeClient), (handler, _) =>
            {
                if (handler.PlatformView is Android.Webkit.WebView webView)
                {
                    webView.Settings.JavaScriptEnabled = true;
                    webView.Settings.SetGeolocationEnabled(true);
                    webView.Settings.DomStorageEnabled = true;
                    webView.Settings.CacheMode = Android.Webkit.CacheModes.NoCache;
                    webView.SetWebChromeClient(new GeolocationWebChromeClient());
                    webView.AddJavascriptInterface(new EvaJavascriptInterface(), "EvaAndroid");
                }
            });
            WebViewHandler.Mapper.AppendToMapping(nameof(EvaWebViewClient), (handler, _) =>
            {
                if (handler.PlatformView is not Android.Webkit.WebView webView)
                    return;
                var h = new Handler(Looper.MainLooper!);
                h.PostDelayed(() =>
                {
                    var inner = webView.WebViewClient;
                    if (inner is EvaWebViewClient)
                        return;
                    webView.SetWebViewClient(new EvaWebViewClient(inner));
                }, 200);
            });
            WebViewHandler.Mapper.AppendToMapping("EvaWebViewFullBleed", (handler, _) =>
            {
                if (handler.PlatformView is not Android.Webkit.WebView webView)
                    return;
                webView.Post(() =>
                {
                    try
                    {
                        webView.SetPadding(0, 0, 0, 0);
                        Android.Views.View? cur = webView;
                        for (var i = 0; i < 22 && cur != null; i++)
                        {
                            if (cur is ViewGroup vg)
                            {
                                vg.SetPadding(0, 0, 0, 0);
                            }

                            cur = cur.Parent as Android.Views.View;
                        }
                    }
                    catch
                    {
                    }
                });
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
