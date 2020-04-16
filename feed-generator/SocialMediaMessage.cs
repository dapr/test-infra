// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace FeedGenerator
{
    using System;

    internal class SocialMediaMessage
    {
        public SocialMediaMessage() { }

        public Guid CorrelationId { get; set; }

        public Guid MessageId { get; set; }

        public string Message { get; set; }

        public DateTime CreationDate { get; set; }

        public string Sentiment { get; set; }

        // Used to measure the time between apps which is reported as a metric.  
        // An app should overwrite this if it wants a later one to measure duration.
        public DateTime PreviousAppTimestamp { get; set; }
    }
}
