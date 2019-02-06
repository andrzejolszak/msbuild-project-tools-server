using System;
using Autofac;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using MSLogging = Microsoft.Extensions.Logging;

namespace MSBuildProjectTools.LanguageServer
{
    using Logging;
    using Serilog.Events;
    using LanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

    /// <summary>
    ///     Registration logic for logging components.
    /// </summary>
    public class LoggingModule
        : Module
    {
        /// <summary>
        ///     Create a new <see cref="LoggingModule"/>.
        /// </summary>
        public LoggingModule()
        {
        }

        /// <summary>
        ///     Configure logging components.
        /// </summary>
        /// <param name="builder">
        ///     The container builder to configure.
        /// </param>
        protected override void Load(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Register(CreateLogger)
                .SingleInstance()
                .As<ILogger>();

            builder.RegisterType<MSLogging.LoggerFactory>()
                .As<MSLogging.ILoggerFactory>()
                .SingleInstance()
                .OnActivated(activation =>
                {
                    activation.Instance.AddSerilog(
                        logger: activation.Context.Resolve<ILogger>().ForContext<LanguageServer>()
                    );
                });
        }

        /// <summary>
        ///     Create the application logger.
        /// </summary>
        /// <param name="componentContext">
        ///     The current component context.
        /// </param>
        /// <returns>
        ///     The logger.
        /// </returns>
        private static ILogger CreateLogger(IComponentContext componentContext)
        {
            if (componentContext == null)
                throw new ArgumentNullException(nameof(componentContext));

            Configuration configuration = componentContext.Resolve<Configuration>();
            ConfigureSeq(configuration.Logging.Seq);

            // Override default log level.
            bool debug = true;
            if (debug)
            {
                configuration.Logging.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
                configuration.Logging.Seq.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
            }

            ILanguageServer languageServer = componentContext.Resolve<ILanguageServer>();

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithCurrentActivityId()
                .Enrich.WithDemystifiedStackTraces()
                .Enrich.FromLogContext();

            if (!String.IsNullOrWhiteSpace(configuration.Logging.Seq.Url))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Seq(configuration.Logging.Seq.Url,
                    apiKey: configuration.Logging.Seq.ApiKey,
                    controlLevelSwitch: configuration.Logging.Seq.LevelSwitch
                );
            }

            string logFilePath = ".\\logs\\log.txt";
            if (!String.IsNullOrWhiteSpace(logFilePath))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.File(
                    path: logFilePath,
                    levelSwitch: configuration.Logging.LevelSwitch,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}/{Operation}] {Message}{NewLine}{Exception}",
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                );
            }

            loggerConfiguration = loggerConfiguration.WriteTo.LanguageServer(languageServer, configuration.Logging.LevelSwitch);

            ILogger logger = loggerConfiguration.CreateLogger();
            Log.Logger = logger;

            logger.Verbose("Logger initialised.");

            return logger;
        }

        /// <summary>
        ///     Configure SEQ logging from environment variables.
        /// </summary>
        /// <param name="configuration">
        ///     The language server's Seq logging configuration.
        /// </param>
        private static void ConfigureSeq(SeqLoggingConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // We have to use environment variables here since at configuration time there's no LSP connection yet.
            configuration.Url = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_SEQ_URL");
            configuration.ApiKey = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_SEQ_API_KEY");
        }
    }
}
