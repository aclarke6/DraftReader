namespace DraftView.Web.Middleware;

/// <summary>
/// Rewrites the controller route value for Reader requests so that mobile
/// clients are served by MobileReaderController and desktop clients by
/// DesktopReaderController. The URL remains identical for both surfaces.
///
/// Must be registered after UseRouting so that route values are available.
/// </summary>
public class MobileDetectionMiddleware(RequestDelegate next)
{
    private static readonly string[] MobileKeywords =
    [
        "Mobile", "Android", "iPhone", "iPad", "iPod",
        "BlackBerry", "IEMobile", "Opera Mini", "webOS"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var routeValues = context.Request.RouteValues;

        if (routeValues.TryGetValue("controller", out var controllerObj)
            && controllerObj is string controller
            && controller.Equals("Reader", StringComparison.OrdinalIgnoreCase))
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var isMobile  = IsMobileUserAgent(userAgent);

            routeValues["controller"] = isMobile ? "MobileReader" : "DesktopReader";
        }

        await next(context);
    }

    private static bool IsMobileUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return false;

        foreach (var keyword in MobileKeywords)
        {
            if (userAgent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}