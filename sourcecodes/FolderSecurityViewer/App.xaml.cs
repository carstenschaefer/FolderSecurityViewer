namespace FolderSecurityViewer
{
    using System;
    using System.Windows;
    using System.Windows.Threading;
    using FSV.AdServices.AbstractionLayer;
    using FSV.Business;
    using FSV.Configuration;
    using FSV.Configuration.Abstractions;
    using FSV.Crypto;
    using FSV.Extensions.Logging;
    using FSV.Extensions.Serialization;
    using FSV.Extensions.WindowConfiguration;
    using FSV.FileSystem.Interop;
    using FSV.Security;
    using FSV.ShareServices;
    using FSV.ViewModel;
    using FSV.ViewModel.Abstractions;
    using FSV.ViewModel.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider serviceProvider;

        public App()
        {
            var services = new ServiceCollection();

            LoggingBootstrappingExtensions.AddLogging(services);

            services
                .UsePlatformServices()
                .UseSerialization()
                .UseWindowConfigurationServices()
                .UseConfigurationServices()
                .UseSecurityServices()
                .UseSecurityContextServices()
                .UseShareServices()
                .UseActiveDirectoryAbstractionLayer()
                .UseBusiness()
                .UseViewModels()
                .UseModelBuilders();

            services.AddModule<AppModule>();
            services.AddSingleton<IDispatcherService>(new DispatcherService(this.Dispatcher));
            this.serviceProvider = services.BuildServiceProvider();
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configurationManager = this.serviceProvider.GetRequiredService<IConfigurationManager>();
            configurationManager.CreateDefaultConfigFileAsync(false);

            var cultureManager = this.serviceProvider.GetRequiredService<ICultureManager>();
            cultureManager.InitializeCulture();

            this.MainWindow = this.serviceProvider.GetRequiredService<HomeWindow>();
            this.MainWindow?.Show();

            this.DispatcherUnhandledException += this.HandleDispatcherError;
            AppDomain.CurrentDomain.UnhandledException += this.HandleCurrentDomainUnhandledException;
        }

        private void HandleCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var log = this.serviceProvider.GetRequiredService<ILogger<App>>();
            log.LogCritical(args.ExceptionObject as Exception, args.ExceptionObject.ToString());
        }

        private void HandleDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var log = this.serviceProvider.GetRequiredService<ILogger<App>>();
            try
            {
                Exception exception = e.Exception;
                var dialogService = this.serviceProvider.GetRequiredService<IDialogService>();
                dialogService.ShowMessage(exception.ToString());
                log.LogCritical(exception, exception.Message);
            }
            catch (Exception exception)
            {
                log.LogCritical(exception, "Failed to handle dispatcher error.");
            }
            finally
            {
                e.Handled = true;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.DispatcherUnhandledException -= this.HandleDispatcherError;
            AppDomain.CurrentDomain.UnhandledException -= this.HandleCurrentDomainUnhandledException;

            this.serviceProvider.Dispose();
            base.OnExit(e);
        }
    }
}