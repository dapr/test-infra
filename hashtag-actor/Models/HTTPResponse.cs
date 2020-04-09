// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Models
{
    using System.ComponentModel.DataAnnotations;

    public class HTTPResponse
    {
        public HTTPResponse(string message) {
            Message = message;
        }

        [Required]
        public string Message { get; set; }
    }
}
