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
    }
}
