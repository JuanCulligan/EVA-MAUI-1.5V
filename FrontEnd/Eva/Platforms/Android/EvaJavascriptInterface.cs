using Android.Webkit;
using AndroidX.Core.View;
using Microsoft.Maui.ApplicationModel;

namespace Eva.Platforms.Android;

public sealed class EvaJavascriptInterface : Java.Lang.Object
{
    [global::Android.Webkit.JavascriptInterface]
    public void Open(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var p = path.Trim();
        var url = p.Contains("://", StringComparison.Ordinal)
            ? p
            : "eva://" + p.TrimStart('/');
        MainThread.BeginInvokeOnMainThread(() => MapWebBridge.RaiseNavigate(url));
    }

    [global::Android.Webkit.JavascriptInterface]
    public string GetSystemBarInsetsDp()
    {
        try
        {
            var act = Platform.CurrentActivity;
            if (act?.Window?.DecorView == null)
            {
                return """{"top":0,"bottom":0,"left":0,"right":0}""";
            }

            var wi = ViewCompat.GetRootWindowInsets(act.Window.DecorView);
            if (wi == null)
            {
                return """{"top":0,"bottom":0,"left":0,"right":0}""";
            }

            var bars = wi.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var cut = wi.DisplayCutout;
            float d = act.Resources?.DisplayMetrics?.Density ?? 1f;
            if (d <= 0.01f)
            {
                d = 1f;
            }

            static int ToCssPx(int px, float density) =>
                (int)System.Math.Ceiling(px / density);

            int topPx = 0, botPx = 0, leftPx = 0, rightPx = 0;
            if (bars != null)
            {
                topPx = bars.Top;
                botPx = bars.Bottom;
                leftPx = bars.Left;
                rightPx = bars.Right;
            }
            if (cut != null)
            {
                topPx = System.Math.Max(topPx, cut.SafeInsetTop);
                botPx = System.Math.Max(botPx, cut.SafeInsetBottom);
                leftPx = System.Math.Max(leftPx, cut.SafeInsetLeft);
                rightPx = System.Math.Max(rightPx, cut.SafeInsetRight);
            }

            if (topPx <= 0 && act.Resources != null)
            {
                int id = act.Resources.GetIdentifier("status_bar_height", "dimen", "android");
                if (id > 0)
                {
                    topPx = act.Resources.GetDimensionPixelSize(id);
                }
            }

            int top = ToCssPx(topPx, d);
            int bottom = ToCssPx(botPx, d);
            int left = ToCssPx(leftPx, d);
            int right = ToCssPx(rightPx, d);
            try { global::Android.Util.Log.Info("EVA-INSETS", $"top={top} bottom={bottom} left={left} right={right} (pxCss)"); } catch { }
            return $$"""{"top":{{top}},"bottom":{{bottom}},"left":{{left}},"right":{{right}}}""";
        }
        catch
        {
            return """{"top":0,"bottom":0,"left":0,"right":0}""";
        }
    }
}
