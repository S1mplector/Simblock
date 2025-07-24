using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Application.Services;
using SimBlock.Infrastructure.Services;
using SimBlock.Infrastructure.Windows;
using System.IO;
using System.Windows.Forms;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering macro-related services
    /// </summary>
    public static class MacroServiceExtensions
    {
        /// <summary>
        /// Adds the macro services to the service collection
        /// </summary>
        public static IServiceCollection AddMacroServices(this IServiceCollection services)
        {
            // Register the core macro service
            services.AddSingleton<IMacroService, MacroService>();

            // Register input recorder and player for Windows
            services.AddSingleton<IInputRecorder, WindowsInputRecorder>();
            services.AddSingleton<IInputPlayer, WindowsInputPlayer>();

            // Register storage with application data path
            services.AddSingleton<IMacroStorage>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<JsonMacroStorage>>();
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Application.CompanyName ?? "SimBlock",
                    Application.ProductName ?? "SimBlock");
                
                return new JsonMacroStorage(logger, appDataPath);
            });

            return services;
        }

        /// <summary>
        /// Adds macro services with custom storage path
        /// </summary>
        public static IServiceCollection AddMacroServices(this IServiceCollection services, string storagePath)
        {
            // Register the core macro service
            services.AddSingleton<IMacroService, MacroService>();

            // Register input recorder and player for Windows
            services.AddSingleton<IInputRecorder, WindowsInputRecorder>();
            services.AddSingleton<IInputPlayer, WindowsInputPlayer>();

            // Register storage with custom path
            services.AddSingleton<IMacroStorage>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<JsonMacroStorage>>();
                return new JsonMacroStorage(logger, storagePath);
            });

            return services;
        }

        /// <summary>
        /// Adds macro services with custom implementations
        /// </summary>
        public static IServiceCollection AddMacroServices<TStorage, TRecorder, TPlayer>(this IServiceCollection services)
            where TStorage : class, IMacroStorage
            where TRecorder : class, IInputRecorder
            where TPlayer : class, IInputPlayer
        {
            // Register the core macro service
            services.AddSingleton<IMacroService, MacroService>();

            // Register custom implementations
            services.AddSingleton<IInputRecorder, TRecorder>();
            services.AddSingleton<IInputPlayer, TPlayer>();
            services.AddSingleton<IMacroStorage, TStorage>();

            return services;
        }
    }
}
