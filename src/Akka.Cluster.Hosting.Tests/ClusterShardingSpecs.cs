using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Hosting.SBR;
using Akka.Cluster.Hosting.Tests.Lease;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Hosting;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public class ClusterShardingSpecs
{
    public sealed class MyTopLevelActor : ReceiveActor
    {
    }

    public sealed class MyEntityActor : ReceiveActor
    {
        public MyEntityActor(string entityId, IActorRef sourceRef)
        {
            EntityId = entityId;
            SourceRef = sourceRef;

            Receive<GetId>(g => { Sender.Tell(EntityId); });
            Receive<GetSourceRef>(g => Sender.Tell(SourceRef));
        }

        public string EntityId { get; }

        public IActorRef SourceRef { get; }

        public sealed class GetId : IWithId
        {
            public GetId(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }

        public sealed class GetSourceRef : IWithId
        {
            public GetSourceRef(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }
    }

    public interface IWithId
    {
        string Id { get; }
    }

    public sealed class Extractor : HashCodeMessageExtractor
    {
        public Extractor() : base(30)
        {
        }

        public override string EntityId(object message)
        {
            if (message is IWithId withId)
                return withId.Id;
            return string.Empty;
        }
    }

    public ClusterShardingSpecs(ITestOutputHelper output)
    {
        Output = output;
    }

    public ITestOutputHelper Output { get; }

    [Fact]
    public async Task Should_use_ActorRegistry_with_ShardRegion()
    {
        // arrange
        using var host = await TestHelper.CreateHost(builder =>
        {
            builder.WithActors((system, registry) =>
                {
                    var tLevel = system.ActorOf(Props.Create(() => new MyTopLevelActor()), "toplevel");
                    registry.Register<MyTopLevelActor>(tLevel);
                })
                .WithShardRegion<MyEntityActor>("entities", (system, registry) =>
                {
                    var tLevel = registry.Get<MyTopLevelActor>();
                    return s => Props.Create(() => new MyEntityActor(s, tLevel));
                }, new Extractor(), new ShardOptions() { Role = "my-host", StateStoreMode = StateStoreMode.DData });
        }, new ClusterOptions() { Roles = new[] { "my-host" } }, Output);

        var actorSystem = host.Services.GetRequiredService<ActorSystem>();
        var actorRegistry = ActorRegistry.For(actorSystem);
        var shardRegion = actorRegistry.Get<MyEntityActor>();
        
        // act
        var id = await shardRegion.Ask<string>(new MyEntityActor.GetId("foo"), TimeSpan.FromSeconds(3));
        var sourceRef =
            await shardRegion.Ask<IActorRef>(new MyEntityActor.GetSourceRef("foo"), TimeSpan.FromSeconds(3));

        // assert
        id.Should().Be("foo");
        sourceRef.Should().Be(actorRegistry.Get<MyTopLevelActor>());
    }

    [Fact(DisplayName = "ShardOptions with different values should generate valid ClusterShardSettings")]
    public void ShardOptionsTest()
    {
        var settings1 = ToSettings(new ShardOptions
        {
            RememberEntities = true,
            StateStoreMode = StateStoreMode.Persistence,
            RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
            Role = "first",
            PassivateIdleEntityAfter = 1.Seconds(),
            SnapshotPluginId = "firstSnapshot",
            JournalPluginId = "firstJournal", 
            LeaseImplementation = new TestLeaseOption(),
            LeaseRetryInterval = 2.Seconds(),
            ShardRegionQueryTimeout = 3.Seconds(),
        });
        settings1.RememberEntities.Should().BeTrue();
        settings1.StateStoreMode.Should().Be(StateStoreMode.Persistence);
        settings1.RememberEntitiesStore.Should().Be(RememberEntitiesStore.Eventsourced);
        settings1.Role.Should().Be("first");
        settings1.PassivateIdleEntityAfter.Should().Be(1.Seconds());
        settings1.SnapshotPluginId.Should().Be("firstSnapshot");
        settings1.JournalPluginId.Should().Be("firstJournal");
        settings1.LeaseSettings.LeaseImplementation.Should().Be("test-lease");
        settings1.LeaseSettings.LeaseRetryInterval.Should().Be(2.Seconds());
        settings1.ShardRegionQueryTimeout.Should().Be(3.Seconds());
        
        var settings2 = ToSettings(new ShardOptions
        {
            RememberEntities = false,
            StateStoreMode = StateStoreMode.DData,
            RememberEntitiesStore = RememberEntitiesStore.DData,
            Role = "second",
            PassivateIdleEntityAfter = 4.Seconds(),
            SnapshotPluginId = "secondSnapshot",
            JournalPluginId = "secondJournal", 
            ShardRegionQueryTimeout = 5.Seconds(),
        });
        settings2.RememberEntities.Should().BeFalse();
        settings2.StateStoreMode.Should().Be(StateStoreMode.DData);
        settings2.RememberEntitiesStore.Should().Be(RememberEntitiesStore.DData);
        settings2.Role.Should().Be("second");
        settings2.PassivateIdleEntityAfter.Should().Be(4.Seconds());
        settings2.JournalPluginId.Should().Be("secondJournal");
        settings2.SnapshotPluginId.Should().Be("secondSnapshot");
        settings2.LeaseSettings.Should().BeNull();
        settings2.ShardRegionQueryTimeout.Should().Be(5.Seconds());
    }

    private static ClusterShardingSettings ToSettings(ShardOptions shardOptions)
    {
        var defaultConfig = ClusterSharding.DefaultConfig()
            .WithFallback(DistributedData.DistributedData.DefaultConfig())
            .WithFallback(ClusterSingleton.DefaultConfig());
        
        var shardingConfig = ConfigurationFactory.ParseString(shardOptions.ToString())
            .WithFallback(defaultConfig.GetConfig("akka.cluster.sharding"));
        var coordinatorConfig = defaultConfig.GetConfig(
            shardingConfig.GetString("coordinator-singleton"));

        return ClusterShardingSettings.Create(shardingConfig, coordinatorConfig);
    }
}