// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace FeedGenerator
{
    using Dapr.Client;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// FeedGenerator - generates messages and publishes them using Dapr.
    /// The main functionality is in StartMessageGeneratorAsync().
    /// </summary>
    public class Program
    {
        // This uses the names of shapes for a generic theme
        static internal string[] HashTags = new string[]
        {
            "circle",
            "ellipse",
            "square",
            "rectangle",
            "triangle",
            "star",
            "cardioid",
            "picycloid",
            "limocon",
            "hypocycoid"
        };

        /// <summary>
        /// Main for FeedGenerator
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            int delayInMilliseconds = 10000;
            if (args.Length != 0 && args[0] != "%LAUNCHER_ARGS%")
            {
                if (int.TryParse(args[0], out delayInMilliseconds) == false)
                {
                    string msg = "Could not parse delay";
                    Console.WriteLine(msg);
                    throw new InvalidOperationException(msg);
                }
            }

            IHost host = CreateHostBuilder(args).Build();

            Task.Run(() => StartMessageGeneratorAsync(delayInMilliseconds));

            host.Run();
        }

        /// <summary>
        /// Creates WebHost Builder.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Returns IHostbuilder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        static internal async void StartMessageGeneratorAsync(int delayInMilliseconds)
        {
            const string PubsubTopicName = "receivemediapost";
            TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            DaprClient client = daprClientBuilder.Build();
            while (true)
            {
                SocialMediaMessage message = GeneratePost();
                Console.WriteLine("Publishing");
                try
                {

                    await client.PublishEventAsync<SocialMediaMessage>(PubsubTopicName, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught {0}", e.ToString());
                }

                await Task.Delay(delay);
            }
        }

        static internal SocialMediaMessage GeneratePost()
        {
            Guid correlationId = Guid.NewGuid();
            Guid messageId = Guid.NewGuid();
            string message = GenerateRandomMessage();
            DateTime creationDate = DateTime.UtcNow;

            return new SocialMediaMessage()
            {
                CorrelationId = correlationId,
                MessageId = messageId,
                Message = message,
                CreationDate = creationDate
            };
        }

        static internal string GenerateRandomMessage()
        {
            Random random = new Random();
            int length = random.Next(5, 10);

            string s = "";
            for (int i = 0; i < length; i++)
            {
                int j = random.Next(26);
                char c = (char)('a' + j);
                s += c;
            }

            // add hashtag
            s += " #";
            s += HashTags[random.Next(HashTags.Length)];
            return s;
        }
    }
}
