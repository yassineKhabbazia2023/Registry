// <copyright file="ApplicationDbContext.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;
using Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

/// <summary>
/// ApplicationDbContext.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// ApplicationDbContext.
    /// </summary>
    /// <param name="options">options.</param>
    public ApplicationDbContext(DbContextOptions options)
    : base(options)
    {
    }

    /// <summary>
    /// Gets or sets Cre contacts.
    /// </summary>
    public DbSet<CreContact> CreContacts { get; set; }

    /// <summary>
    /// Gets or sets Cre accounts.
    /// </summary>
    public DbSet<CreAccount> CreAccounts { get; set; }

    /// <summary>
    /// Gets or sets Cre roles .
    /// </summary>
    public DbSet<CreRole> CreRoles { get; set; }

    /// <summary>
    /// Gets or sets Alx contacts.
    /// </summary>
    public DbSet<AlxContact> AlxContacts { get; set; }

    /// <summary>
    /// Gets or sets Alx accounts.
    /// </summary>
    public DbSet<AlxAccount> AlxAccounts { get; set; }

    /// <summary>
    /// Gets or sets Alx roles .
    /// </summary>
    public DbSet<AlxRole> AlxRoles { get; set; }

    /// <summary>
    /// Gets or sets Cre ProcessDeltaTrigger .
    /// </summary>
    public DbSet<ProcessDeltaTrigger> ProcessDeltaTriggers { get; set; }

    /// <summary>
    /// Gets or sets Cre operations .
    /// </summary>
    public DbSet<CreOperation> CreOperations { get; set; }

    /// <summary>
    /// OnModelCreating.
    /// </summary>
    /// <param name="modelBuilder">modelBuilder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AlxAccountConfiguration());
        modelBuilder.ApplyConfiguration(new AlxContactConfiguration());
        modelBuilder.ApplyConfiguration(new AlxRoleConfiguration());
        modelBuilder.ApplyConfiguration(new CreAccountConfiguration());
        modelBuilder.ApplyConfiguration(new CreContactConfiguration());
        modelBuilder.ApplyConfiguration(new CreRoleConfiguration());
        modelBuilder.ApplyConfiguration(new CreOperationConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessDeltaTriggerConfiguration());
    }
}
