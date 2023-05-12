/*
Copyright 2021 The Dapr Authors
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PubsubWorkflow.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PubsubWorkflow
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDaprClient();
            services.AddControllers();
            services.AddScoped<MessagePublishingService>();
            services.AddSingleton<MetricsService>();
            services.AddScoped<IDictionary<PubsubRates, DateTime>>(s =>
            {
                return new Dictionary<PubsubRates, DateTime>()
                {
                    { PubsubRates.Rapid,  DateTime.UtcNow },
                    { PubsubRates.Medium,  DateTime.UtcNow },
                    { PubsubRates.Slow,  DateTime.UtcNow },
                    { PubsubRates.Glacial,  DateTime.UtcNow }
                };
            });

            services.AddSingleton(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, JsonSerializerOptions serializerOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
            });
        }
    }
}