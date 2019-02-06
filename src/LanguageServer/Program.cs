using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using LSP = OmniSharp.Extensions.LanguageServer;

namespace MSBuildProjectTools.LanguageServer
{
    using Utilities;

    /// <summary>
    ///     The MSBuild language server.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        private static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            try
            {
                AutoDetectExtensionDirectory();

                AsyncMain().Wait();
            }
            catch (AggregateException aggregateError)
            {
                foreach (Exception unexpectedError in aggregateError.Flatten().InnerExceptions)
                {
                    Console.WriteLine(unexpectedError);
                }
            }
            catch (Exception unexpectedError)
            {
                Console.WriteLine(unexpectedError);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        ///     The asynchronous program entry-point.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing program execution.
        /// </returns>
        private static async Task AsyncMain()
        {
            using (ActivityCorrelationManager.BeginActivityScope())
            using (Terminator terminator = new Terminator())
            using (IContainer container = BuildContainer())
            {
                // Force initialisation of logging.
                ILogger log = container.Resolve<ILogger>().ForContext(typeof(Program));

                log.Information("Creating language server...");

                var server = container.Resolve<LSP.Server.LanguageServer>();

                log.Debug("Waiting for client to initialise language server...");

                await server.Initialize();

                log.Debug("Language server initialised by client.");

                await server.WasShutDown;

                log.Debug("Language server is shutting down...");

                await server.WaitForExit;

                log.Debug("Server has shut down. Preparing to terminate server process...");

                // AF: Temporary fix for tintoy/msbuild-project-tools-vscode#36
                //
                //     The server hangs while waiting for LSP's ProcessScheduler thread to terminate so, after a timeout has elapsed, we forcibly terminate this process.
                terminator.TerminateAfter(
                    TimeSpan.FromSeconds(3)
                );

                log.Debug("Server process is ready to terminate.");
            }
        }

        /// <summary>
        ///     Build a container for language server components.
        /// </summary>
        /// <returns>
        ///     The container.
        /// </returns>
        private static IContainer BuildContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterModule<LoggingModule>();
            builder.RegisterModule<LanguageServerModule>();

            return builder.Build();
        }

        /// <summary>
        ///     Auto-detect the directory containing the extension's files.
        /// </summary>
        private static void AutoDetectExtensionDirectory()
        {
            string extensionDir = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_DIR");
            if (String.IsNullOrWhiteSpace(extensionDir))
            {
                extensionDir = Path.Combine(
                    AppContext.BaseDirectory, "..", ".."
                );
            }
            extensionDir = Path.GetFullPath(extensionDir);
            Environment.SetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_DIR", extensionDir);
        }
    }
}
