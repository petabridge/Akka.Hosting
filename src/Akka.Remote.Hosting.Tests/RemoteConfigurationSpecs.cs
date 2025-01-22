﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Remote.Transport.DotNetty;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Akka.Remote.Hosting.Tests;

public class RemoteConfigurationSpecs
{
    [Fact(DisplayName = "Empty WithRemoting should return default remoting settings")]
    public async Task EmptyWithRemotingConfigTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting();
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var transportFailureDetector = config.GetConfig("akka.remote.transport-failure-detector");
        var watchFailureDetector = config.GetConfig("akka.remote.watch-failure-detector");
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().BeEmpty();
        tcpConfig.GetInt("port").Should().Be(2552);
        tcpConfig.GetString("public-hostname").Should().BeEmpty();
        tcpConfig.GetInt("public-port").Should().Be(0);
        tcpConfig.GetByteSize("send-buffer-size").Should().Be(256000);
        tcpConfig.GetByteSize("receive-buffer-size").Should().Be(256000);
        tcpConfig.GetByteSize("maximum-frame-size").Should().Be(128000);
        tcpConfig.GetBoolean("enable-ssl").Should().BeFalse();

        transportFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(4.Seconds());
        transportFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(120.Seconds());

        watchFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.Seconds());
        watchFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(10.Seconds());
        watchFailureDetector.GetDouble("threshold").Should().Be(10.0);
        watchFailureDetector.GetInt("max-sample-size").Should().Be(200);
        watchFailureDetector.GetTimeSpan("min-std-deviation").Should().Be(100.Milliseconds());
        watchFailureDetector.GetTimeSpan("unreachable-nodes-reaper-interval").Should().Be(1.Seconds());
        watchFailureDetector.GetTimeSpan("expected-response-after").Should().Be(1.Seconds());
    }
    
    [Fact(DisplayName = "Empty WithRemoting should return default remoting settings")]
    public async Task WithRemotingWithEmptyOptionsConfigTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(new RemoteOptions());
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var transportFailureDetector = config.GetConfig("akka.remote.transport-failure-detector");
        var watchFailureDetector = config.GetConfig("akka.remote.watch-failure-detector");
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().BeEmpty();
        tcpConfig.GetInt("port").Should().Be(2552);
        tcpConfig.GetString("public-hostname").Should().BeEmpty();
        tcpConfig.GetInt("public-port").Should().Be(0);
        tcpConfig.GetByteSize("send-buffer-size").Should().Be(256000);
        tcpConfig.GetByteSize("receive-buffer-size").Should().Be(256000);
        tcpConfig.GetByteSize("maximum-frame-size").Should().Be(128000);
        tcpConfig.GetBoolean("enable-ssl").Should().BeFalse();

        transportFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(4.Seconds());
        transportFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(120.Seconds());

        watchFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.Seconds());
        watchFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(10.Seconds());
        watchFailureDetector.GetDouble("threshold").Should().Be(10.0);
        watchFailureDetector.GetInt("max-sample-size").Should().Be(200);
        watchFailureDetector.GetTimeSpan("min-std-deviation").Should().Be(100.Milliseconds());
        watchFailureDetector.GetTimeSpan("unreachable-nodes-reaper-interval").Should().Be(1.Seconds());
        watchFailureDetector.GetTimeSpan("expected-response-after").Should().Be(1.Seconds());
    }
    
    [Fact(DisplayName = "WithRemoting should override remote settings")]
    public async Task WithRemotingConfigTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting("0.0.0.0", 0, "localhost", 12345);
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().Be("0.0.0.0");
        tcpConfig.GetInt("port").Should().Be(0);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
    
    [Fact(DisplayName = "WithRemoting with RemoteOptions should override remote settings")]
    public async Task WithRemotingOptionsTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(new RemoteOptions
                {
                    HostName = "0.0.0.0", 
                    Port = 0, 
                    PublicHostName = "localhost",
                    PublicPort = 12345,
                    SendBufferSize = 1024000,
                    ReceiveBufferSize = 512000,
                    MaxFrameSize = 256000,
                    TransportFailureDetector = new DeadlineFailureDetectorOptions
                    {
                        HeartbeatInterval = 1.1.Seconds(),
                        AcceptableHeartbeatPause = 1.2.Seconds(),
                    },
                    WatchFailureDetector = new PhiAccrualFailureDetectorOptions
                    {
                        HeartbeatInterval = 1.3.Seconds(),
                        AcceptableHeartbeatPause = 1.4.Seconds(),
                        Threshold = 1.5,
                        MaxSampleSize = 1,
                        MinStandardDeviation = 1.6.Seconds(),
                        UnreachableNodesReaperInterval = 1.7.Seconds(),
                        ExpectedResponseAfter = 1.8.Seconds()
                    }
                });
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var transportFailureDetector = config.GetConfig("akka.remote.transport-failure-detector");
        var watchFailureDetector = config.GetConfig("akka.remote.watch-failure-detector");
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().Be("0.0.0.0");
        tcpConfig.GetInt("port").Should().Be(0);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
        tcpConfig.GetByteSize("send-buffer-size").Should().Be(1024000);
        tcpConfig.GetByteSize("receive-buffer-size").Should().Be(512000);
        tcpConfig.GetByteSize("maximum-frame-size").Should().Be(256000);
        
        transportFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.1.Seconds());
        transportFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.2.Seconds());

        watchFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.3.Seconds());
        watchFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.4.Seconds());
        watchFailureDetector.GetDouble("threshold").Should().Be(1.5);
        watchFailureDetector.GetInt("max-sample-size").Should().Be(1);
        watchFailureDetector.GetTimeSpan("min-std-deviation").Should().Be(1.6.Seconds());
        watchFailureDetector.GetTimeSpan("unreachable-nodes-reaper-interval").Should().Be(1.7.Seconds());
        watchFailureDetector.GetTimeSpan("expected-response-after").Should().Be(1.8.Seconds());
    }
    
    [Fact(DisplayName = "WithRemoting should override remote settings that are overriden")]
    public async Task WithRemotingConfigOverrideTest()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(publicHostname: "localhost", publicPort:12345);
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().BeEmpty();
        tcpConfig.GetInt("port").Should().Be(2552);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
    
    [Fact(DisplayName = "RemoteOptions should override remote settings that are overriden")]
    public void WithRemotingOptionsOverrideTest()
    {
        // arrange
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "test");
        builder.WithRemoting(new RemoteOptions
        {
            HostName = "a",
            PublicHostName = "b",
            Port = 123,
            PublicPort = 456,
            EnableSsl = true,
            SendBufferSize = 256,
            ReceiveBufferSize = 512,
            MaxFrameSize = 128,
            Ssl = new SslOptions
            {
                SuppressValidation = true,
                CertificateOptions = new SslCertificateOptions
                {
                    Path = "c",
                    Password = "d",
                    UseThumbprintOverFile = true,
                    Thumbprint = "e",
                    StoreName = "f",
                    StoreLocation = "g",
                }
            },
            TransportFailureDetector = new DeadlineFailureDetectorOptions
            {
                HeartbeatInterval = 1.1.Seconds(),
                AcceptableHeartbeatPause = 1.2.Seconds(),
            },
            WatchFailureDetector = new PhiAccrualFailureDetectorOptions
            {
                HeartbeatInterval = 1.3.Seconds(),
                AcceptableHeartbeatPause = 1.4.Seconds(),
                Threshold = 1.5,
                MaxSampleSize = 1,
                MinStandardDeviation = 1.6.Seconds(),
                UnreachableNodesReaperInterval = 1.7.Seconds(),
                ExpectedResponseAfter = 1.8.Seconds()
            }
        });
        
        // act
        var config = builder.Configuration.Value;
        var transportFailureDetector = config.GetConfig("akka.remote.transport-failure-detector");
        var watchFailureDetector = config.GetConfig("akka.remote.watch-failure-detector");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        tcpConfig.GetString("hostname").Should().Be("a");
        tcpConfig.GetInt("port").Should().Be(123);
        tcpConfig.GetString("public-hostname").Should().Be("b");
        tcpConfig.GetInt("public-port").Should().Be(456);
        tcpConfig.GetByteSize("send-buffer-size").Should().Be(256);
        tcpConfig.GetByteSize("receive-buffer-size").Should().Be(512);
        tcpConfig.GetByteSize("maximum-frame-size").Should().Be(128);
        
        var sslConfig = tcpConfig.GetConfig("ssl");
        sslConfig.GetBoolean("suppress-validation").Should().BeTrue();
        
        var certConfig = sslConfig.GetConfig("certificate");
        certConfig.GetString("path").Should().Be("c");
        certConfig.GetString("password").Should().Be("d");
        certConfig.GetBoolean("use-thumbprint-over-file").Should().BeTrue();
        certConfig.GetString("thumbprint").Should().Be("e");
        certConfig.GetString("store-name").Should().Be("f");
        certConfig.GetString("store-location").Should().Be("g");
        
        transportFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.1.Seconds());
        transportFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.2.Seconds());

        watchFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.3.Seconds());
        watchFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.4.Seconds());
        watchFailureDetector.GetDouble("threshold").Should().Be(1.5);
        watchFailureDetector.GetInt("max-sample-size").Should().Be(1);
        watchFailureDetector.GetTimeSpan("min-std-deviation").Should().Be(1.6.Seconds());
        watchFailureDetector.GetTimeSpan("unreachable-nodes-reaper-interval").Should().Be(1.7.Seconds());
        watchFailureDetector.GetTimeSpan("expected-response-after").Should().Be(1.8.Seconds());
    }
    
    [Fact(DisplayName = "RemoteOptions using configurator should override remote settings that are overriden")]
    public void WithRemotingOptionsConfiguratorOverrideTest()
    {
        // arrange
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "test");
        builder.WithRemoting(opt =>
        {
            opt.HostName = "a";
            opt.PublicHostName = "b";
            opt.Port = 123;
            opt.PublicPort = 456;
            opt.EnableSsl = true;
            opt.SendBufferSize = 256;
            opt.ReceiveBufferSize = 512;
            opt.MaxFrameSize = 128;
            opt.Ssl.SuppressValidation = true;
            opt.Ssl.CertificateOptions.Path = "c";
            opt.Ssl.CertificateOptions.Password = "d";
            opt.Ssl.CertificateOptions.UseThumbprintOverFile = true;
            opt.Ssl.CertificateOptions.Thumbprint = "e";
            opt.Ssl.CertificateOptions.StoreName = "f";
            opt.Ssl.CertificateOptions.StoreLocation = "g";
            opt.TransportFailureDetector = new DeadlineFailureDetectorOptions
            {
                HeartbeatInterval = 1.1.Seconds(),
                AcceptableHeartbeatPause = 1.2.Seconds(),
            };
            opt.WatchFailureDetector = new PhiAccrualFailureDetectorOptions
            {
                HeartbeatInterval = 1.3.Seconds(),
                AcceptableHeartbeatPause = 1.4.Seconds(),
                Threshold = 1.5,
                MaxSampleSize = 1,
                MinStandardDeviation = 1.6.Seconds(),
                UnreachableNodesReaperInterval = 1.7.Seconds(),
                ExpectedResponseAfter = 1.8.Seconds()
            };
        });
        
        // act
        var config = builder.Configuration.Value;
        var transportFailureDetector = config.GetConfig("akka.remote.transport-failure-detector");
        var watchFailureDetector = config.GetConfig("akka.remote.watch-failure-detector");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        tcpConfig.GetString("hostname").Should().Be("a");
        tcpConfig.GetInt("port").Should().Be(123);
        tcpConfig.GetString("public-hostname").Should().Be("b");
        tcpConfig.GetInt("public-port").Should().Be(456);
        tcpConfig.GetByteSize("send-buffer-size").Should().Be(256);
        tcpConfig.GetByteSize("receive-buffer-size").Should().Be(512);
        tcpConfig.GetByteSize("maximum-frame-size").Should().Be(128);
        
        var sslConfig = tcpConfig.GetConfig("ssl");
        sslConfig.GetBoolean("suppress-validation").Should().BeTrue();
        
        var certConfig = sslConfig.GetConfig("certificate");
        certConfig.GetString("path").Should().Be("c");
        certConfig.GetString("password").Should().Be("d");
        certConfig.GetBoolean("use-thumbprint-over-file").Should().BeTrue();
        certConfig.GetString("thumbprint").Should().Be("e");
        certConfig.GetString("store-name").Should().Be("f");
        certConfig.GetString("store-location").Should().Be("g");
        
        transportFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.1.Seconds());
        transportFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.2.Seconds());

        watchFailureDetector.GetTimeSpan("heartbeat-interval").Should().Be(1.3.Seconds());
        watchFailureDetector.GetTimeSpan("acceptable-heartbeat-pause").Should().Be(1.4.Seconds());
        watchFailureDetector.GetDouble("threshold").Should().Be(1.5);
        watchFailureDetector.GetInt("max-sample-size").Should().Be(1);
        watchFailureDetector.GetTimeSpan("min-std-deviation").Should().Be(1.6.Seconds());
        watchFailureDetector.GetTimeSpan("unreachable-nodes-reaper-interval").Should().Be(1.7.Seconds());
        watchFailureDetector.GetTimeSpan("expected-response-after").Should().Be(1.8.Seconds());
    }
    
    [Fact(DisplayName = "RemoteOptions with explicit certificate and ssl enabled should use provided certificate")]
    public void WithRemotingOptionsSslEnabledCertificateTest()
    {
        // arrange
        var certificate = new X509Certificate2("./Resources/akka-validcert.pfx", "password");
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "test");
        builder.WithRemoting(new RemoteOptions
        {
            EnableSsl = true,
            Ssl = new SslOptions
            {
                SuppressValidation = true, 
                X509Certificate = certificate
            }
        });
        
        // act
        var setup = (DotNettySslSetup) builder.Setups.First(s => s is DotNettySslSetup);

        // assert
        setup.SuppressValidation.Should().BeTrue();
        setup.Certificate.Should().Be(certificate);
    }
    
    [Fact(DisplayName = "RemoteOptions with explicit certificate and ssl disabled should ignore provided certificate")]
    public void WithRemotingOptionsSslDisabledCertificateTest()
    {
        // arrange
        var certificate = new X509Certificate2("./Resources/akka-validcert.pfx", "password");
        var builder = new AkkaConfigurationBuilder(new ServiceCollection(), "test");
        builder.WithRemoting(new RemoteOptions
        {
            EnableSsl = false,
            Ssl = new SslOptions
            {
                SuppressValidation = true, 
                X509Certificate = certificate
            }
        });
        
        // act
        var setup = builder.Setups.FirstOrDefault(s => s is DotNettySslSetup);

        // assert
        setup.Should().BeNull();
    }
    
    [Fact]
    public async Task AkkaRemoteShouldUsePublicHostnameCorrectly()
    {
        // arrange
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting("0.0.0.0", 0, "localhost");
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();

        // assert
        actorSystem.Provider.DefaultAddress.Host.Should().Be("localhost");
    }
    
    [Fact(DisplayName = "RemoteOptions should be bindable using Microsoft.Extensions.Configuration")]
    public async Task ClusterOptionsConfigurationTest()
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
    ""RemoteOptions"": {
      ""HostName"": ""0.0.0.0"",
      ""Port"" : 0,
      ""PublicHostName"": ""localhost"",
      ""PublicPort"": 12345
    }
  }
}";
        
        // arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
        var remoteOptions = jsonConfig.GetSection("Akka:RemoteOptions").Get<RemoteOptions>();

        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddAkka("RemoteSys", (builder, provider) =>
            {
                builder.WithRemoting(remoteOptions);
            });
        }).Build();

        // act
        await host.StartAsync();
        var actorSystem = (ExtendedActorSystem)host.Services.GetRequiredService<ActorSystem>();
        var config = actorSystem.Settings.Config;
        var adapters = config.GetStringList("akka.remote.enabled-transports");
        var tcpConfig = config.GetConfig("akka.remote.dot-netty.tcp");
        
        // assert
        adapters.Count.Should().Be(1);
        adapters[0].Should().Be("akka.remote.dot-netty.tcp");
        
        tcpConfig.GetString("hostname").Should().Be("0.0.0.0");
        tcpConfig.GetInt("port").Should().Be(0);
        tcpConfig.GetString("public-hostname").Should().Be("localhost");
        tcpConfig.GetInt("public-port").Should().Be(12345);
    }
}