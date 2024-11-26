using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Hosting.Tests.Lease;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Coordination;
using Akka.DistributedData;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Remote.Hosting;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Cluster.Hosting.Tests;

public class ShardOptionsSpecs
{
    private sealed class MyEntityActor : ReceiveActor
    {
        public MyEntityActor(string entityId)
        {
            EntityId = entityId;
            ReceiveAny(m => Sender.Tell(m));
        }

        public string EntityId { get; }
    }
    
    private sealed class Extractor : HashCodeMessageExtractor
    {
        public Extractor() : base(30)
        {
        }

        public override string EntityId(object message)
        {
            return string.Empty;
        }
    }
    
    private sealed class StopMessage
    {
        public static readonly StopMessage Instance = new();
        private StopMessage() { }
    }
    
    [Fact(DisplayName = "Empty ShardOptions and ShardingDDataOptions without DData should contain default HOCON values")]
    public async Task EmptyShardOptionsTest()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddAkka("ConfigSys", (builder, _) =>
                {
                    builder
                        .WithRemoting()
                        .WithClustering()
                        .WithShardRegion<MyEntityActor>(
                            typeName: "entities", 
                            entityPropsFactory: (_, _) =>
                            {
                                return s => Props.Create(() => new MyEntityActor(s));
                            }, 
                            messageExtractor: new Extractor(), 
                            shardOptions: new ShardOptions())
                        .WithShardingDistributedData(new ShardingDDataOptions());
                });
            }).Build();
        await host.StartAsync();

        Config appliedShardingConfig;
        ClusterShardingSettings shardingSettings;
        ReplicatorSettings replicatorSettings;
        try
        {
            var sys = host.Services.GetRequiredService<ActorSystem>();
            (appliedShardingConfig, shardingSettings) = GetClusterShardingSettings(new ShardOptions(), sys);
            replicatorSettings = GetReplicatorSettings(shardingSettings, sys);
        }
        finally
        {
            await host.StopAsync();
        }

        var shardingConfig = ClusterSharding.DefaultConfig().GetConfig("akka.cluster.sharding");

        #region ClusterShardingSettings validation

        shardingSettings.Role.Should().BeNull();
        shardingSettings.RememberEntities.Should().Be(shardingConfig.GetBoolean("remember-entities"));
        shardingSettings.JournalPluginId.Should().Be(shardingConfig.GetString("journal-plugin-id"));
        shardingSettings.SnapshotPluginId.Should().Be(shardingConfig.GetString("snapshot-plugin-id"));
        shardingSettings.StateStoreMode.Should().Be(Enum.Parse<StateStoreMode>(shardingConfig.GetString("state-store-mode"), true));
        shardingSettings.RememberEntitiesStore.Should().Be(Enum.Parse<RememberEntitiesStore>(shardingConfig.GetString("remember-entities-store"), true));
        shardingSettings.ShardRegionQueryTimeout.Should().Be(shardingConfig.GetTimeSpan("shard-region-query-timeout"));
        shardingSettings.PassivateIdleEntityAfter.Should().Be(shardingConfig.GetTimeSpan("passivate-idle-entity-after"));
        appliedShardingConfig.GetBoolean("fail-on-invalid-entity-state-transition").Should().Be(shardingConfig.GetBoolean("fail-on-invalid-entity-state-transition"));
        
        shardingSettings.TuningParameters.CoordinatorFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("coordinator-failure-backoff"));
        shardingSettings.TuningParameters.RetryInterval.Should().Be(shardingConfig.GetTimeSpan("retry-interval"));
        shardingSettings.TuningParameters.BufferSize.Should().Be(shardingConfig.GetInt("buffer-size"));
        shardingSettings.TuningParameters.HandOffTimeout.Should().Be(shardingConfig.GetTimeSpan("handoff-timeout"));
        shardingSettings.TuningParameters.ShardStartTimeout.Should().Be(shardingConfig.GetTimeSpan("shard-start-timeout"));
        shardingSettings.TuningParameters.ShardFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("shard-failure-backoff"));
        shardingSettings.TuningParameters.EntityRestartBackoff.Should().Be(shardingConfig.GetTimeSpan("entity-restart-backoff"));
        shardingSettings.TuningParameters.RebalanceInterval.Should().Be(shardingConfig.GetTimeSpan("rebalance-interval"));
        shardingSettings.TuningParameters.SnapshotAfter.Should().Be(shardingConfig.GetInt("snapshot-after"));
        shardingSettings.TuningParameters.KeepNrOfBatches.Should().Be(shardingConfig.GetInt("keep-nr-of-batches"));
        shardingSettings.TuningParameters.LeastShardAllocationRebalanceThreshold.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-threshold"));
        shardingSettings.TuningParameters.LeastShardAllocationMaxSimultaneousRebalance.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.max-simultaneous-rebalance"));
        shardingSettings.TuningParameters.WaitingForStateTimeout.Should().Be(shardingConfig.GetTimeSpan("waiting-for-state-timeout"));
        shardingSettings.TuningParameters.UpdatingStateTimeout.Should().Be(shardingConfig.GetTimeSpan("updating-state-timeout"));
        shardingSettings.TuningParameters.EntityRecoveryStrategy.Should().Be(shardingConfig.GetString("entity-recovery-strategy"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyFrequency.Should().Be(shardingConfig.GetTimeSpan("entity-recovery-constant-rate-strategy.frequency"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities.Should().Be(shardingConfig.GetInt("entity-recovery-constant-rate-strategy.number-of-entities"));
        shardingSettings.TuningParameters.CoordinatorStateWriteMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.write-majority-plus"));
        shardingSettings.TuningParameters.CoordinatorStateReadMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.read-majority-plus"));
        shardingSettings.TuningParameters.LeastShardAllocationAbsoluteLimit.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-absolute-limit"));
        shardingSettings.TuningParameters.LeastShardAllocationRelativeLimit.Should().Be(shardingConfig.GetDouble("least-shard-allocation-strategy.rebalance-relative-limit"));

        var singletonConfig = ClusterSingletonManager.DefaultConfig().GetConfig("akka.cluster.singleton");
        shardingSettings.CoordinatorSingletonSettings.SingletonName.Should().Be(singletonConfig.GetString("singleton-name"));
        shardingSettings.CoordinatorSingletonSettings.Role.Should().BeNull();
        // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Tools/Singleton/ClusterSingletonManagerSettings.cs#L58
        shardingSettings.CoordinatorSingletonSettings.RemovalMargin.Should().Be(TimeSpan.Zero);
        shardingSettings.CoordinatorSingletonSettings.HandOverRetryInterval.Should().Be(singletonConfig.GetTimeSpan("hand-over-retry-interval"));
        shardingSettings.CoordinatorSingletonSettings.LeaseSettings.Should().Be(GetLeaseUsageSettings(shardingConfig));
#pragma warning disable CS0618 // Type or member is obsolete
        shardingSettings.CoordinatorSingletonSettings.ConsiderAppVersion.Should().Be(singletonConfig.GetBoolean("consider-app-version"));
#pragma warning restore CS0618 // Type or member is obsolete

        shardingSettings.LeaseSettings.Should().BeNull();

        #endregion

        #region ReplicatorSettings validation
        var repConfig = shardingConfig.GetConfig("distributed-data")
            .WithFallback(DistributedData.DistributedData.DefaultConfig().GetConfig("akka.cluster.distributed-data"));

        replicatorSettings.Role.Should().Be(repConfig.GetString("role"));
        replicatorSettings.GossipInterval.Should().Be(repConfig.GetTimeSpan("gossip-interval"));
        replicatorSettings.NotifySubscribersInterval.Should().Be(repConfig.GetTimeSpan("notify-subscribers-interval"));
        replicatorSettings.MaxDeltaElements.Should().Be(repConfig.GetInt("max-delta-elements"));
        replicatorSettings.Dispatcher.Should().Be("akka.actor.internal-dispatcher");
        replicatorSettings.PruningInterval.Should().Be(repConfig.GetTimeSpan("pruning-interval"));
        replicatorSettings.MaxPruningDissemination.Should().Be(repConfig.GetTimeSpan("max-pruning-dissemination"));
        replicatorSettings.DurableKeys.Should().BeEmpty();
        replicatorSettings.PruningMarkerTimeToLive.Should().Be(repConfig.GetTimeSpan("pruning-marker-time-to-live"));
        replicatorSettings.DurableStoreProps.Should().NotBeNull();
        replicatorSettings.MaxDeltaSize.Should().Be(repConfig.GetInt("delta-crdt.max-delta-size"));
        replicatorSettings.RestartReplicatorOnFailure.Should().Be(repConfig.GetBoolean("recreate-on-failure"));
        replicatorSettings.PreferOldest.Should().Be(repConfig.GetBoolean("prefer-oldest"));
        replicatorSettings.VerboseDebugLogging.Should().Be(repConfig.GetBoolean("verbose-debug-logging"));

        #endregion
    }

    [Fact(DisplayName = "Empty ShardOptions and ShardingDDataOptions with DData should contain default HOCON values")]
    public async Task EmptyDDataShardOptionsTest()
    {
        var shardOptions = new ShardOptions
        {
            RememberEntitiesStore = RememberEntitiesStore.DData,
            RememberEntities = true,
        };
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddAkka("ConfigSys", (builder, _) =>
                {
                    builder
                        .WithRemoting()
                        .WithClustering()
                        .WithShardRegion<MyEntityActor>(
                            typeName: "entities", 
                            entityPropsFactory: (_, _) =>
                            {
                                return s => Props.Create(() => new MyEntityActor(s));
                            }, 
                            messageExtractor: new Extractor(), 
                            shardOptions: shardOptions)
                        .WithShardingDistributedData(new ShardingDDataOptions());
                });
            }).Build();
        await host.StartAsync();

        Config appliedShardingConfig;
        ClusterShardingSettings shardingSettings;
        ReplicatorSettings replicatorSettings;
        try
        {
            var sys = host.Services.GetRequiredService<ActorSystem>();
            (appliedShardingConfig, shardingSettings) = GetClusterShardingSettings(shardOptions, sys);
            replicatorSettings = GetReplicatorSettings(shardingSettings, sys);
        }
        finally
        {
            await host.StopAsync();
        }

        var shardingConfig = ClusterSharding.DefaultConfig().GetConfig("akka.cluster.sharding");

        #region ClusterShardingSettings validation

        shardingSettings.Role.Should().BeNull();
        shardingSettings.RememberEntities.Should().BeTrue();
        shardingSettings.RememberEntitiesStore.Should().Be(RememberEntitiesStore.DData);
        shardingSettings.JournalPluginId.Should().Be(shardingConfig.GetString("journal-plugin-id"));
        shardingSettings.SnapshotPluginId.Should().Be(shardingConfig.GetString("snapshot-plugin-id"));
        shardingSettings.StateStoreMode.Should().Be(Enum.Parse<StateStoreMode>(shardingConfig.GetString("state-store-mode"), true));
        shardingSettings.ShardRegionQueryTimeout.Should().Be(shardingConfig.GetTimeSpan("shard-region-query-timeout"));
        shardingSettings.PassivateIdleEntityAfter.Should().Be(shardingConfig.GetTimeSpan("passivate-idle-entity-after"));
        appliedShardingConfig.GetBoolean("fail-on-invalid-entity-state-transition").Should().Be(shardingConfig.GetBoolean("fail-on-invalid-entity-state-transition"));
        
        shardingSettings.TuningParameters.CoordinatorFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("coordinator-failure-backoff"));
        shardingSettings.TuningParameters.RetryInterval.Should().Be(shardingConfig.GetTimeSpan("retry-interval"));
        shardingSettings.TuningParameters.BufferSize.Should().Be(shardingConfig.GetInt("buffer-size"));
        shardingSettings.TuningParameters.HandOffTimeout.Should().Be(shardingConfig.GetTimeSpan("handoff-timeout"));
        shardingSettings.TuningParameters.ShardStartTimeout.Should().Be(shardingConfig.GetTimeSpan("shard-start-timeout"));
        shardingSettings.TuningParameters.ShardFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("shard-failure-backoff"));
        shardingSettings.TuningParameters.EntityRestartBackoff.Should().Be(shardingConfig.GetTimeSpan("entity-restart-backoff"));
        shardingSettings.TuningParameters.RebalanceInterval.Should().Be(shardingConfig.GetTimeSpan("rebalance-interval"));
        shardingSettings.TuningParameters.SnapshotAfter.Should().Be(shardingConfig.GetInt("snapshot-after"));
        shardingSettings.TuningParameters.KeepNrOfBatches.Should().Be(shardingConfig.GetInt("keep-nr-of-batches"));
        shardingSettings.TuningParameters.LeastShardAllocationRebalanceThreshold.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-threshold"));
        shardingSettings.TuningParameters.LeastShardAllocationMaxSimultaneousRebalance.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.max-simultaneous-rebalance"));
        shardingSettings.TuningParameters.WaitingForStateTimeout.Should().Be(shardingConfig.GetTimeSpan("waiting-for-state-timeout"));
        shardingSettings.TuningParameters.UpdatingStateTimeout.Should().Be(shardingConfig.GetTimeSpan("updating-state-timeout"));
        shardingSettings.TuningParameters.EntityRecoveryStrategy.Should().Be(shardingConfig.GetString("entity-recovery-strategy"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyFrequency.Should().Be(shardingConfig.GetTimeSpan("entity-recovery-constant-rate-strategy.frequency"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities.Should().Be(shardingConfig.GetInt("entity-recovery-constant-rate-strategy.number-of-entities"));
        shardingSettings.TuningParameters.CoordinatorStateWriteMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.write-majority-plus"));
        shardingSettings.TuningParameters.CoordinatorStateReadMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.read-majority-plus"));
        shardingSettings.TuningParameters.LeastShardAllocationAbsoluteLimit.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-absolute-limit"));
        shardingSettings.TuningParameters.LeastShardAllocationRelativeLimit.Should().Be(shardingConfig.GetDouble("least-shard-allocation-strategy.rebalance-relative-limit"));

        var singletonConfig = ClusterSingletonManager.DefaultConfig().GetConfig("akka.cluster.singleton");
        shardingSettings.CoordinatorSingletonSettings.SingletonName.Should().Be(singletonConfig.GetString("singleton-name"));
        shardingSettings.CoordinatorSingletonSettings.Role.Should().BeNull();
        // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Tools/Singleton/ClusterSingletonManagerSettings.cs#L58
        shardingSettings.CoordinatorSingletonSettings.RemovalMargin.Should().Be(TimeSpan.Zero);
        shardingSettings.CoordinatorSingletonSettings.HandOverRetryInterval.Should().Be(singletonConfig.GetTimeSpan("hand-over-retry-interval"));
        shardingSettings.CoordinatorSingletonSettings.LeaseSettings.Should().Be(GetLeaseUsageSettings(shardingConfig));
#pragma warning disable CS0618 // Type or member is obsolete
        shardingSettings.CoordinatorSingletonSettings.ConsiderAppVersion.Should().Be(singletonConfig.GetBoolean("consider-app-version"));
#pragma warning restore CS0618 // Type or member is obsolete

        shardingSettings.LeaseSettings.Should().BeNull();

        #endregion

        #region ReplicatorSettings validation
        var repConfig = shardingConfig.GetConfig("distributed-data")
            .WithFallback(DistributedData.DistributedData.DefaultConfig().GetConfig("akka.cluster.distributed-data"));

        replicatorSettings.Role.Should().Be(repConfig.GetString("role"));
        replicatorSettings.GossipInterval.Should().Be(repConfig.GetTimeSpan("gossip-interval"));
        replicatorSettings.NotifySubscribersInterval.Should().Be(repConfig.GetTimeSpan("notify-subscribers-interval"));
        replicatorSettings.MaxDeltaElements.Should().Be(repConfig.GetInt("max-delta-elements"));
        replicatorSettings.Dispatcher.Should().Be("akka.actor.internal-dispatcher");
        replicatorSettings.PruningInterval.Should().Be(repConfig.GetTimeSpan("pruning-interval"));
        replicatorSettings.MaxPruningDissemination.Should().Be(repConfig.GetTimeSpan("max-pruning-dissemination"));
        replicatorSettings.DurableKeys.Should().BeEquivalentTo("shard-*");
        replicatorSettings.PruningMarkerTimeToLive.Should().Be(repConfig.GetTimeSpan("pruning-marker-time-to-live"));
        replicatorSettings.DurableStoreProps.Should().NotBeNull();
        replicatorSettings.MaxDeltaSize.Should().Be(repConfig.GetInt("delta-crdt.max-delta-size"));
        replicatorSettings.RestartReplicatorOnFailure.Should().Be(repConfig.GetBoolean("recreate-on-failure"));
        replicatorSettings.PreferOldest.Should().Be(repConfig.GetBoolean("prefer-oldest"));
        replicatorSettings.VerboseDebugLogging.Should().Be(repConfig.GetBoolean("verbose-debug-logging"));

        #endregion
    }

    [Fact(DisplayName = "Modified ShardOptions and ShardingDDataOptions without DData should contain proper HOCON values")]
    public async Task ModifiedShardOptionsTest()
    {
        var shardOptions = new ShardOptions
        {
            StateStoreMode = StateStoreMode.DData, 
            RememberEntitiesStore = RememberEntitiesStore.Eventsourced, 
            RememberEntities = true, 
            Role = "test", 
            JournalPluginId = "custom-journal", 
            SnapshotPluginId = "custom-snapshot-store", 
            LeaseImplementation = new TestLeaseOption(), 
            LeaseRetryInterval = 1.Seconds(), 
            HandOffStopMessage = StopMessage.Instance, // can't be tested, assigned directly
            FailOnInvalidEntityStateTransition = true, 
#pragma warning disable CS0618 // Type or member is obsolete
            // This property should never get applied to HOCON
            DistributedData =
            {
                Role = "wrong-role", 
                Name = "wrong-name" 
            },
#pragma warning restore CS0618 // Type or member is obsolete
            ShouldPassivateIdleEntities = false, 
            ShardRegionQueryTimeout = 2.Seconds(), 
            PassivateIdleEntityAfter = 3.Seconds(), 
        };
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddAkka("ConfigSys", (builder, _) =>
                {
                    builder
                        .WithRemoting()
                        .WithClustering(new ClusterOptions
                        {
                            Roles = new[] {"test"}
                        })
                        .WithShardRegion<MyEntityActor>(
                            typeName: "entities", 
                            entityPropsFactory: (_, _) =>
                            {
                                return s => Props.Create(() => new MyEntityActor(s));
                            }, 
                            messageExtractor: new Extractor(), 
                            shardOptions: shardOptions)
                        .WithInMemoryJournal(_ => { }, "custom-journal")
                        .WithInMemorySnapshotStore("custom-snapshot-store")
                        .WithShardingDistributedData(new ShardingDDataOptions
                        {
                            Name = "customReplicator", 
                            Role = "test", 
                            RecreateOnFailure = true, 
                            PreferOldest = false, 
                            VerboseDebugLogging = true, 
                            Durable = new DurableOptions
                            {
                                Keys = new[] {"custom-*"},
                                Lmdb = new LmdbOptions
                                {
                                    Directory = "lmdb",
                                    MapSize = 1024 * 1024
                                }
                            },
                            MajorityMinimumCapacity = 1,
                            MaxDeltaElements = 2, // This setting ("max-delta-elements") never get used in core
                        });
                });
            }).Build();
        await host.StartAsync();

        Config appliedShardingConfig;
        ClusterShardingSettings shardingSettings;
        ReplicatorSettings replicatorSettings;
        try
        {
            var sys = host.Services.GetRequiredService<ActorSystem>();
            (appliedShardingConfig, shardingSettings) = GetClusterShardingSettings(shardOptions, sys);
            replicatorSettings = GetReplicatorSettings(shardingSettings, sys);
        }
        finally
        {
            await host.StopAsync();
        }

        var shardingConfig = ClusterSharding.DefaultConfig().GetConfig("akka.cluster.sharding");

        #region ClusterShardingSettings validation

        shardingSettings.Role.Should().Be("test");
        shardingSettings.RememberEntities.Should().BeTrue();
        shardingSettings.JournalPluginId.Should().Be("custom-journal");
        shardingSettings.SnapshotPluginId.Should().Be("custom-snapshot-store");
        shardingSettings.StateStoreMode.Should().Be(StateStoreMode.DData);
        shardingSettings.RememberEntitiesStore.Should().Be(RememberEntitiesStore.Eventsourced);
        shardingSettings.ShardRegionQueryTimeout.Should().Be(2.Seconds());
        shardingSettings.PassivateIdleEntityAfter.Should().Be(default);
        
        appliedShardingConfig.GetBoolean("fail-on-invalid-entity-state-transition").Should().BeTrue();
        appliedShardingConfig.GetInt("distributed-data.majority-min-cap").Should().Be(1);
        
        shardingSettings.TuningParameters.CoordinatorFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("coordinator-failure-backoff"));
        shardingSettings.TuningParameters.RetryInterval.Should().Be(shardingConfig.GetTimeSpan("retry-interval"));
        shardingSettings.TuningParameters.BufferSize.Should().Be(shardingConfig.GetInt("buffer-size"));
        shardingSettings.TuningParameters.HandOffTimeout.Should().Be(shardingConfig.GetTimeSpan("handoff-timeout"));
        shardingSettings.TuningParameters.ShardStartTimeout.Should().Be(shardingConfig.GetTimeSpan("shard-start-timeout"));
        shardingSettings.TuningParameters.ShardFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("shard-failure-backoff"));
        shardingSettings.TuningParameters.EntityRestartBackoff.Should().Be(shardingConfig.GetTimeSpan("entity-restart-backoff"));
        shardingSettings.TuningParameters.RebalanceInterval.Should().Be(shardingConfig.GetTimeSpan("rebalance-interval"));
        shardingSettings.TuningParameters.SnapshotAfter.Should().Be(shardingConfig.GetInt("snapshot-after"));
        shardingSettings.TuningParameters.KeepNrOfBatches.Should().Be(shardingConfig.GetInt("keep-nr-of-batches"));
        shardingSettings.TuningParameters.LeastShardAllocationRebalanceThreshold.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-threshold"));
        shardingSettings.TuningParameters.LeastShardAllocationMaxSimultaneousRebalance.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.max-simultaneous-rebalance"));
        shardingSettings.TuningParameters.WaitingForStateTimeout.Should().Be(shardingConfig.GetTimeSpan("waiting-for-state-timeout"));
        shardingSettings.TuningParameters.UpdatingStateTimeout.Should().Be(shardingConfig.GetTimeSpan("updating-state-timeout"));
        shardingSettings.TuningParameters.EntityRecoveryStrategy.Should().Be(shardingConfig.GetString("entity-recovery-strategy"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyFrequency.Should().Be(shardingConfig.GetTimeSpan("entity-recovery-constant-rate-strategy.frequency"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities.Should().Be(shardingConfig.GetInt("entity-recovery-constant-rate-strategy.number-of-entities"));
        shardingSettings.TuningParameters.CoordinatorStateWriteMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.write-majority-plus"));
        shardingSettings.TuningParameters.CoordinatorStateReadMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.read-majority-plus"));
        shardingSettings.TuningParameters.LeastShardAllocationAbsoluteLimit.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-absolute-limit"));
        shardingSettings.TuningParameters.LeastShardAllocationRelativeLimit.Should().Be(shardingConfig.GetDouble("least-shard-allocation-strategy.rebalance-relative-limit"));

        var singletonConfig = ClusterSingletonManager.DefaultConfig().GetConfig("akka.cluster.singleton");
        shardingSettings.CoordinatorSingletonSettings.SingletonName.Should().Be(singletonConfig.GetString("singleton-name"));
        shardingSettings.CoordinatorSingletonSettings.Role.Should().BeNull();
        // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Tools/Singleton/ClusterSingletonManagerSettings.cs#L58
        shardingSettings.CoordinatorSingletonSettings.RemovalMargin.Should().Be(TimeSpan.Zero);
        shardingSettings.CoordinatorSingletonSettings.HandOverRetryInterval.Should().Be(singletonConfig.GetTimeSpan("hand-over-retry-interval"));
        shardingSettings.CoordinatorSingletonSettings.LeaseSettings.Should().Be(GetLeaseUsageSettings(shardingConfig));
#pragma warning disable CS0618 // Type or member is obsolete
        shardingSettings.CoordinatorSingletonSettings.ConsiderAppVersion.Should().Be(singletonConfig.GetBoolean("consider-app-version"));
#pragma warning restore CS0618 // Type or member is obsolete

        shardingSettings.LeaseSettings.Should().BeEquivalentTo(new LeaseUsageSettings("test-lease", 1.Seconds()));

        #endregion

        #region ReplicatorSettings validation
        var repConfig = shardingConfig.GetConfig("distributed-data")
            .WithFallback(DistributedData.DistributedData.DefaultConfig().GetConfig("akka.cluster.distributed-data"));

        appliedShardingConfig.GetString("distributed-data.name").Should().NotBe("wrong-name").And.Be("customReplicator");
        replicatorSettings.Role.Should().NotBe("wrong-role").And.Be("test");
        replicatorSettings.GossipInterval.Should().Be(repConfig.GetTimeSpan("gossip-interval"));
        replicatorSettings.NotifySubscribersInterval.Should().Be(repConfig.GetTimeSpan("notify-subscribers-interval"));
        replicatorSettings.MaxDeltaElements.Should().Be(2);
        replicatorSettings.Dispatcher.Should().Be("akka.actor.internal-dispatcher");
        replicatorSettings.PruningInterval.Should().Be(repConfig.GetTimeSpan("pruning-interval"));
        replicatorSettings.MaxPruningDissemination.Should().Be(repConfig.GetTimeSpan("max-pruning-dissemination"));
        replicatorSettings.DurableKeys.Should().BeEmpty();
        replicatorSettings.PruningMarkerTimeToLive.Should().Be(repConfig.GetTimeSpan("pruning-marker-time-to-live"));
        replicatorSettings.DurableStoreProps.Should().NotBeNull();
        replicatorSettings.MaxDeltaSize.Should().Be(repConfig.GetInt("delta-crdt.max-delta-size"));
        replicatorSettings.RestartReplicatorOnFailure.Should().BeTrue();
        replicatorSettings.PreferOldest.Should().BeFalse();
        replicatorSettings.VerboseDebugLogging.Should().BeTrue();

        appliedShardingConfig.GetString("distributed-data.durable.lmdb.dir").Should().Be("lmdb");
        appliedShardingConfig.GetLong("distributed-data.durable.lmdb.map-size").Should().Be(1024 * 1024);
        
        #endregion
    }
    
    [Fact(DisplayName = "Modified ShardOptions and ShardingDDataOptions with DData should contain proper HOCON values")]
    public async Task ModifiedDDataShardOptionsTest()
    {
        var shardOptions = new ShardOptions
        {
            StateStoreMode = StateStoreMode.DData, 
            RememberEntitiesStore = RememberEntitiesStore.DData, 
            RememberEntities = true, 
            Role = "test", 
            JournalPluginId = "custom-journal", 
            SnapshotPluginId = "custom-snapshot-store", 
            LeaseImplementation = new TestLeaseOption(), 
            LeaseRetryInterval = 1.Seconds(), 
            HandOffStopMessage = StopMessage.Instance, // can't be tested, assigned directly
            FailOnInvalidEntityStateTransition = true, 
#pragma warning disable CS0618 // Type or member is obsolete
            // This property should never get applied to HOCON
            DistributedData =
            {
                Role = "wrong-role", 
                Name = "wrong-name" 
            },
#pragma warning restore CS0618 // Type or member is obsolete
            ShouldPassivateIdleEntities = false, 
            ShardRegionQueryTimeout = 2.Seconds(), 
            PassivateIdleEntityAfter = 3.Seconds(), 
        };
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddAkka("ConfigSys", (builder, _) =>
                {
                    builder
                        .WithRemoting()
                        .WithClustering(new ClusterOptions
                        {
                            Roles = new[] {"test"}
                        })
                        .WithShardRegion<MyEntityActor>(
                            typeName: "entities", 
                            entityPropsFactory: (_, _) =>
                            {
                                return s => Props.Create(() => new MyEntityActor(s));
                            }, 
                            messageExtractor: new Extractor(), 
                            shardOptions: shardOptions)
                        .WithInMemoryJournal(_ => { }, "custom-journal")
                        .WithInMemorySnapshotStore("custom-snapshot-store")
                        .WithShardingDistributedData(new ShardingDDataOptions
                        {
                            Name = "customReplicator", 
                            Role = "test", 
                            RecreateOnFailure = true, 
                            PreferOldest = false, 
                            VerboseDebugLogging = true, 
                            Durable = new DurableOptions
                            {
                                Keys = new[] {"custom-*"},
                                Lmdb = new LmdbOptions
                                {
                                    Directory = "lmdb",
                                    MapSize = 1024 * 1024
                                }
                            },
                            MajorityMinimumCapacity = 1,
                            MaxDeltaElements = 2, // This setting ("max-delta-elements") never get used in core
                        });
                });
            }).Build();
        await host.StartAsync();

        Config appliedShardingConfig;
        ClusterShardingSettings shardingSettings;
        ReplicatorSettings replicatorSettings;
        try
        {
            var sys = host.Services.GetRequiredService<ActorSystem>();
            (appliedShardingConfig, shardingSettings) = GetClusterShardingSettings(shardOptions, sys);
            replicatorSettings = GetReplicatorSettings(shardingSettings, sys);
        }
        finally
        {
            await host.StopAsync();
        }

        var shardingConfig = ClusterSharding.DefaultConfig().GetConfig("akka.cluster.sharding");

        #region ClusterShardingSettings validation

        shardingSettings.Role.Should().Be("test");
        shardingSettings.RememberEntities.Should().BeTrue();
        shardingSettings.JournalPluginId.Should().Be("custom-journal");
        shardingSettings.SnapshotPluginId.Should().Be("custom-snapshot-store");
        shardingSettings.StateStoreMode.Should().Be(StateStoreMode.DData);
        shardingSettings.RememberEntitiesStore.Should().Be(RememberEntitiesStore.DData);
        shardingSettings.ShardRegionQueryTimeout.Should().Be(2.Seconds());
        shardingSettings.PassivateIdleEntityAfter.Should().Be(default);
        
        appliedShardingConfig.GetBoolean("fail-on-invalid-entity-state-transition").Should().BeTrue();
        appliedShardingConfig.GetInt("distributed-data.majority-min-cap").Should().Be(1);
        
        shardingSettings.TuningParameters.CoordinatorFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("coordinator-failure-backoff"));
        shardingSettings.TuningParameters.RetryInterval.Should().Be(shardingConfig.GetTimeSpan("retry-interval"));
        shardingSettings.TuningParameters.BufferSize.Should().Be(shardingConfig.GetInt("buffer-size"));
        shardingSettings.TuningParameters.HandOffTimeout.Should().Be(shardingConfig.GetTimeSpan("handoff-timeout"));
        shardingSettings.TuningParameters.ShardStartTimeout.Should().Be(shardingConfig.GetTimeSpan("shard-start-timeout"));
        shardingSettings.TuningParameters.ShardFailureBackoff.Should().Be(shardingConfig.GetTimeSpan("shard-failure-backoff"));
        shardingSettings.TuningParameters.EntityRestartBackoff.Should().Be(shardingConfig.GetTimeSpan("entity-restart-backoff"));
        shardingSettings.TuningParameters.RebalanceInterval.Should().Be(shardingConfig.GetTimeSpan("rebalance-interval"));
        shardingSettings.TuningParameters.SnapshotAfter.Should().Be(shardingConfig.GetInt("snapshot-after"));
        shardingSettings.TuningParameters.KeepNrOfBatches.Should().Be(shardingConfig.GetInt("keep-nr-of-batches"));
        shardingSettings.TuningParameters.LeastShardAllocationRebalanceThreshold.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-threshold"));
        shardingSettings.TuningParameters.LeastShardAllocationMaxSimultaneousRebalance.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.max-simultaneous-rebalance"));
        shardingSettings.TuningParameters.WaitingForStateTimeout.Should().Be(shardingConfig.GetTimeSpan("waiting-for-state-timeout"));
        shardingSettings.TuningParameters.UpdatingStateTimeout.Should().Be(shardingConfig.GetTimeSpan("updating-state-timeout"));
        shardingSettings.TuningParameters.EntityRecoveryStrategy.Should().Be(shardingConfig.GetString("entity-recovery-strategy"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyFrequency.Should().Be(shardingConfig.GetTimeSpan("entity-recovery-constant-rate-strategy.frequency"));
        shardingSettings.TuningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities.Should().Be(shardingConfig.GetInt("entity-recovery-constant-rate-strategy.number-of-entities"));
        shardingSettings.TuningParameters.CoordinatorStateWriteMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.write-majority-plus"));
        shardingSettings.TuningParameters.CoordinatorStateReadMajorityPlus.Should().Be(ConfigMajorityPlus(shardingConfig, "coordinator-state.read-majority-plus"));
        shardingSettings.TuningParameters.LeastShardAllocationAbsoluteLimit.Should().Be(shardingConfig.GetInt("least-shard-allocation-strategy.rebalance-absolute-limit"));
        shardingSettings.TuningParameters.LeastShardAllocationRelativeLimit.Should().Be(shardingConfig.GetDouble("least-shard-allocation-strategy.rebalance-relative-limit"));

        var singletonConfig = ClusterSingletonManager.DefaultConfig().GetConfig("akka.cluster.singleton");
        shardingSettings.CoordinatorSingletonSettings.SingletonName.Should().Be(singletonConfig.GetString("singleton-name"));
        shardingSettings.CoordinatorSingletonSettings.Role.Should().BeNull();
        // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Tools/Singleton/ClusterSingletonManagerSettings.cs#L58
        shardingSettings.CoordinatorSingletonSettings.RemovalMargin.Should().Be(TimeSpan.Zero);
        shardingSettings.CoordinatorSingletonSettings.HandOverRetryInterval.Should().Be(singletonConfig.GetTimeSpan("hand-over-retry-interval"));
        shardingSettings.CoordinatorSingletonSettings.LeaseSettings.Should().Be(GetLeaseUsageSettings(shardingConfig));
#pragma warning disable CS0618 // Type or member is obsolete
        shardingSettings.CoordinatorSingletonSettings.ConsiderAppVersion.Should().Be(singletonConfig.GetBoolean("consider-app-version"));
#pragma warning restore CS0618 // Type or member is obsolete

        shardingSettings.LeaseSettings.Should().BeEquivalentTo(new LeaseUsageSettings("test-lease", 1.Seconds()));

        #endregion

        #region ReplicatorSettings validation
        var repConfig = shardingConfig.GetConfig("distributed-data")
            .WithFallback(DistributedData.DistributedData.DefaultConfig().GetConfig("akka.cluster.distributed-data"));

        appliedShardingConfig.GetString("distributed-data.name").Should().NotBe("wrong-name").And.Be("customReplicator");
        replicatorSettings.Role.Should().NotBe("wrong-role").And.Be("test");
        replicatorSettings.GossipInterval.Should().Be(repConfig.GetTimeSpan("gossip-interval"));
        replicatorSettings.NotifySubscribersInterval.Should().Be(repConfig.GetTimeSpan("notify-subscribers-interval"));
        replicatorSettings.MaxDeltaElements.Should().Be(2);
        replicatorSettings.Dispatcher.Should().Be("akka.actor.internal-dispatcher");
        replicatorSettings.PruningInterval.Should().Be(repConfig.GetTimeSpan("pruning-interval"));
        replicatorSettings.MaxPruningDissemination.Should().Be(repConfig.GetTimeSpan("max-pruning-dissemination"));
        replicatorSettings.DurableKeys.Should().BeEquivalentTo("custom-*");
        replicatorSettings.PruningMarkerTimeToLive.Should().Be(repConfig.GetTimeSpan("pruning-marker-time-to-live"));
        replicatorSettings.DurableStoreProps.Should().NotBeNull();
        replicatorSettings.MaxDeltaSize.Should().Be(repConfig.GetInt("delta-crdt.max-delta-size"));
        replicatorSettings.RestartReplicatorOnFailure.Should().BeTrue();
        replicatorSettings.PreferOldest.Should().BeFalse();
        replicatorSettings.VerboseDebugLogging.Should().BeTrue();

        appliedShardingConfig.GetString("distributed-data.durable.lmdb.dir").Should().Be("lmdb");
        appliedShardingConfig.GetLong("distributed-data.durable.lmdb.map-size").Should().Be(1024 * 1024);
        
        #endregion
    }

    #region Helper methods

    // This is how ShardSettings is created in Akka.Cluster.Hosting
    // https://github.com/akkadotnet/Akka.Hosting/blob/2f63b5d14b1664003f166a3f30a913dac1428104/src/Akka.Cluster.Hosting/AkkaClusterHostingExtensions.cs#L977-L982
    private static (Config, ClusterShardingSettings) GetClusterShardingSettings(ShardOptions shardOptions, ActorSystem system)
    {
        var shardingConfig = ConfigurationFactory.ParseString(shardOptions.ToString())
            .WithFallback(system.Settings.Config.GetConfig("akka.cluster.sharding"));
        var coordinatorConfig = system.Settings.Config.GetConfig(
            shardingConfig.GetString("coordinator-singleton"));
                
        return (shardingConfig, ClusterShardingSettings.Create(shardingConfig, coordinatorConfig));
    }
    
    // Copied from Akka core code
    // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Sharding/ClusterShardingSettings.cs#L404-L407
    private static LeaseUsageSettings? GetLeaseUsageSettings(Config config)
    {
        LeaseUsageSettings? lease = null;
        var leaseConfigPath = config.GetString("use-lease");
        if (!string.IsNullOrEmpty(leaseConfigPath))
            lease = new LeaseUsageSettings(leaseConfigPath, config.GetTimeSpan("lease-retry-interval"));

        return lease;
    }
    
    // Copied from Akka core code
    // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Sharding/ClusterShardingSettings.cs#L422-L427
    private static int ConfigMajorityPlus(Config config, string p)
    {
        if (config.GetString(p)?.ToLowerInvariant() == "all")
            return int.MaxValue;
        return config.GetInt(p);
    }
    
    // Copied from Akka core code
    // This is how sharding replicator settings is populated in core
    // https://github.com/akkadotnet/akka.net/blob/4ae47927da9f2539742c336acfa8ae0037fabbb7/src/contrib/cluster/Akka.Cluster.Sharding/ClusterShardingGuardian.cs#L300-L310
    private static ReplicatorSettings GetReplicatorSettings(ClusterShardingSettings shardingSettings, ActorSystem system)
    {
        var config = system.Settings.Config.GetConfig("akka.cluster.sharding.distributed-data")
            .WithFallback(system.Settings.Config.GetConfig("akka.cluster.distributed-data"));
        var configuredSettings = ReplicatorSettings.Create(config);
        var settingsWithRoles = configuredSettings.WithRole(shardingSettings.Role);
        if (shardingSettings.RememberEntities && shardingSettings.RememberEntitiesStore == RememberEntitiesStore.DData)
            return settingsWithRoles; // only enable durable keys when using DData for remember-entities
        else
            return settingsWithRoles.WithDurableKeys(ImmutableHashSet<string>.Empty);
    }

    #endregion
}