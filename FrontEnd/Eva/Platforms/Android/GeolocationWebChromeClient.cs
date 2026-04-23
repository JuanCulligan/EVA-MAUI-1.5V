using Android.Webkit;

namespace Eva.Platforms.Android;

public sealed class GeolocationWebChromeClient : WebChromeClient
{
    public override void OnGeolocationPermissionsShowPrompt(string? origin, GeolocationPermissions.ICallback? callback) =>
        callback?.Invoke(origin, true, false);
}
