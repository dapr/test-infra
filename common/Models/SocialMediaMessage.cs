using System;
using System.ComponentModel.DataAnnotations;

namespace Dapr.Tests.Common.Models
{
    public class SocialMediaMessage
    {
        [Required]
        public Guid CorrelationId { get; set; }
        [Required]
        public Guid MessageId { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public string Sentiment { get; set; }
        [Required]
        public DateTime CreationDate { get; set; }

        // Used to measure the time between apps which is reported as a metric.  
        // An app should overwrite this if it wants a later one to measure duration.
        public DateTime PreviousAppTimestamp { get; set; }
    }
}
