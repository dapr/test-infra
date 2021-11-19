// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dapr.Tests.Common.Models
{
    public class BindingInvocationMessage
    {
        [Required]
        public string TargetBinding { get; set; }
        [Required]
        public string TargetOperation { get; set; }
        [Required]
        public int InvocationCount { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
