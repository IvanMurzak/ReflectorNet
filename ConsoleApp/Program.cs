using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Set up the logger
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();
                    builder.AddConsole(options =>
                    {
                        options.FormatterName = nameof(CustomConsoleFormatter);
                    });
                    builder.SetMinimumLevel(LogLevel.Debug);
                })
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()!.CreateLogger<Program>();

            await Tester.Test(logger);
        }
    }
}