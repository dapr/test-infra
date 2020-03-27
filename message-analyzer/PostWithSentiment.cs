// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace MessageAnalyzer
{
    internal class PostWithSentiment : Post
    {
        public PostWithSentiment() { }

        public PostWithSentiment(Post p)
        {
            this.CorrelationId = p.CorrelationId;
            this.MessageId = p.MessageId;
            this.Message = p.Message;
            this.CreationDate = p.CreationDate;
        }

        // like "stars": 1-5
        public int Sentiment { get; set; }
    }
}
