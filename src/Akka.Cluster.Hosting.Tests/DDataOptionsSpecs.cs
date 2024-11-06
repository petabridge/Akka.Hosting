using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Cluster.Hosting.Tests;

public class DDataOptionsSpecs
{
    public DDataOptionsSpecs(ITestOutputHelper output)
    {
        Output = output;
    }

    public ITestOutputHelper Output { get; }

    public static readonly TheoryData<DDataOptions> DDataOptionsTypes = new TheoryData<DDataOptions>()
    {
        new DDataOptions() // empty
        {
            Durable = new DurableOptions()
            {
                Keys = []
            }
        },
        new DDataOptions() // null
        {
            Durable = new DurableOptions()
            {
                Keys = null
            }
        },
    };

    /// <summary>
    /// Reproduction for https://github.com/akkadotnet/Akka.Hosting/issues/512 
    /// </summary>
    [Theory]
    [MemberData(nameof(DDataOptionsTypes))]
    public async Task Should_not_emit_durable_keys_when_empty(DDataOptions options)
    {
        // arrange
        using var host = await TestHelper.CreateHost(builder => { builder.WithDistributedData(options); },
            new ClusterOptions() { Roles = new[] { "my-host" } }, Output);

        var actorSystem = host.Services.GetRequiredService<ActorSystem>();

        // act
        var config = actorSystem.Settings.Config.GetConfig("akka.cluster.distributed-data");

        // assert
        Assert.True(config.HasPath("durable.keys"));
        var keys = config.GetStringList("durable.keys");
        Assert.Empty(keys);
    }
}