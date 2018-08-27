
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;
using CrashReporterDotNET;
using System;

namespace NaturalLauncher
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.Current.DispatcherUnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            SendReport(unobservedTaskExceptionEventArgs.Exception);
            Environment.Exit(0);
        }

        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            SendReport(dispatcherUnhandledExceptionEventArgs.Exception);
            Environment.Exit(0);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            SendReport((Exception)unhandledExceptionEventArgs.ExceptionObject);
            Environment.Exit(0);
        }

        public static void SendReport(Exception exception, string developerMessage = "")
        {
            var reportCrash = new ReportCrash("mael.vignaux@elseware-experience.com"); // mail to send crash dumps to
            reportCrash.DeveloperMessage = developerMessage;
            reportCrash.DoctorDumpSettings = new DoctorDumpSettings
            {
                ApplicationID = new Guid("abac8312-371a-4fad-aa9d-b4b0cd5d11ab"),
            };
            /*var reportCrash = new ReportCrash("Email where you want to receive crash reports.")
            {
                DeveloperMessage = developerMessage
            };*/
            reportCrash.Send(exception);
        }
    }
}
