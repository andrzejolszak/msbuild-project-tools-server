using System;
using System.Diagnostics;

namespace MSBuildProjectTools.LanguageServer.SemanticModel
{
    /// <summary>
    ///     Information about a position in XML.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class SourceLocation
    {
        /// <summary>
        ///     Create a new <see cref="SourceLocation"/>.
        /// </summary>
        /// <param name="position">
        ///     The location's position, in line / column form.
        /// </param>
        /// <param name="absolutePosition">
        ///     The location's (0-based) absolute position.
        /// </param>
        /// <param name="node">
        ///     The <see cref="SourceNode"/> closest to the location's position.
        /// </param>
        /// <param name="flags">
        ///     <see cref="LocationFlags"/> describing the location.
        /// </param>
        public SourceLocation(Position position, int absolutePosition, SourceNode node, LocationFlags flags)
        {
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Position = position;
            AbsolutePosition = absolutePosition;
            Node = node;
            Flags = flags;
        }

        /// <summary>
        ///     The position, in line / column form.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        ///     The (0-based) absolute position.
        /// </summary>
        public int AbsolutePosition { get; }

        /// <summary>
        ///     The <see cref="SourceNode"/> closest to the position.
        /// </summary>
        public SourceNode Node { get; }

        /// <summary>
        ///     The node's parent node (if any).
        /// </summary>
        public SourceNode Parent
        {
            get
            {
                switch (Node)
                {
                    case SourceWhitespace whitespace:
                        {
                            return null;
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
        }

        /// <summary>
        ///     The next sibling (if any) of the <see cref="SourceNode"/> closest to the position.
        /// </summary>
        public SourceNode NextSibling => Node?.NextSibling;

        /// <summary>
        ///     The previous sibling (if any) of the <see cref="SourceNode"/> closest to the position.
        /// </summary>
        public SourceNode PreviousSibling => Node?.PreviousSibling;

        /// <summary>
        ///     <see cref="LocationFlags"/> value(s) describing the position.
        /// </summary>
        public LocationFlags Flags { get; }

        /// <summary>
        ///     Get a string representation of the <see cref="SourceLocation"/>.
        /// </summary>
        /// <returns>
        ///     The display string.
        /// </returns>
        public override string ToString()
        {
            string nodeDescription = Node.Kind.ToString();

            return String.Format("{0} -> [{1}]:{2}",
                Position,
                Flags,
                Node.Range
            );
        }
    }
}
