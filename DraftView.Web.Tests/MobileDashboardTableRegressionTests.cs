using Xunit;

namespace DraftView.Web.Tests;

public class MobileDashboardTableRegressionTests
{
    [Fact]
    public void MobileStyles_DoNotApplyGenericLastCellFlexLayoutToAllTables()
    {
        var solutionRoot = GetSolutionRoot();
        var mobileCssPath = Path.Combine(solutionRoot, "DraftView.Web", "wwwroot", "css", "DraftView.Mobile.css");
        var dashboardViewPath = Path.Combine(solutionRoot, "DraftView.Web", "Views", "Author", "Dashboard.cshtml");

        var mobileCss = File.ReadAllText(mobileCssPath);
        var dashboardView = File.ReadAllText(dashboardViewPath);

        Assert.DoesNotContain("\n        td:last-child {", mobileCss, StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n        td:last-child {", mobileCss, StringComparison.Ordinal);
        Assert.Contains(".dashboard-table__actions {", mobileCss, StringComparison.Ordinal);
        Assert.Contains("class=\"dashboard-table__actions\"", dashboardView, StringComparison.Ordinal);
    }

    private static string GetSolutionRoot()
    {
        var dir = Directory.GetCurrentDirectory();

        while (dir != null &&
               !Directory.GetFiles(dir, "*.sln").Any() &&
               !Directory.GetFiles(dir, "*.slnx").Any())
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        if (dir == null)
            throw new InvalidOperationException("Solution root not found (.sln or .slnx).");

        return dir;
    }
}
