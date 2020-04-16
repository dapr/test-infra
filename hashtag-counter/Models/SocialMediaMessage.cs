// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class SocialMediaMessage
    {
        [Required]
        public string CorrelationId { get; set; }
        [Required]
        public string MessageId { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public string Sentiment { get; set; }
        [Required]
        public DateTime CreationDate { get; set; }

        // Used to measure the time between apps which is reported as a metric.  
        // An app should overwrite this if it wants a later one to measure duration.
        [Required]
        public DateTime PreviousAppTimestamp { get; set; }
    }
}
