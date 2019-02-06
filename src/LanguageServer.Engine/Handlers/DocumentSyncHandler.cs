using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using Serilog.Events;

namespace MSBuildProjectTools.LanguageServer.Handlers
{
    using CustomProtocol;
    using Documents;
    using Utilities;

    /// <summary>
    ///     The handler for language server document synchronisation.
    /// </summary>
    public sealed class DocumentSyncHandler
        : Handler, ITextDocumentSyncHandler
    {
        /// <summary>
        ///     Create a new <see cref="DocumentSyncHandler"/>.
        /// </summary>
        /// <param name="server">
        ///     The language server.
        /// </param>
        /// <param name="workspace">
        ///     The document workspace.
        /// </param>
        /// <param name="logger">
        ///     The application logger.
        /// </param>
        public DocumentSyncHandler(ILanguageServer server, Workspace workspace, ILogger logger)
            : base(server, logger)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            Workspace = workspace;
        }

        /// <summary>
        ///     Options that control synchronisation.
        /// </summary>
        public TextDocumentSyncOptions Options { get; } = new TextDocumentSyncOptions
        {
            WillSaveWaitUntil = false,
            WillSave = true,
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions
            {
                IncludeText = true
            },
            OpenClose = true
        };

        /// <summary>
        ///     The document selector that describes documents to synchronise.
        /// </summary>
        private DocumentSelector DocumentSelector { get; } = new DocumentSelector(
            new DocumentFilter
            {
                Pattern = "**/*.*",
                Language = "msbuild",
                Scheme = "file"
            },
            new DocumentFilter
            {
                Pattern = "**/*.*proj",
                Language = "xml",
                Scheme = "file"
            },
            new DocumentFilter
            {
                Pattern = "**/*.props",
                Language = "xml",
                Scheme = "file"
            },
            new DocumentFilter
            {
                Pattern = "**/*.targets",
                Language = "xml",
                Scheme = "file"
            }
        );

        /// <summary>
        ///     The document workspace.
        /// </summary>
        private Workspace Workspace { get; }

        /// <summary>
        ///     Has the client supplied synchronisation capabilities?
        /// </summary>
        private bool HaveSynchronizationCapabilities => SynchronizationCapabilities != null;

        /// <summary>
        ///     The client's synchronisation capabilities.
        /// </summary>
        private SynchronizationCapability SynchronizationCapabilities { get; set; }

        /// <summary>
        ///     Get registration options for handling document events.
        /// </summary>
        private TextDocumentRegistrationOptions DocumentRegistrationOptions
        {
            get => new TextDocumentRegistrationOptions
            {
                DocumentSelector = DocumentSelector
            };
        }

        /// <summary>
        ///     Get registration options for handling document-change events.
        /// </summary>
        private TextDocumentChangeRegistrationOptions DocumentChangeRegistrationOptions
        {
            get => new TextDocumentChangeRegistrationOptions
            {
                DocumentSelector = DocumentSelector,
                SyncKind = Options.Change
            };
        }

        /// <summary>
        ///     Get registration options for handling document save events.
        /// </summary>
        private TextDocumentSaveRegistrationOptions DocumentSaveRegistrationOptions
        {
            get => new TextDocumentSaveRegistrationOptions
            {
                DocumentSelector = DocumentSelector,
                IncludeText = Options.Save.IncludeText
            };
        }

        /// <summary>
        ///     Handle a document being opened.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task INotificationHandler<DidOpenTextDocumentParams>.Handle(DidOpenTextDocumentParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (BeginOperation("OnDidOpenTextDocument"))
            {
                try
                {
                    await OnDidOpenTextDocument(parameters);
                }
                catch (Exception unexpectedError)
                {
                    Log.Error(unexpectedError, "Unhandled exception in {Method:l}.", "OnDidOpenTextDocument");
                }
            }
        }

        /// <summary>
        ///     Handle a document being closed.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task INotificationHandler<DidCloseTextDocumentParams>.Handle(DidCloseTextDocumentParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (BeginOperation("OnDidCloseTextDocument"))
            {
                try
                {
                    await OnDidCloseTextDocument(parameters);
                }
                catch (Exception unexpectedError)
                {
                    Log.Error(unexpectedError, "Unhandled exception in {Method:l}.", "OnDidCloseTextDocument");
                }
            }
        }

        /// <summary>
        ///     Handle a change in document text.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task INotificationHandler<DidChangeTextDocumentParams>.Handle(DidChangeTextDocumentParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (BeginOperation("OnDidChangeTextDocument"))
            {
                try
                {
                    await OnDidChangeTextDocument(parameters);
                }
                catch (Exception unexpectedError)
                {
                    Log.Error(unexpectedError, "Unhandled exception in {Method:l}.", "OnDidChangeTextDocument");
                }
            }
        }

        /// <summary>
        ///     Handle a document being saved.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        async Task INotificationHandler<DidSaveTextDocumentParams>.Handle(DidSaveTextDocumentParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (BeginOperation("OnDidSaveTextDocument"))
            {
                try
                {
                    await OnDidSaveTextDocument(parameters);
                }
                catch (Exception unexpectedError)
                {
                    Log.Error(unexpectedError, "Unhandled exception in {Method:l}.", "OnDidSaveTextDocument");
                }
            }
        }

        /// <summary>
        ///     Get registration options for handling document events.
        /// </summary>
        /// <returns>
        ///     The registration options.
        /// </returns>
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() => DocumentRegistrationOptions;

        /// <summary>
        ///     Get registration options for handling document-change events.
        /// </summary>
        /// <returns>
        ///     The registration options.
        /// </returns>
        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions() => DocumentChangeRegistrationOptions;

        /// <summary>
        ///     Get registration options for handling document save events.
        /// </summary>
        /// <returns>
        ///     The registration options.
        /// </returns>
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() => DocumentSaveRegistrationOptions;

        /// <summary>
        ///     Called to inform the handler of the language server's document-synchronisation capabilities.
        /// </summary>
        /// <param name="capabilities">
        ///     A <see cref="SynchronizationCapability"/> data structure representing the capabilities.
        /// </param>
        void ICapability<SynchronizationCapability>.SetCapability(SynchronizationCapability capabilities)
        {
            SynchronizationCapabilities = capabilities;
        }

        /// <summary>
        ///     Get attributes for the specified text document.
        /// </summary>
        /// <param name="documentUri">
        ///     The document URI.
        /// </param>
        /// <returns>
        ///     The document attributes.
        /// </returns>
        TextDocumentAttributes ITextDocumentSyncHandler.GetTextDocumentAttributes(Uri documentUri)
        {
            if (documentUri == null)
                throw new ArgumentNullException(nameof(documentUri));

            return GetTextDocumentAttributes(documentUri);
        }

        /// <summary>
        ///     Called when a text document is opened.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        private async Task OnDidOpenTextDocument(DidOpenTextDocumentParams parameters)
        {
            Server.NotifyBusy("Loading project...");

            ProjectDocument projectDocument = await Workspace.GetProjectDocument(parameters.TextDocument.Uri);
            Workspace.PublishDiagnostics(projectDocument);

            // Only enable expression-related language service facilities if they're using our custom "MSBuild" language type (rather than "XML").
            projectDocument.EnableExpressions = parameters.TextDocument.LanguageId == "msbuild";

            Server.ClearBusy("Project loaded.");

            if (Log.IsEnabled(LogEventLevel.Verbose))
            {
                Log.Verbose("===========================");
                Log.Verbose("===========================");
                Log.Verbose("MSBuild project not loaded.");
            }
        }

        /// <summary>
        ///     Called when a text document is changed.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        private async Task OnDidChangeTextDocument(DidChangeTextDocumentParams parameters)
        {
            Log.Verbose("Reloading project {ProjectFile}...",
                VSCodeDocumentUri.GetFileSystemPath(parameters.TextDocument.Uri)
            );

            TextDocumentContentChangeEvent mostRecentChange = parameters.ContentChanges.LastOrDefault();
            if (mostRecentChange == null)
                return;

            string updatedDocumentText = mostRecentChange.Text;
            ProjectDocument projectDocument = await Workspace.TryUpdateProjectDocument(parameters.TextDocument.Uri, updatedDocumentText);
            Workspace.PublishDiagnostics(projectDocument);

            if (Log.IsEnabled(LogEventLevel.Verbose))
            {
                Log.Verbose("===========================");
                Log.Verbose("MSBuild project not loaded; will used cached project state (as long as positional lookups are not required).");
            }
        }

        /// <summary>
        ///     Called when a text document is saved.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        private async Task OnDidSaveTextDocument(DidSaveTextDocumentParams parameters)
        {
            Log.Information("Reloading project {ProjectFile}...",
                VSCodeDocumentUri.GetFileSystemPath(parameters.TextDocument.Uri)
            );

            ProjectDocument projectDocument = await Workspace.GetProjectDocument(parameters.TextDocument.Uri, reload: true);
            Workspace.PublishDiagnostics(projectDocument);

            Log.Information("Successfully reloaded project {ProjectFilePath}.", projectDocument.ProjectFile.FullName);
        }

        /// <summary>
        ///     Called when a text document is closed.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        private async Task OnDidCloseTextDocument(DidCloseTextDocumentParams parameters)
        {
            await Workspace.RemoveProjectDocument(parameters.TextDocument.Uri);

            Log.Information("Unloaded project {ProjectFile}.",
                VSCodeDocumentUri.GetFileSystemPath(parameters.TextDocument.Uri)
            );
        }

        /// <summary>
        ///     Get attributes for the specified text document.
        /// </summary>
        /// <param name="documentUri">
        ///     The document URI.
        /// </param>
        /// <returns>
        ///     The document attributes.
        /// </returns>
        private TextDocumentAttributes GetTextDocumentAttributes(Uri documentUri)
        {
            string documentFilePath = VSCodeDocumentUri.GetFileSystemPath(documentUri);
            if (documentFilePath == null)
                return new TextDocumentAttributes(documentUri, "plaintext");

            string extension = Path.GetExtension(documentFilePath).ToLower();
            switch (extension)
            {
                case "props":
                case "targets":
                    {
                        break;
                    }
                default:
                    {
                        if (extension.EndsWith("proj"))
                            break;

                        return new TextDocumentAttributes(documentUri, "plaintext");
                    }
            }

            return new TextDocumentAttributes(documentUri, "msbuild");
        }
    }
}
