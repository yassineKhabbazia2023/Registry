// <copyright file="TestJobActivator.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Hangfire;

public class TestJobActivator : JobActivator
{
    private readonly IServiceProvider _serviceProvider;

    public TestJobActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override object ActivateJob(Type jobType)
    {
        return _serviceProvider.GetService(jobType);
    }
}