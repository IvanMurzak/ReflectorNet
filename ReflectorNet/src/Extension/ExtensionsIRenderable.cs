using System.IO;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsIRenderable
    {
        public static StringBuilder RenderToStringBuilder(this IRenderable renderable, bool useAnsi = false, StringBuilder? stringBuilder = null)
        {
            stringBuilder ??= new StringBuilder();

            using (var writer = new StringWriter(stringBuilder))
            {
                var settings = new AnsiConsoleSettings
                {
                    Out = new AnsiConsoleOutput(writer),
                    Ansi = useAnsi ? AnsiSupport.Yes : AnsiSupport.No
                };

                AnsiConsole.Create(settings)
                    .Write(renderable);
            }

            return stringBuilder;
        }
    }
}