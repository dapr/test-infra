namespace Dapr.Tests.Actors.PresenceTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Dapr.Actors;
    using Dapr.Actors.Client;

    class Program
    {
        private static void Main()
        {
            const int nGames = 10; // number of games to simulate
            const int nPlayersPerGame = 4; // number of players in each game

            var sendInterval = TimeSpan.FromSeconds(5); // interval for sending updates

            // Precreate base heartbeat data objects for each of the games.
            // We'll modify them before every time before sending.
            var heartbeats = new HeartbeatData[nGames];
            for (var i = 0; i < nGames; i++)
            {
                heartbeats[i] = new HeartbeatData();
                heartbeats[i].Game = Guid.NewGuid();
                for (var j = 0; j < nPlayersPerGame; j++)
                {
                    var playerId = Guid.NewGuid();
                    heartbeats[i].Status.Players.Add(playerId);
                }
            }

            var outstandingUpdates = new List<Task>();
            var outstandingScoreReads = new List<Task<byte[]>>();
            var iteration = 0;

            while (true)
            {
                iteration++;
                Console.WriteLine();
                Console.WriteLine("Sending heartbeat series # {0}", iteration);

                ulong high = ((ulong) iteration) << 32;
                ulong low = (ulong) (iteration > 5 ? iteration - 5 : 0);
                var value = high | low;
                var score = BitConverter.GetBytes(value);
                var presence = ActorProxy.Create<IPresenceActor>(ActorId.CreateRandom(), "PresenceActor"); // get any stateless actor
                outstandingUpdates.Clear();
                try
                {
                    for (var i = 0; i < nGames; i++)
                    {
                        heartbeats[i].Status.Score = score;

                        var heartbeatData = JsonSerializer.SerializeToUtf8Bytes<HeartbeatData>(heartbeats[i]);
                        var t = presence.Heartbeat(heartbeatData);
                        outstandingUpdates.Add(t);
                    }

                    // Wait for all calls to finish.
                    // It is okay to block the thread here because it's a client program with no parallelism.
                    // One should never block a thread in grain code.
                    Console.WriteLine("Wating for the tasks to finish");
                    Task.WaitAll(outstandingUpdates.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: {0}", e);
                }

                Console.WriteLine();
                Console.WriteLine("Getting game scores: ");
                outstandingScoreReads.Clear();
                try
                {
                    for (var i = 0; i < nGames; i++)
                    {
                        var t = ActorProxy.Create<IGameActor>(new ActorId(heartbeats[i].Game.ToString()), "PresenceActor").GetGameScore();
                        outstandingScoreReads.Add(t);
                    }

                    // Wait for all calls to finish.
                    // It is okay to block the thread here because it's a client program with no parallelism.
                    // One should never block a thread in grain code.
                    Task.WhenAll(outstandingScoreReads.ToArray()).Wait();

                    for (var i = 0; i < nGames; i++)
                    {
                        Console.WriteLine("Game: {0}, Score: {1}", heartbeats[i].Game, String.Join(",", outstandingScoreReads[i].Result.Select(b => b.ToString())));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: {0}", e);
                }

                Console.WriteLine();
                Console.WriteLine("Sleeping for {0} seconds.", sendInterval.TotalSeconds);
                Console.WriteLine("Press CTRL-C to exit");
                Thread.Sleep(sendInterval);
            }
        }
    }
}