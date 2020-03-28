// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace MessageAnalyzer
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
    }
}
