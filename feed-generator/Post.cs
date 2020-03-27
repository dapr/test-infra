// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace FeedGenerator
{
    using System;

    internal class Post
    {
        public Post() { }

        public Guid CorrelationId { get; set; }

        public Guid MessageId { get; set; }

        public string Message { get; set; }

        public DateTime CreationDate { get; set; }
    }
}
