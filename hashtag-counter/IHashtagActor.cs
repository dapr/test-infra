// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Actors
{
    using System.Threading.Tasks;
    using Dapr.Actors;

    public interface IHashTagActor : IActor
    {
        Task Increment(string hashtagAndSentiment);
    }
}