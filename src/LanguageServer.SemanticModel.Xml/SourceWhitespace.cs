namespace MSBuildProjectTools.LanguageServer.SemanticModel
{
    /// <summary>
    ///     Represents non-significant whitespace (the syntax model refers to this as whitespace trivia).
    /// </summary>
    public class SourceWhitespace
        : SourceNode
    {
        /// <summary>
        ///     Create new <see cref="SourceWhitespace"/>.
        /// </summary>
        /// <param name="range">
        ///     The <see cref="Range"/>, within the source text, spanned by the whitespace.
        /// </param>
        public SourceWhitespace(Range range)
            : base(range)
        {
        }

        /// <summary>
        ///     The kind of <see cref="SourceNode"/>.
        /// </summary>
        public override SourceNodeKind Kind => SourceNodeKind.Whitespace;

        /// <summary>
        ///     The node name.
        /// </summary>
        public override string Name => "#whitespace";

        /// <summary>
        ///     Does the <see cref="SourceNode"/> represent valid XML?
        /// </summary>
        public override bool IsValid => true;
    }
}
