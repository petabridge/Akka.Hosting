<a id="akkahosting"></a>
# Akka.Hosting

HOCON-less configuration, application lifecycle management, `ActorSystem` startup, and actor instantiation for [Akka.NET](https://getakka.net/).

See the ["Introduction to Akka.Hosting - HOCON-less, "Pit of Success" Akka.NET Runtime and Configuration" video](https://www.youtube.com/watch?v=Mnb9W9ClnB0) for a walkthrough of the library and how it can save you a tremendous amount of time and trouble.

# Table Of Content

- [Supported Packages](#supported-packages)
    * [Akka.NET Core Packages](#akkanet-core-packages)
    * [Akka Persistence Plugins](#akka-persistence-plugins)
    * [Akka.HealthCheck](#akkahealthcheck)
    * [Akka.Management Plugins](#akkamanagement-plugins)
        + [Akka.Management Core Package](#akkamanagement-core-package)
        + [Akka.Discovery Plugins](#akkadiscovery-plugins)
        + [Akka.Coordination Plugins](#akkacoordination-plugins)
- [Summary](#summary)
- [Dependency Injection Outside and Inside Akka.NET](#dependency-injection-outside-and-inside-akkanet)
    * [Registering Actors with the `ActorRegistry`](#registering-actors-with-the-actorregistry)
    * [Injecting Actors with `IRequiredActor<TKey>`](#injecting-actors-with-irequiredactortkey)
    * [Resolving `IRequiredActor<TKey>` within Akka.NET](#resolving-irequiredactortkey-within-akkanet)
- [Microsoft.Extensions.Configuration Integration](#microsoftextensionsconfiguration-integration)
    * [IConfiguration To HOCON Adapter](#iconfiguration-to-hocon-adapter)
- [Microsoft.Extensions.Logging Integration](#microsoftextensionslogging-integration)
    * [Logger Configuration Support](#logger-configuration-support)
    * [Microsoft.Extensions.Logging.ILoggerFactory Logging Support](#microsoftextensionsloggingiloggerfactory-logging-support)
    * [Serilog Support](#serilog-support)
    * [Microsoft.Extensions.Logging Log Event Filtering](#microsoftextensionslogging-log-event-filtering)

<a id="supported-packages"></a>
# Supported Packages

<a id="akkanet-core-packages"></a>
## Akka.NET Core Packages

* `Akka.Hosting` - the core `Akka.Hosting` package, needed for everything
* [`Akka.Remote.Hosting`](src/Akka.Remote.Hosting) - enables Akka.Remote configuration. Documentation can be read [here](src/Akka.Remote.Hosting/README.md)
* [`Akka.Cluster.Hosting`](src/Akka.Cluster.Hosting) - used for Akka.Cluster, Akka.Cluster.Sharding, and Akka.Cluster.Tools. Documentation can be read [here](src/Akka.Cluster.Hosting/README.md)
* [`Akka.Persistence.Hosting`](src/Akka.Persistence.Hosting/README.md) - used for adding persistence functionality to perform local database-less testing. Documentation can be read [here](src/Akka.Persistence.Hosting/README.md)

[Back to top](#akkahosting)

<a id="akka-persistence-plugins"></a>
## Akka Persistence Plugins

* [`Akka.Persistence.SqlServer.Hosting`](https://github.com/akkadotnet/Akka.Persistence.SqlServer/tree/dev/src/Akka.Persistence.SqlServer.Hosting) - used for Akka.Persistence.SqlServer support. Documentation can be read [here](https://github.com/akkadotnet/Akka.Persistence.SqlServer/blob/dev/src/Akka.Persistence.SqlServer.Hosting/README.md)
* [`Akka.Persistence.PostgreSql.Hosting`](https://github.com/akkadotnet/Akka.Persistence.PostgreSql/tree/dev/src/Akka.Persistence.PostgreSql.Hosting) - used for Akka.Persistence.PostgreSql support. Documentation can be read [here](https://github.com/akkadotnet/Akka.Persistence.PostgreSql/blob/dev/src/Akka.Persistence.PostgreSql.Hosting/README.md)
* [`Akka.Persistence.Azure.Hosting`](https://github.com/petabridge/Akka.Persistence.Azure) - used for Akka.Persistence.Azure support. Documentation can be read [here](https://github.com/petabridge/Akka.Persistence.Azure/blob/master/README.md)

[Back to top](#akkahosting)

<a id="akkahealthcheck"></a>
## [Akka.HealthCheck](https://github.com/petabridge/akkadotnet-healthcheck)

Embed health check functionality for environments such as Kubernetes, ASP.NET, AWS, Azure, Pivotal Cloud Foundry, and more. Documentation can be read [here](https://github.com/petabridge/akkadotnet-healthcheck/blob/dev/README.md)

[Back to top](#akkahosting)

<a id="akkamanagement-plugins"></a>
## [Akka.Management Plugins](https://github.com/akkadotnet/Akka.Management)

Useful tools for managing Akka.NET clusters running inside containerized or cloud based environment. `Akka.Hosting` is embedded in each of its packages.

[Back to top](#akkahosting)

<a id="akkamanagement-core-package"></a>
### Akka.Management Core Package

* [`Akka.Management`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/management/Akka.Management) - core module of the management utilities which provides a central HTTP endpoint for Akka management extensions. Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/tree/dev/src/management/Akka.Management#akka-management)
* `Akka.Management.Cluster.Bootstrap` - used to bootstrap a cluster formation inside dynamic deployment environments. Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/tree/dev/src/management/Akka.Management#akkamanagementclusterbootstrap)
  > **NOTE**
  >
  > As of version 1.0.0, cluster bootstrap came bundled inside the core `Akka.Management` NuGet package and are part of the default HTTP endpoint for `Akka.Management`. All `Akka.Management.Cluster.Bootstrap` NuGet package versions below 1.0.0 should now be considered deprecated.

[Back to top](#akkahosting)

<a id="akkadiscovery-plugins"></a>
### Akka.Discovery Plugins

* [`Akka.Discovery.AwsApi`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/aws/Akka.Discovery.AwsApi) - provides dynamic node discovery service for AWS EC2 environment. Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/blob/dev/src/discovery/aws/Akka.Discovery.AwsApi/README.md)
* [`Akka.Discovery.Azure`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/azure/Akka.Discovery.Azure) - provides a dynamic node discovery service for Azure PaaS ecosystem. Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/blob/dev/src/discovery/azure/Akka.Discovery.Azure/README.md)
* [`Akka.Discovery.KubernetesApi`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/discovery/kubernetes/Akka.Discovery.KubernetesApi) - provides a dynamic node discovery service for Kubernetes clusters. Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/blob/dev/src/discovery/kubernetes/Akka.Discovery.KubernetesApi/README.md)

[Back to top](#akkahosting)

<a id="akkacoordination-plugins"></a>
### Akka.Coordination Plugins

* [`Akka.Coordination.KubernetesApi`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/coordination/kubernetes/Akka.Coordination.KubernetesApi) - provides a lease-based distributed lock mechanism backed by [Kubernetes CRD](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/) for [Akka.NET Split Brain Resolver](https://getakka.net/articles/clustering/split-brain-resolver.html), [Akka.Cluster.Sharding](https://getakka.net/articles/clustering/cluster-sharding.html), and [Akka.Cluster.Singleton](https://getakka.net/articles/clustering/cluster-singleton.html). Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/blob/dev/src/coordination/kubernetes/Akka.Coordination.KubernetesApi/README.md)
* [`Akka.Coordination.Azure`](https://github.com/akkadotnet/Akka.Management/tree/dev/src/coordination/azure/Akka.Coordination.Azure) - provides a lease-based distributed lock mechanism backed by [Microsoft Azure Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-overview) for [Akka.NET Split Brain Resolver](https://getakka.net/articles/clustering/split-brain-resolver.html), [Akka.Cluster.Sharding](https://getakka.net/articles/clustering/cluster-sharding.html), and [Akka.Cluster.Singleton](https://getakka.net/articles/clustering/cluster-singleton.html). Documentation can be read [here](https://github.com/akkadotnet/Akka.Management/blob/dev/src/coordination/azure/Akka.Coordination.Azure/README.md)

[Back to top](#akkahosting)

<a id="summary"></a>
# Summary

We want to make Akka.NET something that can be instantiated more typically per the patterns often used with the Microsoft.Extensions.Hosting APIs that are common throughout .NET.

```csharp
using Akka.Hosting;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Remote.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions(){ Roles = new[]{ "myRole" },
            SeedNodes = new[]{ Address.Parse("akka.tcp://MyActorSystem@localhost:8110")}})
        .WithActors((system, registry) =>
    {
        var echo = system.ActorOf(act =>
        {
            act.ReceiveAny((o, context) =>
            {
                context.Sender.Tell($"{context.Self} rcv {o}");
            });
        }, "echo");
        registry.TryRegister<Echo>(echo); // register for DI
    });
});

var app = builder.Build();

app.MapGet("/", async (context) =>
{
    var echo = context.RequestServices.GetRequiredService<ActorRegistry>().Get<Echo>();
    var body = await echo.Ask<string>(context.TraceIdentifier, context.RequestAborted).ConfigureAwait(false);
    await context.Response.WriteAsync(body);
});

app.Run();
```

No HOCON. Automatically runs all Akka.NET application lifecycle best practices behind the scene. Automatically binds the `ActorSystem` and the `ActorRegistry`, another new 1.5 feature, to the `IServiceCollection` so they can be safely consumed via both actors and non-Akka.NET parts of users' .NET applications.

This should be open to extension in other child plugins, such as `Akka.Persistence.SqlServer`:

```csharp
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions()
        {
            Roles = new[] { "myRole" },
            SeedNodes = new[] { Address.Parse("akka.tcp://MyActorSystem@localhost:8110") }
        })
        .WithSqlServerPersistence(builder.Configuration.GetConnectionString("sqlServerLocal"))
        .WithShardRegion<UserActionsEntity>("userActions", s => UserActionsEntity.Props(s),
            new UserMessageExtractor(),
            new ShardOptions(){ StateStoreMode = StateStoreMode.DData, Role = "myRole"})
        .WithActors((system, registry) =>
        {
            var userActionsShard = registry.Get<UserActionsEntity>();
            var indexer = system.ActorOf(Props.Create(() => new Indexer(userActionsShard)), "index");
            registry.TryRegister<Index>(indexer); // register for DI
        });
})
```

[Back to top](#akkahosting)

<a id="dependency-injection-outside-and-inside-akkanet"></a>
# Dependency Injection Outside and Inside Akka.NET

One of the other design goals of Akka.Hosting is to make the dependency injection experience with Akka.NET as seamless as any other .NET technology. We accomplish this through two new APIs:

* The `ActorRegistry`, a DI container that is designed to be populated with `Type`s for keys and `IActorRef`s for values, just like the `IServiceCollection` does for ASP.NET services.
* The `IRequiredActor<TKey>` - you can place this type the constructor of any dependency injected resource and it will automatically resolve a reference to the actor stored inside the `ActorRegistry` with `TKey`. This is how we inject actors into ASP.NET, SignalR, gRPC, and other Akka.NET actors!

> **N.B.** The `ActorRegistry` and the `ActorSystem` are automatically registered with the `IServiceCollection` / `IServiceProvider` associated with your application.

[Back to top](#akkahosting)

<a id="registering-actors-with-the-actorregistry"></a>
## Registering Actors with the `ActorRegistry`

As part of Akka.Hosting, we need to provide a means of making it easy to pass around top-level `IActorRef`s via dependency injection both within the `ActorSystem` and outside of it.

The `ActorRegistry` will fulfill this role through a set of generic, typed methods that make storage and retrieval of long-lived `IActorRef`s easy and coherent:

* Fetch ActorRegistry from ActorSystem manually
```csharp
var registry = ActorRegistry.For(myActorSystem); 
```

* Provided by the actor builder
```csharp
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithActors((system, actorRegistry) =>
        {
            var actor = system.ActorOf(Props.Create(() => new MyActor));
            actorRegistry.TryRegister<MyActor>(actor); // register actor for DI
        });
});
```

* Obtaining the `IActorRef` manually
```csharp
var registry = ActorRegistry.For(myActorSystem); 
registry.Get<Index>(); // use in DI
```

[Back to top](#akkahosting)

<a id="injecting-actors-with-irequiredactortkey"></a>
## Injecting Actors with `IRequiredActor<TKey>`

Suppose we have a class that depends on having a reference to a top-level actor, a router, a `ShardRegion`, or perhaps a `ClusterSingleton` (common types of actors that often interface with non-Akka.NET parts of a .NET application):

```csharp
public sealed class MyConsumer
{
    private readonly IActorRef _actor;

    public MyConsumer(IRequiredActor<MyActorType> actor)
    {
        _actor = actor.ActorRef;
    }

    public async Task<string> Say(string word)
    {
        return await _actor.Ask<string>(word, TimeSpan.FromSeconds(3));
    }
}
```

The `IRequiredActor<MyActorType>` will cause the Microsoft.Extensions.DependencyInjection mechanism to resolve `MyActorType` from the `ActorRegistry` and inject it into the `IRequired<Actor<MyActorType>` instance passed into `MyConsumer`.

The `IRequiredActor<TActor>` exposes a single property:

```csharp
public interface IRequiredActor<TActor>
{
    /// <summary>
    /// The underlying actor resolved via <see cref="ActorRegistry"/> using the given <see cref="TActor"/> key.
    /// </summary>
    IActorRef ActorRef { get; }
}
```

By default, you can automatically resolve any actors registered with the `ActorRegistry` without having to declare anything special on your `IServiceCollection`:

```csharp
using var host = new HostBuilder()
  .ConfigureServices(services =>
  {
      services.AddAkka("MySys", (builder, provider) =>
      {
          builder.WithActors((system, registry) =>
          {
              var actor = system.ActorOf(Props.Create(() => new MyActorType()), "myactor");
              registry.Register<MyActorType>(actor);
          });
      });
      services.AddScoped<MyConsumer>();
  })
  .Build();
  await host.StartAsync();
```

Adding your actor and your type key into the `ActorRegistry` is sufficient - no additional DI registration is required to access the `IRequiredActor<TActor>` for that type.

[Back to top](#akkahosting)

<a id="resolving-irequiredactortkey-within-akkanet"></a>
## Resolving `IRequiredActor<TKey>` within Akka.NET

Akka.NET does not use dependency injection to start actors by default primarily because actor lifetime is unbounded by default - this means reasoning about the scope of injected dependencies isn't trivial. ASP.NET, by contrast, is trivial: all HTTP requests are request-scoped and all web socket connections are connection-scoped - these are objects have _bounded_ and typically short lifetimes.

Therefore, users have to explicitly signal when they want to use Microsoft.Extensions.DependencyInjection via [the `IDependencyResolver` interface in Akka.DependencyInjection](https://getakka.net/articles/actors/dependency-injection.html) - which is easy to do in most of the Akka.Hosting APIs for starting actors:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IReplyGenerator, DefaultReplyGenerator>();
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting(hostname: "localhost", port: 8110)
        .WithClustering(new ClusterOptions{SeedNodes = new []{ "akka.tcp://MyActorSystem@localhost:8110", }})
        .WithShardRegion<Echo>(
            typeName: "myRegion",
            entityPropsFactory: (_, _, resolver) =>
            {
                // uses DI to inject `IReplyGenerator` into EchoActor
                return s => resolver.Props<EchoActor>(s);
            },
            extractEntityId: ExtractEntityId,
            extractShardId: ExtractShardId,
            shardOptions: new ShardOptions());
});
```

The `dependencyResolver.Props<MySingletonDiActor>()` call will leverage the `ActorSystem`'s built-in `IDependencyResolver` to instantiate the `MySingletonDiActor` and inject it with all of the necessary dependencies, including `IRequiredActor<TKey>`.

[Back to top](#akkahosting)

<a id="microsoftextensionsconfiguration-integration"></a>
# Microsoft.Extensions.Configuration Integration

<a id="iconfiguration-to-hocon-adapter"></a>
## IConfiguration To HOCON Adapter

The `AddHocon` extension method can convert `Microsoft.Extensions.Configuration` `IConfiguration` into HOCON `Config` instance and adds it to the ActorSystem being configured.
* Unlike `IConfiguration`, all HOCON key names are **case sensitive**.
* **Unless enclosed inside double quotes**, all "." (period) in the `IConfiguration` key will be treated as a HOCON object key separator
* `IConfiguration` **does not support object composition**, if you declare the same key multiple times inside multiple configuration providers (JSON/environment variables/etc), **only the last one declared will take effect**.
* For environment variable configuration provider:
   * "__" (double underline) will be converted to "." (period).
   * "_" (single underline) will be converted to "-" (dash).
   * If all keys are composed of integer parseable keys, the whole object is treated as an array

__Example:__

`appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "akka": {
    "cluster": {
      "roles": [ "front-end", "back-end" ],
      "min-nr-of-members": 3,
      "log-info": true
    }
  }    
}
```

Environment variables:

```powershell
AKKA__ACTOR__TELEMETRY__ENABLE=true
AKKA__CLUSTER__SEED_NODES__0=akka.tcp//mySystem@localhost:4055
AKKA__CLUSTER__SEED_NODES__1=akka.tcp//mySystem@localhost:4056
AKKA__CLUSTER__SEED_NODE_TIMEOUT=00:00:05
```

Note the integer parseable key inside the seed-nodes configuration, seed-nodes will be parsed as an array. These environment variables will be parsed as HOCON settings:

```hocon
akka {
  actor {
    telemetry.enabled: on
  }
  cluster {
    seed-nodes: [ 
      "akka.tcp//mySystem@localhost:4055",
      "akka.tcp//mySystem@localhost:4056" 
    ]
    seed-node-timeout: 5s
  }
}
```

Example code:

```csharp
/*
Both appsettings.json and environment variables are combined
into HOCON configuration:

akka {
  actor.telemetry.enabled: on
  cluster {
    roles: [ "front-end", "back-end" ]
    seed-nodes: [ 
      "akka.tcp//mySystem@localhost:4055",
      "akka.tcp//mySystem@localhost:4056" 
    ]
    min-nr-of-members: 3
    seed-node-timeout: 5s
    log-info: true
  }
}
*/
var host = new HostBuilder()
    .ConfigureHostConfiguration(builder =>
    {
        // Setup IConfiguration to load from appsettings.json and
        // environment variables
        builder
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("mySystem", (builder, provider) =>
            {
                // convert IConfiguration to HOCON
                var akkaConfig = context.Configuration.GetSection("akka");
                builder.AddHocon(akkaConfig, HoconAddMode.Prepend); 
            });
    });
```

### Special Characters And Case Sensitivity

This advanced usage of the `IConfiguration` adapter is solely used for edge cases where HOCON key capitalization needs to be preserved, such as declaring serialization binding. Note that when you're using this feature, none of the keys are normalized, you will have to write all of your keys in a HOCON compatible way.

`appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "akka": {
    "\"Key.With.Dots\"": "Key Value",
    "cluster": {
      "roles": [ "front-end", "back-end" ],
      "min-nr-of-members": 3,
      "log-info": true
    }
  }    
}
```

Note that "Key.With.Dots" needs to be inside escaped double quotes, this is a HOCON requirement that preserves the "." (period) inside HOCON property names.

Environment variables:

```powershell
PS C:/> [Environment]::SetEnvironmentVariable('akka__actor__telemetry__enabled', 'true')
PS C:/> [Environment]::SetEnvironmentVariable('akka__actor__serialization_bindings__"System.Object"', 'hyperion')
PS C:/> [Environment]::SetEnvironmentVariable('akka__cluster__seed_nodes__0', 'akka.tcp//mySystem@localhost:4055')
PS C:/> [Environment]::SetEnvironmentVariable('akka__cluster__seed_nodes__1', 'akka.tcp//mySystem@localhost:4056')
PS C:/> [Environment]::SetEnvironmentVariable('akka__cluster__seed_node_timeout', '00:00:05')
```

Note that:
1. All of the environment variable names are in lower case, except "System.Object" where it needs to preserve name capitalization.
2. To set serialization binding via environment variable, you have to use "." (period) instead of "__" (double underscore), this might be problematic for some shell scripts and there is no way of getting around this.

Example code:

```csharp
/*
Both appsettings.json and environment variables are combined
into HOCON configuration:

akka {
  "Key.With.Dots": Key Value
  actor {
    telemetry.enabled: on
    serialization-bindings {
      "System.Object" = hyperion
    }
  }
  cluster {
    roles: [ "front-end", "back-end" ]
    seed-nodes: [ 
      "akka.tcp//mySystem@localhost:4055",
      "akka.tcp//mySystem@localhost:4056" 
    ]
    min-nr-of-members: 3
    seed-node-timeout: 5s
    log-info: true
  }
}
*/
var host = new HostBuilder()
    .ConfigureHostConfiguration(builder =>
    {
        // Setup IConfiguration to load from appsettings.json and
        // environment variables
        builder
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("mySystem", (builder, provider) =>
            {
                // convert IConfiguration to HOCON
                var akkaConfig = context.Configuration.GetSection("akka");
                // Note the last method argument is set to false
                builder.AddHocon(akkaConfig, HoconAddMode.Prepend, false); 
            });
    });
```

[Back to top](#akkahosting)

<a id="microsoftextensionslogging-integration"></a>
# Microsoft.Extensions.Logging Integration

<a id="logger-configuration-support"></a>
## Logger Configuration Support

You can use `AkkaConfigurationBuilder` extension method called `ConfigureLoggers(Action<LoggerConfigBuilder>)` to configure how Akka.NET logger behave.

Example:
```csharp
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .ConfigureLoggers(setup =>
        {
            // Example: This sets the minimum log level
            setup.LogLevel = LogLevel.DebugLevel;
            
            // Example: Clear all loggers
            setup.ClearLoggers();
            
            // Example: Add the default logger
            // NOTE: You can also use setup.AddLogger<DefaultLogger>();
            setup.AddDefaultLogger();
            
            // Example: Add the ILoggerFactory logger
            // NOTE:
            //   - You can also use setup.AddLogger<LoggerFactoryLogger>();
            //   - To use a specific ILoggerFactory instance, you can use setup.AddLoggerFactory(myILoggerFactory);
            setup.AddLoggerFactory();
            
            // Example: Adding a serilog logger
            setup.AddLogger<SerilogLogger>();
        })
        .WithActors((system, registry) =>
        {
            var echo = system.ActorOf(act =>
            {
                act.ReceiveAny((o, context) =>
                {
                    Logging.GetLogger(context.System, "echo").Info($"Actor received {o}");
                    context.Sender.Tell($"{context.Self} rcv {o}");
                });
            }, "echo");
            registry.TryRegister<Echo>(echo); // register for DI
        });
});
```

A complete code sample can be viewed [here](https://github.com/akkadotnet/Akka.Hosting/tree/dev/src/Examples/Akka.Hosting.LoggingDemo).

Exposed properties are:
- `LogLevel`: Configure the Akka.NET minimum log level filter, defaults to `InfoLevel`
- `LogConfigOnStart`: When set to true, Akka.NET will log the complete HOCON settings it is using at start up, this can then be used for debugging purposes.

Currently supported logger methods:
- `ClearLoggers()`: Clear all registered logger types.
- `AddLogger<TLogger>()`: Add a logger type by providing its class type.
- `AddDefaultLogger()`: Add the default Akka.NET console logger.
- `AddLoggerFactory()`: Add the new `ILoggerFactory` logger.

[Back to top](#akkahosting)

<a id="microsoftextensionsloggingiloggerfactory-logging-support"></a>
## Microsoft.Extensions.Logging.ILoggerFactory Logging Support

You can now use `ILoggerFactory` from Microsoft.Extensions.Logging as one of the sinks for Akka.NET logger. This logger will use the `ILoggerFactory` service set up inside the dependency injection `ServiceProvider` as its sink.

[Back to top](#akkahosting)

<a id="serilog-support"></a>
## Serilog Message Formatting Support

If you're interested in using [Akka.Logger.Serilog](https://github.com/akkadotnet/Akka.Logger.Serilog), you can set Akka.NET's default logger and log message formatter to allow for Serilog's semantic logging to be enabled by default:

```csharp
builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder
        .ConfigureLoggers(setup =>
        {
            // Example: This sets the minimum log level
            setup.LogLevel = LogLevel.DebugLevel;
            
            // Example: Clear all loggers
            setup.ClearLoggers();
            
            // Add Serilog
            setup.AddLogger<SerilogLogger>();
            
            // use the default SerilogFormatter everywhere
            setup.WithDefaultLogMessageFormatter<SerilogLogMessageFormatter>();
        })
        .WithActors((system, registry) =>
        {
            var echo = system.ActorOf(act =>
            {
                act.ReceiveAny((o, context) =>
                {
                    Logging.GetLogger(context.System, "echo").Info($"Actor received {o}");
                    context.Sender.Tell($"{context.Self} rcv {o}");
                });
            }, "echo");
            registry.TryRegister<Echo>(echo); // register for DI
        });
});
```

This will eliminate the need to have to do `Context.GetLogger<SerilogLoggingAdapter>()` everywhere you want to use it.

[Back to top](#akkahosting)

<a id="microsoftextensionslogging-log-event-filtering"></a>
## Microsoft.Extensions.Logging Log Event Filtering

There will be two log event filters acting on the final log input, the Akka.NET `akka.loglevel` setting and the `Microsoft.Extensions.Logging` settings, make sure that both are set correctly or some log messages will be missing.

To set up the `Microsoft.Extensions.Logging` log filtering, you will need to edit the `appsettings.json` file. Note that we also set the `Akka` namespace to be filtered at debug level in the example below.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Akka": "Debug"
    }
  }
}
```

[Back to top](#akkahosting)

<a id="filtering-logs"></a>
## Filtering Logs In Akka.NET

In Akka.NET 1.5.21, we introduced [log filtering for log messages based on the LogSource or the content of a log message](https://getakka.net/articles/utilities/logging.html#filtering-log-messages). Depending on your coding style, you can use this feature in Akka.Hosting in several ways.

1. Using The `LoggerConfigBuilder.WithLogFilter()` method.

   The `LoggerConfigBuilder.WithLogFilter()` method lets you set up the `LogFilterBuilder`

   ```csharp
   builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
   {
       configurationBuilder
           .ConfigureLoggers(loggerConfigBuilder =>
           {
               loggerConfigBuilder.WithLogFilter(filterBuilder =>
               {
                   filterBuilder.ExcludeMessageContaining("Test");
               });
           });
   });
   ```

2. Setting the `loggerConfigBuilder.LogFilterBuilder` property directly.

   ```csharp
   builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
   {
       configurationBuilder
           .ConfigureLoggers(loggerConfigBuilder =>
           {
               loggerConfigBuilder.LogFilterBuilder = new LogFilterBuilder();
               loggerConfigBuilder.LogFilterBuilder.ExcludeMessageContaining("Test");
           });
   });
   ```

[Back to top](#akkahosting)
