using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Application.Services;
using SimBlock.Core.Domain.Interfaces;
using SimBlock.Infrastructure.Windows;
using SimBlock.Infrastructure.Services;
using SimBlock.Presentation.Forms;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;
using SimBlock.Presentation.Managers;
using SimBlock.Presentation.Services;
using System.Windows.Forms;
using System.Threading;

namespace SimBlock
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Ensure only a single instance runs
            bool createdNew;
            using var mutex = new Mutex(true, "SimBlockSingletonMutex", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("SimBlock is already running.", "SimBlock", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Enable Windows Forms visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Build the host - don't use 'using' to prevent disposal
            var host = CreateHostBuilder(args).Build();

            try
            {
                // Run initialization and show main form
                var splashScreenManager = host.Services.GetRequiredService<ISplashScreenManager>();
                var logger = host.Services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Starting SimBlock application...");

                // Get required services for initialization
                var keyboardBlockerService = host.Services.GetRequiredService<IKeyboardBlockerService>();
                var mouseBlockerService = host.Services.GetRequiredService<IMouseBlockerService>();

                // Create splash form
                var splashForm = host.Services.GetRequiredService<SplashForm>();
                bool initializationSuccessful = false;

                // Set up initialization to run after splash form is shown
                splashForm.Shown += async (sender, e) =>
                {
                    try
                    {
                        // Create progress reporter and wire it up to splash screen
                        var progressReporter = new InitializationProgressReporter();
                        progressReporter.ProgressChanged += (sender2, args) =>
                        {
                            if (splashForm.InvokeRequired)
                            {
                                splashForm.Invoke(new Action(() =>
                                {
                                    splashForm.UpdateProgress(args.Percentage, args.Status);
                                }));
                            }
                            else
                            {
                                splashForm.UpdateProgress(args.Percentage, args.Status);
                            }
                        };

                        // Initialize services
                        await keyboardBlockerService.InitializeAsync(progressReporter);
                        await mouseBlockerService.InitializeAsync(progressReporter);

                        // Small delay to show completion
                        await Task.Delay(500);

                        // Mark initialization as successful
                        initializationSuccessful = true;

                        // Switch to loading application state with spinner
                        splashForm.ShowLoadingApplication();

                        // Small delay to show the loading state
                        await Task.Delay(1000);

                        // Close splash form - this will end the Application.Run() below
                        splashForm.Close();
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "Fatal error during initialization");
                        MessageBox.Show($"Fatal error: {ex.Message}", "SimBlock Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        splashForm.Close();
                    }
                };

                // Start the message loop with the splash screen
                Application.Run(splashForm);

                // Dispose splash form to free resources
                splashForm.Dispose();

                // If initialization was successful, create and run the main form
                if (initializationSuccessful)
                {
                    try
                    {
                        // Get all required services for MainForm after successful initialization
                        var uiSettings = host.Services.GetRequiredService<UISettings>();
                        var statusBarManager = host.Services.GetRequiredService<IStatusBarManager>();
                        var logoManager = host.Services.GetRequiredService<ILogoManager>();
                        var layoutManager = host.Services.GetRequiredService<IUILayoutManager>();
                        var shortcutManager = host.Services.GetRequiredService<IKeyboardShortcutManager>();
                        var resourceMonitor = host.Services.GetRequiredService<IResourceMonitor>();
                        var themeManager = host.Services.GetRequiredService<IThemeManager>();
                        var keyboardInfoService = host.Services.GetRequiredService<IKeyboardInfoService>();
                        var mouseInfoService = host.Services.GetRequiredService<IMouseInfoService>();
                        var visualizationManager = host.Services.GetRequiredService<IBlockingVisualizationManager>();
                        var mainFormLogger = host.Services.GetRequiredService<ILogger<MainForm>>();
                        var systemTrayService = host.Services.GetRequiredService<ISystemTrayService>();

                        // Create MainForm only after successful initialization
                        var mainForm = new MainForm(
                            keyboardBlockerService,
                            mouseBlockerService,
                            mainFormLogger,
                            uiSettings,
                            statusBarManager,
                            logoManager,
                            layoutManager,
                            shortcutManager,
                            resourceMonitor,
                            themeManager,
                            keyboardInfoService,
                            mouseInfoService,
                            host.Services,
                            visualizationManager,
                            systemTrayService
                        );

                        // Run main form
                        Application.Run(mainForm);

                        // Shutdown services after form is closed (avoid sync-over-async deadlocks)
                        keyboardBlockerService.ShutdownAsync().GetAwaiter().GetResult();
                        mouseBlockerService.ShutdownAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error running main form");
                        MessageBox.Show($"Failed to start SimBlock: {ex.Message}", "SimBlock Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogCritical(ex, "Fatal error occurred");
                MessageBox.Show($"Fatal error: {ex.Message}", "SimBlock Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Dispose the host when done
                host.Dispose();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register UI configuration first
                    services.AddSingleton<UISettings>();

                    // Register system tray service
                    services.AddSingleton<ISystemTrayService, WindowsSystemTrayService>();

                    // Register domain services (keyboard first, then mouse with delayed injection)
                    services.AddSingleton<IKeyboardHookService, WindowsKeyboardHookService>();
                    services.AddSingleton<IKeyboardInfoService, WindowsKeyboardInfoService>();
                    services.AddSingleton<IMouseInfoService, WindowsMouseInfoService>();

                    // Register mouse hook service with custom factory to avoid circular dependency
                    services.AddSingleton<IMouseHookService>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<WindowsMouseHookService>>();
                        var uiSettings = provider.GetRequiredService<UISettings>();
                        var keyboardHookService = provider.GetRequiredService<IKeyboardHookService>();
                        return new WindowsMouseHookService(logger, uiSettings, keyboardHookService);
                    });

                    // Register application services
                    services.AddSingleton<IKeyboardBlockerService, KeyboardBlockerService>();
                    services.AddSingleton<IMouseBlockerService, MouseBlockerService>();

                    // Register infrastructure services
                    services.AddSingleton<SimBlock.Presentation.Interfaces.IResourceMonitor, ResourceMonitor>();

                    // Register UI managers
                    services.AddSingleton<ILogoManager, LogoManager>();
                    services.AddSingleton<IStatusBarManager, StatusBarManager>();
                    services.AddSingleton<IUILayoutManager, UILayoutManager>();
                    services.AddSingleton<IKeyboardShortcutManager, KeyboardShortcutManager>();
                    services.AddSingleton<IThemeManager, ThemeManager>();
                    services.AddSingleton<ISettingsManager, SettingsManager>();
                    services.AddSingleton<IBlockingVisualizationManager, BlockingVisualizationManager>();

                    // Register auto-update services
                    services.AddSingleton<IVersionComparator, VersionComparator>();
                    services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();
                    services.AddSingleton<IAutoUpdateService, AutoUpdateService>();
                    services.AddSingleton<IAutoUpdateManager, AutoUpdateManager>();

                    // Register presentation layer
                    // MainForm is created manually in Main() to avoid disposal issues
                    services.AddTransient<SimBlock.Presentation.Forms.SettingsForm>();
                    services.AddTransient<SplashForm>();

                    // Register splash screen services
                    services.AddSingleton<ISplashScreenManager, SplashScreenManager>();
                    services.AddTransient<InitializationProgressReporter>();

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
