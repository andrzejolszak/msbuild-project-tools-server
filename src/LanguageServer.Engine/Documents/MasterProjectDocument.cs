using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace MSBuildProjectTools.LanguageServer.Documents
{
    /// <summary>
    ///     Represents the document state for an MSBuild project.
    /// </summary>
    public class MasterProjectDocument
        : ProjectDocument
    {
        /// <summary>
        ///     Create a new <see cref="MasterProjectDocument"/>.
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
        public MasterProjectDocument(Workspace workspace, Uri documentUri, ILogger logger)
            : base(workspace, documentUri, logger)
        {
        }

        /// <summary>
        ///     Unload the project.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }

        /// <summary>
        ///     Load the project document.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used to cancel the load.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> representing the operation.
        /// </returns>
        public override async Task Load(CancellationToken cancellationToken)
        {
            await base.Load(cancellationToken);
        }

        /// <summary>
        ///     Dispose of resources being used by the <see cref="ProjectDocument"/>.
        /// </summary>
        /// <param name="disposing">
        ///     Explicit disposal?
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        /// <summary>
        ///     Attempt to load the underlying MSBuild project.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the project was successfully loaded; otherwise, <c>false</c>.
        /// </returns>
        protected override bool TryLoadProject()
        {
            return false;
        }

        /// <summary>
        ///     Attempt to unload the underlying MSBuild project.
        /// </summary>
        /// <returns>
        ///     <c>true</c>, if the project was successfully unloaded; otherwise, <c>false</c>.
        /// </returns>
        protected override bool TryUnloadProject()
        {
            return false;
        }
    }
}
