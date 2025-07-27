using Spectre.Console;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Experimental
{
    public class SpectreConsoleTests : BaseTest
    {
        public SpectreConsoleTests(ITestOutputHelper output) : base(output)
        {
        }

        Tree BuildTree()
        {
            // Create the tree
            var tree = new Tree("Root")
                .Style(Style.Parse("red"))
                .Guide(TreeGuide.Line);

            // Add some nodes
            var foo = tree.AddNode("[yellow]Foo[/]");
            var table = foo.AddNode(new Table()
                .RoundedBorder()
                .AddColumn("First")
                .AddColumn("Second")
                .AddRow("1", "2")
                .AddRow("3", "4")
                .AddRow("5", "6"));

            table.AddNode("[blue]Baz[/]");
            foo.AddNode("Qux");

            var bar = tree.AddNode("[yellow]Bar[/]");
            bar.AddNode(new Calendar(2020, 12)
                .AddCalendarEvent(2020, 12, 12)
                .HideHeader());

            // Return the tree
            return tree;
        }
        [Fact]
        public void Test1()
        {
            _output.WriteLine(BuildTree().RenderToStringBuilder().ToString());
        }
    }
}