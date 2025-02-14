// <copyright file="TestJobFilter.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Hangfire.Client;
using Hangfire.Common;

public class TestJobFilter : JobFilterAttribute, IClientFilter
{
    public static List<string> CreatedJobs { get; } = new List<string>();

    public void OnCreating(CreatingContext filterContext)
    {
        CreatedJobs.Add(filterContext.Job.Method.Name);
    }

    public void OnCreated(CreatedContext filterContext)
    {    
    }
}