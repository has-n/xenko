// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using Irony.Parsing;

namespace SiliconStudio.Shaders.Grammar
{
    /// <summary>
    /// A Custom Scanner used for Irony
    /// </summary>
    internal class CustomScanner : Scanner
    {
        private Tokenizer tokenizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomScanner"/> class.
        /// </summary>
        /// <param name="tokenizer">The tokenizer.</param>
        public CustomScanner(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
        }

        /// <inheritdoc/>
        public override SourceLocation Location
        {
            get
            {
                return tokenizer.Location;
            }

            set
            {
                tokenizer.Location = value;
            }
        }

        /// <inheritdoc/>
        public override void SetSourceText(string sourceText, string sourceFileName)
        {
            tokenizer.SetSourceText(sourceText, sourceFileName);
        }

        /// <inheritdoc/>
        protected override void NextToken()
        {
            Context.CurrentToken = tokenizer.GetNextToken();
        }

        /// <inheritdoc/>
        protected override void PrepareInput()
        {
        }
    }
}
