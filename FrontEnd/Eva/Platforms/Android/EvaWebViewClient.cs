using Android.Webkit;
using Microsoft.Maui.ApplicationModel;

namespace Eva.Platforms.Android;

public sealed class EvaWebViewClient : WebViewClient
{
    readonly WebViewClient? _inner;

    public EvaWebViewClient(WebViewClient? inner) => _inner = inner;

    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
    {
        var url = request?.Url?.ToString();
        if (IsEvaScheme(url))
        {
            var u = url!;
            MainThread.BeginInvokeOnMainThread(() => MapWebBridge.RaiseNavigate(u));
            return true;
        }

        return _inner?.ShouldOverrideUrlLoading(view!, request!) ?? base.ShouldOverrideUrlLoading(view, request);
    }

#pragma warning disable CA1422
    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view, string? url)
    {
        if (IsEvaScheme(url))
        {
            var u = url!;
            MainThread.BeginInvokeOnMainThread(() => MapWebBridge.RaiseNavigate(u));
            return true;
        }

        return _inner?.ShouldOverrideUrlLoading(view!, url!) ?? base.ShouldOverrideUrlLoading(view, url);
    }
#pragma warning restore CA1422

    static bool IsEvaScheme(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith("eva://", StringComparison.OrdinalIgnoreCase);
}
