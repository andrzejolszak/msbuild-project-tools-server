using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Serilog;

using LspModels = OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MSBuildProjectTools.LanguageServer.CompletionProviders
{
    using Documents;
    using SemanticModel;
    using Utilities;

    /// <summary>
    ///     Completion provider for XML comments.
    /// </summary>
    public class CommentCompletion
        : CompletionProvider
    {
        /// <summary>
        ///     Create a new <see cref="CommentCompletion"/> provider.
        /// </summary>
        /// <param name="logger">
        ///     The application logger.
        /// </param>
        public CommentCompletion(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        ///     The provider display name.
        /// </summary>
        public override string Name => "XML Comments";

        /// <summary>
        ///     Provide completions for the specified location.
        /// </summary>
        /// <param name="location">
        ///     The <see cref="SourceLocation"/> where completions are requested.
        /// </param>
        /// <param name="projectDocument">
        ///     The <see cref="ProjectDocument"/> that contains the <paramref name="location"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}"/> that resolves either a <see cref="CompletionList"/>s, or <c>null</c> if no completions are provided.
        /// </returns>
        public override async Task<CompletionList> ProvideCompletions(SourceLocation location, ProjectDocument projectDocument, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            if (projectDocument == null)
                throw new ArgumentNullException(nameof(projectDocument));

            List<CompletionItem> completions = new List<CompletionItem>();

            Log.Verbose("Evaluate completions for {XmlLocation:l}", location);

            using (await projectDocument.Lock.ReaderLockAsync())
            {
                SourceNode replaceElement = null;
                // TODO
                if (replaceElement != null)
                {
                    Log.Verbose("Offering completions to replace element {ElementName} @ {ReplaceRange:l}",
                        replaceElement.Name,
                        replaceElement.Range
                    );

                    completions.AddRange(
                        GetCompletionItems(replaceElement.Range)
                    );
                }
                else
                {
                    Log.Verbose("Offering completions to insert element @ {InsertPosition:l}",
                        location.Position
                    );

                    completions.AddRange(
                        GetCompletionItems(
                            replaceRange: location.Position.ToEmptyRange()
                        )
                    );
                }
            }

            Log.Verbose("Offering {CompletionCount} completion(s) for {XmlLocation:l}", completions.Count, location);

            if (completions.Count == 0)
                return null;

            return new CompletionList(completions,
                isIncomplete: false // Consider this list to be exhaustive
            );
        }

        /// <summary>
        ///     Get comment completions.
        /// </summary>
        /// <param name="replaceRange">
        ///     The range of text to be replaced by the completions.
        /// </param>
        /// <returns>
        ///     A sequence of <see cref="CompletionItem"/>s.
        /// </returns>
        public IEnumerable<CompletionItem> GetCompletionItems(Range replaceRange)
        {
            if (replaceRange == null)
                throw new ArgumentNullException(nameof(replaceRange));

            LspModels.Range completionRange = replaceRange.ToLsp();

            // <!--  -->
            yield return new CompletionItem
            {
                Label = "<!-- -->",
                Detail = "Comment",
                Documentation = "XML comment",
                SortText = Priority + "<!-- -->",
                TextEdit = new TextEdit
                {
                    NewText = "<!-- $0 -->",
                    Range = completionRange
                },
                InsertTextFormat = InsertTextFormat.Snippet
            };
        }
    }
}
