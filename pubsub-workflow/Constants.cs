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

namespace PubsubWorkflow
{
    public class Constants
    {
        public const string RapidTopic = "rapidtopic";
        public const string MediumTopic = "mediumtopic";
        public const string SlowTopic = "slowtopic";
        public const string GlacialTopic = "glacialtopic";
        public const string RapidPubsubName = "longhaul-eh-rapid";
        public const string MediumPubsubName = "longhaul-eh-medium";
        public const string SlowPubsubName = "longhaul-eh-slow";
        public const string GlacialPubsubName = "longhaul-eh-glacial";
        public const int RapidDelaySeconds = 10;
        public const int MediumDelaySeconds = 300;
        public const int SlowDelaySeconds = 3600;
        public const int GlacialDelaySeconds = 43200;
    }

    public enum PubsubRates
    {
        Rapid,
        Medium,
        Slow,
        Glacial
    }
}
