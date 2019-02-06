using System;

namespace MSBuildProjectTools.LanguageServer.SemanticModel
{
    /// <summary>
    ///     Represents an XML node in the semantic model.
    /// </summary>
    public abstract class SourceNode
    {
        // TODO: Consider storing TextPositions here to allow XSNode and friends to calculate Positions and Ranges as-needed.

        /// <summary>
        ///     Create a new.
        /// </summary>
        /// <param name="range">
        ///     The <see cref="Range"/>, within the source text, spanned by the node.
        /// </param>
        protected SourceNode(Range range)
        {
            if (range == null)
                throw new ArgumentNullException(nameof(range));

            Range = range;
        }

        /// <summary>
        ///     The <see cref="Range"/>, within the source text, spanned by the node.
        /// </summary>
        public Range Range { get; }

        /// <summary>
        ///     The node's starting position.
        /// </summary>
        public Position Start => Range.Start;

        /// <summary>
        ///     The node's ending position.
        /// </summary>
        public Position End => Range.End;

        /// <summary>
        ///     The node's next sibling node (if any).
        /// </summary>
        public SourceNode NextSibling { get; internal set; }

        /// <summary>
        ///     The node's previous sibling node (if any).
        /// </summary>
        public SourceNode PreviousSibling { get; internal set; }

        /// <summary>
        ///     The kind of XML node represented by the <see cref="SourceNode"/>.
        /// </summary>
        public abstract SourceNodeKind Kind { get; }

        /// <summary>
        ///     The node name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     Does the <see cref="SourceNode"/> represent valid XML?
        /// </summary>
        public abstract bool IsValid { get; }
    }

    /// <summary>
    ///     Represents an XML node in the semantic model with a known type.
    /// </summary>
    public abstract class SourceNode<TSyntax>
        : SourceNode
    {
        /// <summary>
        ///     Create a new <see cref="SourceNode{TSyntax}"/>.
        /// </summary>
        /// <param name="syntaxNode">
        ///     The <typeparamref name="TSyntax"/> represented by the <see cref="SourceNode{TSyntax}"/>.
        /// </param>
        /// <param name="range">
        ///     The <see cref="Range"/>, within the source text, spanned by the node.
        /// </param>
        protected SourceNode(TSyntax syntaxNode, Range range)
            : base(range)
        {
            if (syntaxNode == null)
                throw new ArgumentNullException(nameof(syntaxNode));

            if (range == null)
                throw new ArgumentNullException(nameof(range));

            SyntaxNode = syntaxNode;
        }

        /// <summary>
        ///     The underlying <typeparamref name="TSyntax"/> represented by the <see cref="SourceNode{TSyntax}"/>.
        /// </summary>
        protected TSyntax SyntaxNode { get; }
    }
}
