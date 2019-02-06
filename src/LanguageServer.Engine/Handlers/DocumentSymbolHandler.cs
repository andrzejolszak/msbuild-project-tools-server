using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace MSBuildProjectTools.LanguageServer.Handlers
{
    using Documents;

    /// <summary>
    ///     Handler for document symbol requests.
    /// </summary>
    public sealed class DocumentSymbolHandler
        : Handler, IDocumentSymbolHandler
    {
        /// <summary>
        ///     Create a new <see cref="DocumentSymbolHandler"/>.
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
        public DocumentSymbolHandler(ILanguageServer server, Workspace workspace, ILogger logger)
            : base(server, logger)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            Workspace = workspace;
        }

        /// <summary>
        ///     The document workspace.
        /// </summary>
        private Workspace Workspace { get; }

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
        ///     Has the client supplied document symbol capabilities?
        /// </summary>
        private bool HaveDocumentSymbolCapabilities => DocumentSymbolCapabilities != null;

        /// <summary>
        ///     The client's document symbol capabilities.
        /// </summary>
        private DocumentSymbolCapability DocumentSymbolCapabilities { get; set; }

        /// <summary>
        ///     Get registration options for handling document events.
        /// </summary>
        /// <returns>
        ///     The registration options.
        /// </returns>
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() => DocumentRegistrationOptions;

        /// <summary>
        ///     Handle a request for document symbols.
        /// </summary>
        /// <param name="parameters">
        ///     The request parameters.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation whose result is the symbol container or <c>null</c> if no symbols are provided.
        /// </returns>
        async Task<DocumentSymbolInformationContainer> IRequestHandler<DocumentSymbolParams, DocumentSymbolInformationContainer>.Handle(DocumentSymbolParams parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (BeginOperation("OnDocumentSymbols"))
            {
                try
                {
                    return await OnDocumentSymbols(parameters, cancellationToken);
                }
                catch (Exception unexpectedError)
                {
                    Log.Error(unexpectedError, "Unhandled exception in {Method:l}.", "OnDocumentSymbols");

                    return null;
                }
            }
        }

        /// <summary>
        ///     Called to inform the handler of the language server's document symbol capabilities.
        /// </summary>
        /// <param name="capabilities">
        ///     A <see cref="DocumentSymbolCapability"/> data structure representing the capabilities.
        /// </param>
        void ICapability<DocumentSymbolCapability>.SetCapability(DocumentSymbolCapability capabilities)
        {
            DocumentSymbolCapabilities = capabilities;
        }

        /// <summary>
        ///     Called when completions are requested.
        /// </summary>
        /// <param name="parameters">
        ///     The request parameters.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation whose result is the completion list or <c>null</c> if no completions are provided.
        /// </returns>
        private async Task<DocumentSymbolInformationContainer> OnDocumentSymbols(DocumentSymbolParams parameters, CancellationToken cancellationToken)
        {
            ProjectDocument projectDocument = await Workspace.GetProjectDocument(parameters.TextDocument.Uri);

            List<DocumentSymbolInformation> symbols = new List<DocumentSymbolInformation>();
            using (await projectDocument.Lock.ReaderLockAsync(cancellationToken))
            {
            }

            if (symbols.Count == 0)
                return null;

            return new DocumentSymbolInformationContainer(
                symbols.OrderBy(symbol => symbol.Name)
            );
        }
    }
}
