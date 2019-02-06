using System;
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
    using SemanticModel;
    using Utilities;

    /// <summary>
    ///     Handler for document hover requests.
    /// </summary>
    public sealed class HoverHandler
        : Handler, IHoverHandler
    {
        /// <summary>
        ///     Create a new <see cref="HoverHandler"/>.
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
        public HoverHandler(ILanguageServer server, Workspace workspace, ILogger logger)
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
        ///     Has the client supplied hover capabilities?
        /// </summary>
        private bool HaveHoverCapabilities => HoverCapabilities != null;

        /// <summary>
        ///     The client's hover capabilities.
        /// </summary>
        private HoverCapability HoverCapabilities { get; set; }

        /// <summary>
        ///     Get registration options for handling document events.
        /// </summary>
        /// <returns>
        ///     The registration options.
        /// </returns>
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() => DocumentRegistrationOptions;

        /// <summary>
        ///     Handle a request for hover information.
        /// </summary>
        /// <param name="parameters">
        ///     The request parameters.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the request.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation whose result is the hover details or <c>null</c> if no hover details are provided.
        /// </returns>
        async Task<Hover> IRequestHandler<TextDocumentPositionParams, Hover>.Handle(TextDocumentPositionParams parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            try
            {
                return await OnHover(parameters, cancellationToken);
            }
            catch (Exception unexpectedError)
            {
                Log.Error(unexpectedError, "Unhandled exception in {Method:l}.", "OnHover");

                return null;
            }
        }

        /// <summary>
        ///     Called to inform the handler of the language server's hover capabilities.
        /// </summary>
        /// <param name="capabilities">
        ///     A <see cref="HoverCapability"/> data structure representing the capabilities.
        /// </param>
        void ICapability<HoverCapability>.SetCapability(HoverCapability capabilities)
        {
            HoverCapabilities = capabilities;
        }

        /// <summary>
        ///     Called when the mouse pointer hovers over text.
        /// </summary>
        /// <param name="parameters">
        ///     The notification parameters.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}"/> whose result is the hover details, or <c>null</c> if no hover details are provided by the handler.
        /// </returns>
        private async Task<Hover> OnHover(TextDocumentPositionParams parameters, CancellationToken cancellationToken)
        {
            if (Workspace.Configuration.Language.DisableFeature.Hover)
                return null;

            ProjectDocument projectDocument = await Workspace.GetProjectDocument(parameters.TextDocument.Uri);

            using (await projectDocument.Lock.ReaderLockAsync(cancellationToken))
            {
                Position position = parameters.Position.ToNative();

                SourceLocation location = projectDocument.SourceLocator.Inspect(position);
                if (location == null)
                {
                    Log.Debug("Not providing hover information for {Position} in {ProjectFile} (nothing interesting at this position).",
                        position,
                        projectDocument.ProjectFile.FullName
                    );

                    return null;
                }

                Log.Debug("Examining location {Location:l}...", location);

                if (!location.IsElementOrAttribute())
                {
                    Log.Debug("Not providing hover information for {Position} in {ProjectFile} (position does not represent an element or attribute).",
                        position,
                        projectDocument.ProjectFile.FullName
                    );

                    return null;
                }

                MarkedStringContainer hoverContent = null;

                // TODO

                if (hoverContent == null)
                {
                    Log.Debug("No hover content available for {Position} in {ProjectFile}.",
                        position,
                        projectDocument.ProjectFile.FullName
                    );

                    return null;
                }

                return new Hover
                {
                    Contents = new MarkedStringsOrMarkupContent(hoverContent),
                    Range = location.Node.Range.ToLsp()
                };
            }
        }
    }
}
