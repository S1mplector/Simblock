using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Application.Services;
using SimBlock.Core.Domain.Interfaces;
using SimBlock.Infrastructure.Windows;
using SimBlock.Presentation.Forms;
using System.Windows.Forms;

namespace SimBlock
{
    public class Program
    {
        [STAThread]
        public static async Task Main(string[] args)
        {
            // Enable Windows Forms visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Build the host
            var host = CreateHostBuilder(args).Build();

            // Get the main service and initialize
            var keyboardBlockerService = host.Services.GetRequiredService<IKeyboardBlockerService>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting SimBlock application...");

                // Initialize the keyboard blocker service
                await keyboardBlockerService.InitializeAsync();

                // Create and show the main form
                var mainForm = host.Services.GetRequiredService<MainForm>();
                
                // Ensure the form is visible
                mainForm.Show();
                mainForm.BringToFront();
                mainForm.Activate();
                
                // Start the application message loop
                Application.Run(mainForm);

                logger.LogInformation("Application shutting down...");
                await keyboardBlockerService.ShutdownAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Fatal error occurred");
                MessageBox.Show($"Fatal error: {ex.Message}", "SimBlock Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register domain services
                    services.AddSingleton<IKeyboardHookService, WindowsKeyboardHookService>();
                    services.AddSingleton<ISystemTrayService, WindowsSystemTrayService>();

                    // Register application services
                    services.AddSingleton<IKeyboardBlockerService, KeyboardBlockerService>();

                    // Register presentation layer
                    services.AddTransient<MainForm>();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                });
    }
}
