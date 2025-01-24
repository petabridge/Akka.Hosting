//-----------------------------------------------------------------------
// <copyright file="AwaitAssertTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util.Internal;
using Xunit;
using Xunit.Sdk;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Akka.Hosting.TestKit.Tests.TestKitBaseTests;

public class AwaitAssertTests : TestKit
{
    protected override Config Config { get; } = "akka.test.timefactor=2";

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public void AwaitAssert_must_not_throw_any_exception_when_assertion_is_valid()
    {
        AwaitAssert(() => Assert.Equal("foo", "foo"));
    }

    [Fact(DisplayName = "AwaitAssertAsync must not throw any exception when assertion is valid")]
    public async Task AwaitAssertAsync_must_not_throw_any_exception_when_assertion_is_valid()
    {
        await AwaitAssertAsync(() => Assert.Equal("foo", "foo"));
    }

    [Fact]
    public void AwaitAssert_must_throw_exception_when_assertion_is_invalid()
    {
        Within(TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(1), () =>
        {
            Assert.Throws<EqualException>(() =>
                AwaitAssert(() => Assert.Equal("foo", "bar"), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(300)));
        });
    }
    
    [Fact(DisplayName = "AwaitAssertAsync must throw exception when assertion is invalid")]
    public async Task AwaitAssertAsync_must_throw_exception_when_assertion_is_invalid()
    {
        await WithinAsync(TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(1), async () =>
        {
            await Awaiting(async () =>
            {
                await AwaitAssertAsync(() => Assert.Equal("foo", "bar"), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(300));
            }).Should().ThrowExactlyAsync<EqualException>();
        });
    }

    [Fact(DisplayName = "AwaitAssertAsync should repeatedly call the assert action while it is failing")]
    public async Task AwaitAssertAsyncMultiCallTest()
    {
        var counter = new AtomicCounter(0);
        var testActor = Sys.ActorOf(Props.Create(() => new AwaitAssertTestActor(counter)));

        await AwaitAssertAsync(async () =>
            {
                var r = await testActor.Ask<string>("count", TimeSpan.FromMilliseconds(100));
                r.Should().Be("OK");
            },
            TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(200));
        counter.Current.Should().Be(4);
    }
    
    private class AwaitAssertTestActor : ReceiveActor
    {
        public AwaitAssertTestActor(AtomicCounter counter)
        {
            ReceiveAny(_ =>
            {
                var count = counter.GetAndIncrement();
                if(count < 3)
                    Sender.Tell("NOK");
                else
                    Sender.Tell("OK");
            });
        }
    }
}