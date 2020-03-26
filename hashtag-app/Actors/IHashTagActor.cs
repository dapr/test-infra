namespace Dapr.Tests.HashTagApp.Actors
{
    using System.Threading.Tasks;
    using Dapr.Actors;
    
    public interface IHashTagActor : IActor
    {
        Task Increment(string sentiment);
    }
}