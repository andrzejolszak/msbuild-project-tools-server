using System;
using System.Collections.Generic;

namespace MSBuildProjectTools.LanguageServer.SemanticModel
{
    using Utilities;

    /// <summary>
    ///     A facility for looking up XML by textual location.
    /// </summary>
    public class SourceLocator
    {
        /// <summary>
        ///     The ranges for all XML nodes in the document
        /// </summary>
        /// <remarks>
        ///     Sorted by range comparison (effectively, this means document order).
        /// </remarks>
        private readonly List<Range> _nodeRanges = new List<Range>();

        /// <summary>
        ///     All nodes XML, keyed by starting position.
        /// </summary>
        /// <remarks>
        ///     Sorted by position comparison.
        /// </remarks>
        private readonly SortedDictionary<Position, SourceNode> _nodesByStartPosition = new SortedDictionary<Position, SourceNode>();

        /// <summary>
        ///     The position-lookup for the underlying XML document text.
        /// </summary>
        private readonly TextPositions _documentPositions;

        /// <summary>
        ///     Create a new <see cref="SourceLocator"/>.
        /// </summary>
        /// <param name="documentPositions">
        ///     The position-lookup for the underlying XML document text.
        /// </param>
        public SourceLocator(TextPositions documentPositions)
        {
            if (documentPositions == null)
                throw new ArgumentNullException(nameof(documentPositions));

            _documentPositions = documentPositions;

            // TODO
            List<SourceNode> allNodes = null;
            foreach (SourceNode node in allNodes)
            {
                _nodeRanges.Add(node.Range);
                _nodesByStartPosition.Add(node.Range.Start, node);
            }

            _nodeRanges.Sort();
        }

        /// <summary>
        ///     All nodes in the document.
        /// </summary>
        public IEnumerable<SourceNode> AllNodes => _nodesByStartPosition.Values;

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

            // Internally, we always use 1-based indexing because this is what the System.Xml APIs (and I'd rather keep things simple).
            position = position.ToOneBased();

            SourceNode nodeAtPosition = FindNode(position);
            if (nodeAtPosition == null)
                return null;

            // If we're on the (seamless, i.e. overlapping) boundary between 2 nodes, select the next node.
            if (nodeAtPosition.NextSibling != null)
            {
                if (position == nodeAtPosition.Range.End && position == nodeAtPosition.NextSibling.Range.Start)
                {
                    Serilog.Log.Debug("XmlLocator.Inspect moves to next sibling ({NodeKind} @ {NodeRange} -> {NextSiblingKind} @ {NextSiblingRange}).",
                        nodeAtPosition.Kind,
                        nodeAtPosition.Range,
                        nodeAtPosition.NextSibling.Kind,
                        nodeAtPosition.NextSibling.Range
                    );

                    nodeAtPosition = nodeAtPosition.NextSibling;
                }
            }

            int absolutePosition = _documentPositions.GetAbsolutePosition(position);

            LocationFlags flags = ComputeLocationFlags(nodeAtPosition, absolutePosition);
            SourceLocation inspectionResult = new SourceLocation(position, absolutePosition, nodeAtPosition, flags);

            return inspectionResult;
        }

        /// <summary>
        ///     Inspect the specified position in the XML.
        /// </summary>
        /// <param name="absolutePosition">
        ///     The target position (0-based).
        /// </param>
        /// <returns>
        ///     An <see cref="SourceLocation"/> representing the result of the inspection.
        /// </returns>
        public SourceLocation Inspect(int absolutePosition)
        {
            if (absolutePosition < 0)
                throw new ArgumentOutOfRangeException(nameof(absolutePosition), absolutePosition, "Absolute position cannot be less than 0.");

            return Inspect(
                _documentPositions.GetPosition(absolutePosition)
            );
        }

        /// <summary>
        ///     Find the node (if any) at the specified position.
        /// </summary>
        /// <param name="position">
        ///     The target position.
        /// </param>
        /// <returns>
        ///     The node, or <c>null</c> if no node was found at the specified position.
        /// </returns>
        public SourceNode FindNode(Position position)
        {
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            // Internally, we always use 1-based indexing because this is what the MSBuild APIs use (and I'd rather keep things simple).
            position = position.ToOneBased();

            // Short-circuit.
            if (_nodesByStartPosition.TryGetValue(position, out SourceNode exactMatch))
                return exactMatch;

            // TODO: Use binary search.

            Range lastMatchingRange = Range.Zero;
            foreach (Range objectRange in _nodeRanges)
            {
                if (lastMatchingRange != Range.Zero && objectRange.End > lastMatchingRange.End)
                    break; // We've moved past the end of the last matching range.

                if (objectRange.Contains(position))
                    lastMatchingRange = objectRange;
            }
            if (lastMatchingRange == Range.Zero)
                return null;

            return _nodesByStartPosition[lastMatchingRange.Start];
        }

        /// <summary>
        ///     Determine <see cref="LocationFlags"/> for the current position.
        /// </summary>
        /// <returns>
        ///     <see cref="LocationFlags"/> describing the position.
        /// </returns>
        private LocationFlags ComputeLocationFlags(SourceNode node, int absolutePosition)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            LocationFlags flags = LocationFlags.None;
            if (!node.IsValid)
                flags |= LocationFlags.Invalid;

            switch (node)
            {
                case SourceWhitespace whitespace:
                    {
                        flags |= LocationFlags.Whitespace | LocationFlags.Element | LocationFlags.Value;

                        break;
                    }
            }

            return flags;
        }
    }
}
