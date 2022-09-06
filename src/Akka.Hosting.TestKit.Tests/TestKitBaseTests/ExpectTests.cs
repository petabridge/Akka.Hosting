﻿//-----------------------------------------------------------------------
// <copyright file="ExpectTests.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Akka.Actor;
using static FluentAssertions.FluentActions;

namespace Akka.Hosting.TestKit.Tests.TestKitBaseTests
{
    public class ExpectTests : HostingSpec
    {
        public ExpectTests(ITestOutputHelper output) : base(nameof(ExpectTests), output)
        {
        }

        protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
        {
        }
        
        [Fact]
        public async Task ExpectMsgAllOfAsync_should_receive_correct_messages()
        {
            TestActor.Tell("1");
            TestActor.Tell("2");
            TestActor.Tell("3");
            TestActor.Tell("4");
            var messages = await ExpectMsgAllOfAsync(new[] { "3", "1", "4", "2" }).ToListAsync();
            messages.Should().BeEquivalentTo(new[] { "1", "2", "3", "4" }, opt => opt.WithStrictOrdering());
        }

        [Fact]
        public async Task ExpectMsgAllOfAsync_should_fail_when_receiving_unexpected()
        {
            TestActor.Tell("1");
            TestActor.Tell("2");
            TestActor.Tell("Totally unexpected");
            TestActor.Tell("3");
            await Awaiting(async () =>
            {
                await ExpectMsgAllOfAsync(new[] { "3", "1", "2" }).ToListAsync();
            }).Should().ThrowAsync<XunitException>().WithMessage("not found [*");
        }

        [Fact]
        public async Task ExpectMsgAllOfAsync_should_timeout_when_not_receiving_any_messages()
        {
            await Awaiting(async () =>
            {
                await ExpectMsgAllOfAsync(TimeSpan.FromMilliseconds(100), new[] { "3", "1", "2" }).ToListAsync();
            }).Should().ThrowAsync<XunitException>().WithMessage("Timeout (*");
        }

        [Fact]
        public async Task ExpectMsgAllOfAsync_should_timeout_if_to_few_messages()
        {
            TestActor.Tell("1");
            TestActor.Tell("2");
            await Awaiting(async () =>
            {
                await ExpectMsgAllOfAsync(TimeSpan.FromMilliseconds(100), new[] { "3", "1", "2" }).ToListAsync();
            }).Should().ThrowAsync<XunitException>().WithMessage("Timeout (*");
        }

    }
}

