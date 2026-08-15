using S3Explorer.ViewModels;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises the navigation logic extracted from <see cref="MainWindowViewModel"/>:
/// <c>BuildBreadcrumbs</c> (the breadcrumb trail) and <c>ComputeParentPrefix</c>
/// (the "go up" computation). (Internal, visible via <c>InternalsVisibleTo</c>.)
/// </summary>
public static class NavigationSuite
{
    private const string Id = "Navigation";

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "MainWindowViewModel navigation",
        cases: new List<TestCaseDescriptor>
        {
            // BuildBreadcrumbs -------------------------------------------------

            Case(Id, "crumbs-null-bucket", "No bucket -> empty breadcrumb trail", () =>
                Check.Empty(MainWindowViewModel.BuildBreadcrumbs(null, ""))),

            Case(Id, "crumbs-root", "Bucket at root -> single root segment", () =>
            {
                var crumbs = MainWindowViewModel.BuildBreadcrumbs("mybucket", "");
                Check.Count(1, crumbs);
                Check.Equal("mybucket", crumbs[0].Label);
                Check.Equal("", crumbs[0].Prefix);
            }),

            Case(Id, "crumbs-empty-bucket-name", "Empty bucket name still yields a root segment", () =>
            {
                var crumbs = MainWindowViewModel.BuildBreadcrumbs("", "");
                Check.Count(1, crumbs);
                Check.Equal("", crumbs[0].Label);
            }),

            Case(Id, "crumbs-one-level", "One prefix level adds one segment", () =>
            {
                var crumbs = MainWindowViewModel.BuildBreadcrumbs("b", "docs/");
                Check.Count(2, crumbs);
                Check.Equal("b", crumbs[0].Label);
                Check.Equal("", crumbs[0].Prefix);
                Check.Equal("docs", crumbs[1].Label);
                Check.Equal("docs/", crumbs[1].Prefix);
            }),

            Case(Id, "crumbs-multi-level", "Multi-level prefix accumulates prefixes", () =>
            {
                var crumbs = MainWindowViewModel.BuildBreadcrumbs("b", "a/b/c/");
                Check.Count(4, crumbs);
                Check.Equal("b", crumbs[0].Label);
                Check.Equal("", crumbs[0].Prefix);
                Check.Equal("a", crumbs[1].Label);
                Check.Equal("a/", crumbs[1].Prefix);
                Check.Equal("b", crumbs[2].Label);
                Check.Equal("a/b/", crumbs[2].Prefix);
                Check.Equal("c", crumbs[3].Label);
                Check.Equal("a/b/c/", crumbs[3].Prefix);
            }),

            Case(Id, "crumbs-no-trailing-slash", "Prefix without trailing slash still splits", () =>
            {
                var crumbs = MainWindowViewModel.BuildBreadcrumbs("b", "a/b");
                Check.Count(3, crumbs);
                Check.Equal("a", crumbs[1].Label);
                Check.Equal("a/", crumbs[1].Prefix);
                Check.Equal("b", crumbs[2].Label);
                Check.Equal("a/b/", crumbs[2].Prefix);
            }),

            // ComputeParentPrefix ---------------------------------------------

            Case(Id, "parent-of-empty", "Parent of root is root", () =>
                Check.Equal("", MainWindowViewModel.ComputeParentPrefix(""))),

            Case(Id, "parent-single-level", "Parent of 'a/' is root", () =>
                Check.Equal("", MainWindowViewModel.ComputeParentPrefix("a/"))),

            Case(Id, "parent-two-levels", "Parent of 'a/b/' is 'a/'", () =>
                Check.Equal("a/", MainWindowViewModel.ComputeParentPrefix("a/b/"))),

            Case(Id, "parent-three-levels", "Parent of 'a/b/c/' is 'a/b/'", () =>
                Check.Equal("a/b/", MainWindowViewModel.ComputeParentPrefix("a/b/c/"))),

            Case(Id, "parent-no-trailing-slash", "Parent of 'a/b' is 'a/'", () =>
                Check.Equal("a/", MainWindowViewModel.ComputeParentPrefix("a/b"))),

            Case(Id, "parent-single-segment", "Parent of single segment is root", () =>
                Check.Equal("", MainWindowViewModel.ComputeParentPrefix("a"))),

            Case(Id, "parent-bare-slash", "Parent of '/' is root", () =>
                Check.Equal("", MainWindowViewModel.ComputeParentPrefix("/"))),
        });
}
