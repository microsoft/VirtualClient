// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Abstract parser for various VC documents
    /// </summary>
    public abstract class TextParser<T>
    {
        /// <summary>
        /// Constructor for <see cref="TextParser{T}"/>
        /// </summary>
        /// <param name="rawText">Text to be parsed.</param>
        public TextParser(string rawText)
        {
            this.Metadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            this.Sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.RawText = rawText;
        }

        /// <summary>
        /// Additional metadata parsed from the results.
        /// </summary>
        public IDictionary<string, IConvertible> Metadata { get; }

        /// <summary>
        /// Sections of a document
        /// </summary>
        public IDictionary<string, string> Sections { get; set; }

        /// <summary>
        /// Raw input text.
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// Processed text before parsing.
        /// </summary>
        public string PreprocessedText { get; set; }

        /// <summary>
        /// Override if special parsing of the text is needed.
        /// </summary>
        public abstract T Parse();

        /// <summary>
        /// Override if preprocessing of the text is needed.
        /// </summary>
        protected virtual void Preprocess()
        {
            this.PreprocessedText = this.RawText;
        }
    }
}
