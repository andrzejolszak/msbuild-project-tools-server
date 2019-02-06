using System;

namespace MSBuildProjectTools.LanguageServer.SemanticModel
{
    /// <summary>
    ///     Extension methods for <see cref="SourceLocation"/>.
    /// </summary>
    public static class SourceLocationExtensions
    {
        /// <summary>
        ///     Does the location represent a name?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element or attribute name; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsName(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.Flags.HasFlag(LocationFlags.Name);
        }

        /// <summary>
        ///     Does the location represent a value?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents element content (text / whitespace) or an attribute value; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValue(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.Flags.HasFlag(LocationFlags.Value);
        }

        /// <summary>
        ///     Does the location represent text?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents text content within an element; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsText(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.Flags.HasFlag(LocationFlags.Text);
        }

        /// <summary>
        ///     Does the location represent whitespace?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <param name="whitespace">
        ///     Receives the <see cref="SourceWhitespace"/> (if any) at the location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents whitespace within element content; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWhitespace(this SourceLocation location, out SourceWhitespace whitespace)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            if (location.IsWhitespace())
            {
                whitespace = (SourceWhitespace)location.Node;

                return true;
            }

            whitespace = null;

            return false;
        }

        /// <summary>
        ///     Does the location represent whitespace?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents whitespace within element content; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWhitespace(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.Flags.HasFlag(LocationFlags.Whitespace);
        }

        /// <summary>
        ///     Does the location represent an attribute?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttribute(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.Flags.HasFlag(LocationFlags.Attribute);
        }

        /// <summary>
        ///     Does the location represent an attribute's name?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an attribute's name; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttributeName(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsAttribute() && location.IsName();
        }

        /// <summary>
        ///     Does the location represent an attribute's value?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an attribute's name; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttributeValue(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsAttribute() && location.IsValue();
        }

        /// <summary>
        ///     Does the location represent an element?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElement(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.Flags.HasFlag(LocationFlags.Element);
        }

        /// <summary>
        ///     Does the location represent an empty element?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an empty element; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmptyElement(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() && location.Flags.HasFlag(LocationFlags.Empty);
        }

        /// <summary>
        ///     Does the location represent an element content (i.e. text or whitespace)?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents element content; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElementContent(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() && location.IsValue();
        }

        /// <summary>
        ///     Does the location represent an element's textual content?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element's textual content; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElementText(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() && location.IsText();
        }

        /// <summary>
        ///     Does the location represent an element's opening tag?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element's opening tag; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElementOpeningTag(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() && location.Flags.HasFlag(LocationFlags.OpeningTag);
        }

        /// <summary>
        ///     Does the location represent an element's closing tag?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element's closing tag; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElementClosingTag(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() && location.Flags.HasFlag(LocationFlags.ClosingTag);
        }

        /// <summary>
        ///     Does the location represent an element's attributes range (but not a specific attribute)?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElementBetweenAttributes(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() && location.Flags.HasFlag(LocationFlags.Attributes);
        }

        /// <summary>
        ///     Does the location represent an element or an attribute?
        /// </summary>
        /// <param name="location">
        ///     The XML location.
        /// </param>
        /// <returns>
        ///     <c>true</c>, if the location represents an element or attribute; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsElementOrAttribute(this SourceLocation location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            return location.IsElement() || location.IsAttribute();
        }
    }
}
