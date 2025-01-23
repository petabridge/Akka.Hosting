// -----------------------------------------------------------------------
//  <copyright file="ClusterOptionsSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.Actor;
using Akka.Cluster.Hosting.SBR;
using Akka.Cluster.SBR;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Remote.Hosting;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterOptionsSpec
{
    [Fact(DisplayName = "Empty ClusterOptions should contain default HOCON values")]
    public void EmptyClusterOptionsTest()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "")
            .AddHocon(ConfigurationFactory.FromResource("Akka.Cluster.Configuration.Cluster.conf", typeof(Cluster).Assembly), HoconAddMode.Append)
            .WithActorRefProvider(ProviderSelection.Cluster.Instance)
            .BuildClusterHocon(new ClusterOptions());
        
        builder.Configuration.HasValue.Should().BeTrue();
        var settings = new ClusterSettings(builder.Configuration.Value, "");

        settings.Roles.Count.Should().Be(0);
        settings.AppVersion.CompareTo(Util.AppVersion.Create("assembly-version")).Should().Be(0);
        settings.MinNrOfMembersOfRole.Count.Should().Be(0);
        settings.SeedNodes.Count.Should().Be(0);
        settings.MinNrOfMembers.Should().Be(1);
        settings.LogInfo.Should().BeTrue();
        settings.LogInfoVerbose.Should().BeFalse();
        settings.DowningProviderType.Should().Be(typeof(SplitBrainResolverProvider));
        settings.HeartbeatInterval.Should().Be(1.Seconds());
        settings.HeartbeatExpectedResponseAfter.Should().Be(1.Seconds());
    }
    
    [Fact(DisplayName = "ClusterOptions should generate proper HOCON values")]
    public void ClusterOptionsTest()
    {
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "")
            .AddHocon(ConfigurationFactory.FromResource("Akka.Cluster.Configuration.Cluster.conf", typeof(Cluster).Assembly), HoconAddMode.Append)
            .BuildClusterHocon(new ClusterOptions
            {
                Roles = new []{ "front-end", "back-end"},
                MinimumNumberOfMembersPerRole = new Dictionary<string, int>
                {
                    ["back-end"] = 5
                },
                AppVersion = "1.0.0",
                MinimumNumberOfMembers = 99,
                SeedNodes = new [] { "akka.tcp://system@somewhere.com:9999" },
                LogInfo = false,
                LogInfoVerbose = true,
                SplitBrainResolver = new KeepMajorityOption
                {
                    Role = "back-end"
                },
                FailureDetector = new PhiAccrualFailureDetectorOptions
                {
                    HeartbeatInterval = 1.1.Seconds(),
                    AcceptableHeartbeatPause = 1.1.Seconds(),
                    Threshold = 1.1,
                    MaxSampleSize = 1,
                    MinStandardDeviation = 1.1.Seconds(),
                    UnreachableNodesReaperInterval = 1.1.Seconds(),
                    ExpectedResponseAfter = 1.1.Seconds()
                }
            });
        
        builder.Configuration.HasValue.Should().BeTrue();
        var settings = new ClusterSettings(builder.Configuration.Value, "");

        settings.Roles.Should().BeEquivalentTo("front-end", "back-end");
        
        settings.MinNrOfMembersOfRole.Count.Should().Be(1);
        settings.MinNrOfMembersOfRole.ContainsKey("back-end").Should().BeTrue();
        settings.MinNrOfMembersOfRole["back-end"].Should().Be(5);
        
        settings.AppVersion.CompareTo(Util.AppVersion.Create("1.0.0")).Should().Be(0);
        settings.SeedNodes.Should().BeEquivalentTo(new [] { Address.Parse("akka.tcp://system@somewhere.com:9999" )});
        settings.MinNrOfMembers.Should().Be(99);
        settings.LogInfo.Should().BeTrue(); // This is not intuitive, but LogInfo is defined as LogInfoVerbose || LogInfo in ClusterSettings
        settings.LogInfoVerbose.Should().BeTrue();
        settings.DowningProviderType.Should().Be(typeof(SplitBrainResolverProvider));

        var sbrConfig = builder.Configuration.Value.GetConfig("akka.cluster.split-brain-resolver");
        sbrConfig.GetString("active-strategy").Should().Be(SplitBrainResolverSettings.KeepMajorityName);
        sbrConfig.GetString($"{SplitBrainResolverSettings.KeepMajorityName}.role").Should().Be("back-end");

        var detectorConfig = builder.Configuration.Value.GetConfig("akka.cluster.failure-detector");
        detectorConfig.GetTimeSpan("heartbeat-interval").Should().Be(1.1.Seconds());
        detectorConfig.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.1.Seconds());
        detectorConfig.GetDouble("threshold").Should().Be(1.1);
        detectorConfig.GetInt("max-sample-size").Should().Be(1);
        detectorConfig.GetTimeSpan("min-std-deviation").Should().Be(1.1.Seconds());
        detectorConfig.GetTimeSpan("unreachable-nodes-reaper-interval").Should().Be(1.1.Seconds());
        detectorConfig.GetTimeSpan("expected-response-after").Should().Be(1.1.Seconds());
    }

    [Fact(DisplayName = "ClusterOptions should be bindable using Microsoft.Extensions.Configuration")]
    public void ClusterOptionsConfigurationTest()
    {
        const string json = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""ConnectionStrings"": {
    ""sqlServerLocal"": ""Server=localhost,1533;Database=Akka;User Id=sa;Password=l0lTh1sIsOpenSource;"",
  },
  ""Akka"": {
    ""ClusterOptions"": {
      ""Roles"": [ ""front-end"", ""back-end"" ],
      ""MinimumNumberOfMembersPerRole"" : {
        ""back-end"" : 5
      },
      ""AppVersion"": ""1.0.0"",
      ""MinimumNumberOfMembers"": 99,
      ""SeedNodes"": [ ""akka.tcp://system@somewhere.com:9999"" ],
      ""LogInfo"": false,
      ""LogInfoVerbose"": true
    },
    ""KeepMajorityOption"": {
      ""Role"" : ""back-end""
    }
  }
}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        
        var clusterOptions = jsonConfig.GetSection("Akka:ClusterOptions").Get<ClusterOptions>();
        clusterOptions.SplitBrainResolver = jsonConfig.GetSection("Akka:KeepMajorityOption").Get<KeepMajorityOption>();
        
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "")
            .AddHocon(ConfigurationFactory.FromResource("Akka.Cluster.Configuration.Cluster.conf", typeof(Cluster).Assembly), HoconAddMode.Append)
            .BuildClusterHocon(clusterOptions);
        
        builder.Configuration.HasValue.Should().BeTrue();
        var settings = new ClusterSettings(builder.Configuration.Value, "");

        settings.Roles.Should().BeEquivalentTo("front-end", "back-end");
        
        settings.MinNrOfMembersOfRole.Count.Should().Be(1);
        settings.MinNrOfMembersOfRole.ContainsKey("back-end").Should().BeTrue();
        settings.MinNrOfMembersOfRole["back-end"].Should().Be(5);
        
        settings.AppVersion.CompareTo(Util.AppVersion.Create("1.0.0")).Should().Be(0);
        settings.SeedNodes.Should().BeEquivalentTo(new [] { Address.Parse("akka.tcp://system@somewhere.com:9999" )});
        settings.MinNrOfMembers.Should().Be(99);
        settings.LogInfo.Should().BeTrue(); // This is not intuitive, but LogInfo is defined as LogInfoVerbose || LogInfo in ClusterSettings
        settings.LogInfoVerbose.Should().BeTrue();
        settings.DowningProviderType.Should().Be(typeof(SplitBrainResolverProvider));

        var sbrConfig = builder.Configuration.Value.GetConfig("akka.cluster.split-brain-resolver");
        sbrConfig.GetString("active-strategy").Should().Be(SplitBrainResolverSettings.KeepMajorityName);
        sbrConfig.GetString($"{SplitBrainResolverSettings.KeepMajorityName}.role").Should().Be("back-end");
    }
}