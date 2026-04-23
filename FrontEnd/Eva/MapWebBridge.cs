namespace Eva;

public static class MapWebBridge
{
    public static event Action<string>? NavigateRequested;

    public static void RaiseNavigate(string url) => NavigateRequested?.Invoke(url);
}
