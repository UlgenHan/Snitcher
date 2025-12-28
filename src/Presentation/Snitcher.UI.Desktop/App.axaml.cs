using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Snitcher.Repository.Extensions;
using Snitcher.Service.Extensions;
using Snitcher.Service.Configuration;
using Snitcher.Service.DTOs;
using Snitcher.UI.Desktop.ViewModels;
using Snitcher.UI.Desktop.Views;
using Snitcher.UI.Desktop.Services.Database;
using Snitcher.UI.Desktop.Services;
using Snitcher.UI.Desktop.Configuration;
using Snitcher.UI.Desktop.Domains.Proxy;
using Snitcher.UI.Desktop.Domains.RequestBuilder;
using Snitcher.UI.Desktop.Domains.Automation;
using Snitcher.UI.Desktop.Domains.Collections;
using Snitcher.UI.Desktop.Domains.Workspace;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Certificates;

namespace Snitcher.UI.Desktop
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                // DisableAvaloniaDataAnnotationValidation();
                
                // Setup dependency injection
                _serviceProvider = ConfigureServices();
                
                // Create and show main window with DI
                desktop.MainWindow = new SnitcherMainWindow
                {
                    DataContext = _serviceProvider.GetRequiredService<SnitcherMainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
#if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
#else
                builder.SetMinimumLevel(LogLevel.Information);
#endif
            });

            // Configure Snitcher application stack with SQLite database
            services.ConfigureSnitcher(options =>
            {
                options.DatabaseProvider = "sqlite";
                options.DatabasePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Snitcher",
                    "snitcher.db");
#if DEBUG
                options.EnableSensitiveDataLogging = true;
                options.EnableDetailedErrors = true;
#endif
            });

            // Register UI services
            services.AddScoped<IDatabaseIntegrationService, DatabaseIntegrationService>();
            services.AddTransient<SnitcherMainViewModel>();
            services.AddTransient<MainApplicationWindowViewModel>();
            
            // Register Core ViewModels
            services.AddTransient<WelcomeViewModel>();
            services.AddTransient<ExtensionsViewModel>();
            
            // Register Domain-specific ViewModels and Services
            if (UIConfiguration.Features.EnableRequestBuilder)
            {
                services.AddTransient<RequestBuilderViewModel>();
                services.AddTransient<IRequestSender, RequestSender>();
            }
            
            if (UIConfiguration.Features.EnableCollections)
            {
                services.AddTransient<CollectionsExplorerViewModel>();
            }
            
            if (UIConfiguration.Features.EnableAutomation)
            {
                services.AddTransient<AutomationWorkflowViewModel>();
            }
            
            if (UIConfiguration.Features.EnableWorkspaceManagement)
            {
                services.AddTransient<WorkspaceManagerViewModel>();
            }
            
            // Register Proxy Inspector services
            if (UIConfiguration.Features.EnableHttpsInterception)
            {
                services.AddSingleton<ICertificateManager, CertificateManager>();
                services.AddSingleton<Snitcher.Sniffer.Core.Interfaces.ILogger>(provider => 
                    new SnitcherLoggerAdapter(provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SnitcherLoggerAdapter>>()));
                services.AddSingleton<IProxyService, ProxyService>();
                services.AddTransient<ProxyInspectorViewModel>();
                services.AddSingleton<IFlowMapper, FlowMapperService>();
            }

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Initialize database
            _ = Task.Run(async () =>
            {
                try
                {
                    await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<App>>();
                    logger.LogError(ex, "Failed to initialize database");
                }
            });

            return serviceProvider;
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Validation method disabled - Avalonia.Data.Validation namespace not available
        }

        /// <summary>
        /// Gets the service provider for accessing services throughout the application.
        /// </summary>
        public static IServiceProvider? ServiceProvider => (Current as App)?._serviceProvider;
    }
}