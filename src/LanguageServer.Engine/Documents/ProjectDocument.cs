using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Serilog;
using LspModels = OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MSBuildProjectTools.LanguageServer.Documents
{
    using SemanticModel;
    using SemanticModel.MSBuildExpressions;
    using Utilities;

    /// <summary>
    ///     Represents the document state for an MSBuild project.
    /// </summary>
    public abstract class ProjectDocument
        : IDisposable
    {
        /// <summary>
        ///     Diagnostics (if any) for the project.
        /// </summary>
        private readonly List<LspModels.Diagnostic> _diagnostics = new List<LspModels.Diagnostic>();

        /// <summary>
        ///     Create a new <see cref="ProjectDocument"/>.
        /// </summary>
        /// <param name="workspace">
        ///     The document workspace.
        /// </param>
        /// <param name="documentUri">
        ///     The document URI.
        /// </param>
        /// <param name="logger">
        ///     The application logger.
        /// </param>
        protected ProjectDocument(Workspace workspace, Uri documentUri, ILogger logger)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (documentUri == null)
                throw new ArgumentNullException(nameof(documentUri));

            Workspace = workspace;
            DocumentUri = documentUri;
            ProjectFile = new FileInfo(
                VSCodeDocumentUri.GetFileSystemPath(documentUri)
            );

            if (ProjectFile.Extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase))
                Kind = ProjectDocumentKind.Project;
            else if (ProjectFile.Extension.Equals(".props", StringComparison.OrdinalIgnoreCase))
                Kind = ProjectDocumentKind.Properties;
            else if (ProjectFile.Extension.Equals(".targets", StringComparison.OrdinalIgnoreCase))
                Kind = ProjectDocumentKind.Targets;
            else
                Kind = ProjectDocumentKind.Other;

            Log = logger.ForContext(GetType()).ForContext("ProjectDocument", ProjectFile.FullName);
        }

        /// <summary>
        ///     Finaliser for <see cref="ProjectDocument"/>.
        /// </summary>
        ~ProjectDocument()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Is parsing of MSBuild expressions enabled?
        /// </summary>
        public bool EnableExpressions { get; set; }

        /// <summary>
        ///     The document workspace.
        /// </summary>
        public Workspace Workspace { get; }

        /// <summary>
        ///     The project document URI.
        /// </summary>
        public Uri DocumentUri { get; }

        /// <summary>
        ///     The project file.
        /// </summary>
        public FileInfo ProjectFile { get; }

        /// <summary>
        ///     The kind of project document.
        /// </summary>
        public ProjectDocumentKind Kind { get; }

        /// <summary>
        ///     A lock used to control access to project state.
        /// </summary>
        public AsyncReaderWriterLock Lock { get; } = new AsyncReaderWriterLock();

        /// <summary>
        ///     Are there currently any diagnostics to be published for the project?
        /// </summary>
        public bool HasDiagnostics => _diagnostics.Count > 0;

        /// <summary>
        ///     Diagnostics (if any) for the project.
        /// </summary>
        public IReadOnlyList<LspModels.Diagnostic> Diagnostics => _diagnostics;

        /// <summary>
        ///     Does the project have in-memory changes?
        /// </summary>
        public bool IsDirty { get; protected set; }

        /// <summary>
        ///     The textual position translator for the project XML .
        /// </summary>
        public TextPositions SourcePositions { get; protected set; }

        /// <summary>
        ///     The project XML node lookup facility.
        /// </summary>
        public SourceLocator SourceLocator { get; protected set; }

        /// <summary>
        ///     The document's logger.
        /// </summary>
        protected ILogger Log { get; set; }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="ProjectDocument"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Inspect the specified location in the XML.
        /// </summary>
        /// <param name="position">
        ///     The location's position.
        /// </param>
        /// <returns>
        ///     An <see cref="SourceLocation"/> representing the result of the inspection.
        /// </returns>
        public SourceLocation Inspect(Position position)
        {
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            return SourceLocator.Inspect(position);
        }

        /// <summary>
        ///     Load and parse the project.
        /// </summary>
        /// <param name="cancellationToken">
        ///     An optional <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A task representing the load operation.
        /// </returns>
        public virtual async Task Load(CancellationToken cancellationToken = default(CancellationToken))
        {
            ClearDiagnostics();

            SourcePositions = null;
            SourceLocator = null;

            string xml;
            using (StreamReader reader = ProjectFile.OpenText())
            {
                xml = await reader.ReadToEndAsync();
            }
            SourcePositions = new TextPositions(xml);
            SourceLocator = new SourceLocator(SourcePositions);

            IsDirty = false;

            bool loaded = TryLoadProject();
        }

        /// <summary>
        ///     Update the project in-memory state.
        /// </summary>
        /// <param name="xml">
        ///     The project XML.
        /// </param>
        public virtual void Update(string xml)
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            ClearDiagnostics();

            SourcePositions = new TextPositions(xml);
            SourceLocator = new SourceLocator(SourcePositions);
            IsDirty = true;

            bool loaded = TryLoadProject();
        }

        /// <summary>
        ///     Unload the project.
        /// </summary>
        public virtual void Unload()
        {
            TryUnloadProject();

            SourcePositions = null;
            IsDirty = false;
        }

        /// <summary>
        ///     Get the expression's containing range.
        /// </summary>
        /// <param name="expression">
        ///     The MSBuild expression.
        /// </param>
        /// <param name="relativeTo">
        ///     The range of the <see cref="SourceNode"/> that contains the expression.
        /// </param>
        /// <returns>
        ///     The containing <see cref="Range"/>.
        /// </returns>
        public Range GetRange(ExpressionNode expression, Range relativeTo)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (relativeTo == null)
                throw new ArgumentNullException(nameof(relativeTo));

            return GetRange(expression, relativeTo.Start);
        }

        /// <summary>
        ///     Get the expression's containing range.
        /// </summary>
        /// <param name="expression">
        ///     The MSBuild expression.
        /// </param>
        /// <param name="relativeToPosition">
        ///     The starting position of the <see cref="SourceNode"/> that contains the expression.
        /// </param>
        /// <returns>
        ///     The containing <see cref="Range"/>.
        /// </returns>
        public Range GetRange(ExpressionNode expression, Position relativeToPosition)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (relativeToPosition == null)
                throw new ArgumentNullException(nameof(relativeToPosition));

            int absoluteBasePosition = SourcePositions.GetAbsolutePosition(relativeToPosition);

            return SourcePositions.GetRange(
                absoluteBasePosition + expression.AbsoluteStart,
                absoluteBasePosition + expression.AbsoluteEnd
            );
        }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="ProjectDocument"/>.
        /// </summary>
        /// <param name="disposing">
        ///     Explicit disposal?
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        ///     Attempt to load the underlying MSBuild project.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the project was successfully loaded; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool TryLoadProject();

        /// <summary>
        ///     Attempt to unload the underlying MSBuild project.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the project was successfully unloaded; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool TryUnloadProject();

        /// <summary>
        ///     Remove all diagnostics for the project file.
        /// </summary>
        protected void ClearDiagnostics()
        {
            _diagnostics.Clear();
        }

        /// <summary>
        ///     Add a diagnostic to be published for the project file.
        /// </summary>
        /// <param name="severity">
        ///     The diagnostic severity.
        /// </param>
        /// <param name="message">
        ///     The diagnostic message.
        /// </param>
        /// <param name="range">
        ///     The range of text within the project XML that the diagnostic relates to.
        /// </param>
        /// <param name="diagnosticCode">
        ///     A code to identify the diagnostic type.
        /// </param>
        protected void AddDiagnostic(LspModels.DiagnosticSeverity severity, string message, Range range, string diagnosticCode)
        {
            if (String.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'message'.", nameof(message));

            _diagnostics.Add(new LspModels.Diagnostic
            {
                Severity = severity,
                Code = new LspModels.DiagnosticCode(diagnosticCode),
                Message = message,
                Range = range.ToLsp(),
                Source = ProjectFile.FullName
            });
        }

        /// <summary>
        ///     Add an error diagnostic to be published for the project file.
        /// </summary>
        /// <param name="message">
        ///     The diagnostic message.
        /// </param>
        /// <param name="range">
        ///     The range of text within the project XML that the diagnostic relates to.
        /// </param>
        /// <param name="diagnosticCode">
        ///     A code to identify the diagnostic type.
        /// </param>
        protected void AddErrorDiagnostic(string message, Range range, string diagnosticCode) => AddDiagnostic(LspModels.DiagnosticSeverity.Error, message, range, diagnosticCode);

        /// <summary>
        ///     Add a warning diagnostic to be published for the project file.
        /// </summary>
        /// <param name="message">
        ///     The diagnostic message.
        /// </param>
        /// <param name="range">
        ///     The range of text within the project XML that the diagnostic relates to.
        /// </param>
        /// <param name="diagnosticCode">
        ///     A code to identify the diagnostic type.
        /// </param>
        protected void AddWarningDiagnostic(string message, Range range, string diagnosticCode) => AddDiagnostic(LspModels.DiagnosticSeverity.Warning, message, range, diagnosticCode);

        /// <summary>
        ///     Add an informational diagnostic to be published for the project file.
        /// </summary>
        /// <param name="message">
        ///     The diagnostic message.
        /// </param>
        /// <param name="range">
        ///     The range of text within the project XML that the diagnostic relates to.
        /// </param>
        /// <param name="diagnosticCode">
        ///     A code to identify the diagnostic type.
        /// </param>
        protected void AddInformationDiagnostic(string message, Range range, string diagnosticCode) => AddDiagnostic(LspModels.DiagnosticSeverity.Information, message, range, diagnosticCode);

        /// <summary>
        ///     Add a hint diagnostic to be published for the project file.
        /// </summary>
        /// <param name="message">
        ///     The diagnostic message.
        /// </param>
        /// <param name="range">
        ///     The range of text within the project XML that the diagnostic relates to.
        /// </param>
        /// <param name="diagnosticCode">
        ///     A code to identify the diagnostic type.
        /// </param>
        protected void AddHintDiagnostic(string message, Range range, string diagnosticCode) => AddDiagnostic(LspModels.DiagnosticSeverity.Hint, message, range, diagnosticCode);

        /// <summary>
        ///     Create a <see cref="Serilog.Context.LogContext"/> representing an operation.
        /// </summary>
        /// <param name="operationDescription">
        ///     The operation description.
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> representing the log context.
        /// </returns>
        protected IDisposable OperationContext(string operationDescription)
        {
            if (String.IsNullOrWhiteSpace(operationDescription))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'operationDescription'.", nameof(operationDescription));

            return Serilog.Context.LogContext.PushProperty("Operation", operationDescription);
        }
    }
}
