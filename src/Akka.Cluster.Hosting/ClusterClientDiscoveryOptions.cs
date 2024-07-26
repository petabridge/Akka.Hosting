﻿using System;
using System.Text;
using Akka.Cluster.Tools.Client;
using Akka.Hosting;

namespace Akka.Cluster.Hosting;

public sealed class ClusterClientDiscoveryOptions
{
    /// <summary>
    ///     <para>
    ///         The discovery sub-system that will be used to discover cluster client contacts. This has to be
    ///         an instance of <c>KubernetesDiscoveryOptions</c>, <c>AkkaDiscoveryOptions</c>,
    ///         <c>Ec2ServiceDiscoveryOptions</c>, <c>EcsServiceDiscoveryOptions</c>,
    ///         or <c>ConfigServiceDiscoveryOptions</c>.
    ///     </para>
    ///     Note that if you're also using Akka.Discovery for ClusterBootstrap, in order for
    ///     <see cref="ClusterClientDiscovery"/> to work, you <b>will need</b> to set
    ///     <c>DiscoveryOptions.IsDefaultPlugin</c> to <c>false</c>
    /// </summary>
    public IHoconOption DiscoveryOptions { get; set; } = null!;
    
    /// <summary>
    /// The service name that are being discovered. This setting is not optional. 
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// The Akka.Management port name, usually used in conjunction with Akka.Discovery.KubernetesApi
    /// </summary>
    public string? PortName { get; set; }
    
    /// <summary>
    /// Interval at which service discovery will be polled in search for new initial contacts
    /// </summary>
    public TimeSpan? RetryInterval { get; set; }
    
    /// <summary>
    /// Timeout for getting a reply from the service-discovery subsystem
    /// </summary>
    public TimeSpan? Timeout { get; set; }
    
    /// <summary>
    /// The number of initial contacts will be trimmed down to this number of contact points to the client
    /// </summary>
    public int? NumberOfContacts { get; set; }
    
    /// <summary>
    /// The name of the cluster client actor
    /// </summary>
    public string? ClientActorName { get; set; }
    
    internal void Apply(AkkaConfigurationBuilder builder)
    {
        Validate();
        
        var sb = new StringBuilder();
        sb.AppendLine("akka.cluster.client {");
        sb.AppendLine("use-initial-contacts-discovery = true");
        sb.AppendLine("discovery {");
        sb.AppendLine($"method = {DiscoveryOptions.ConfigPath.ToHocon()}");
        sb.AppendLine($"service-name = {ServiceName.ToHocon()}");
        if (string.IsNullOrWhiteSpace(PortName))
            sb.AppendLine($"port-name = {PortName.ToHocon()}");
        if (NumberOfContacts is not null)
            sb.AppendLine($"number-of-contacts = {NumberOfContacts.ToHocon()}");
        if (RetryInterval is not null)
            sb.AppendLine($"interval = {RetryInterval.ToHocon()}");
        if (Timeout is not null)
            sb.AppendLine($"resolve-timeout = {Timeout.ToHocon()}");
        
        sb.AppendLine("}}");
            
        DiscoveryOptions.Apply(builder);
    }

    private void Validate()
    {
        var type = DiscoveryOptions.GetType().Name;
        if(!(type.Contains("ConfigServiceDiscoveryOptions") 
             || type.Contains("KubernetesDiscoveryOptions")
             || type.Contains("AkkaDiscoveryOptions")
             || type.Contains("Ec2ServiceDiscoveryOptions")
             || type.Contains("EcsServiceDiscoveryOptions")))
            throw new ArgumentException(
                "Discovery options must be of Type KubernetesDiscoveryOptions, AkkaDiscoveryOptions, " +
                "Ec2ServiceDiscoveryOptions, EcsServiceDiscoveryOptions, or ConfigServiceDiscoveryOptions",
                nameof(DiscoveryOptions));

        if (string.IsNullOrWhiteSpace(ServiceName))
            throw new ArgumentException("Service name must be provided", nameof(ServiceName));

        if (RetryInterval is not null && RetryInterval < TimeSpan.Zero)
            throw new ArgumentException("Retry interval must be greater than zero", nameof(RetryInterval));

        if(Timeout is not null && Timeout < TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero", nameof(Timeout));

        if (NumberOfContacts < 1)
            throw new ArgumentException("Number of contacts must be greater than 0", nameof(NumberOfContacts));

        if (ClientActorName is not null && string.IsNullOrWhiteSpace(ClientActorName))
            throw new ArgumentException("Cluster client actor name must not be empty or whitespace", nameof(ClientActorName));
    }
}