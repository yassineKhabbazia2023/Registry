// <copyright file="RegProcessDeltaTriggerConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class RegProcessDeltaTriggerConfiguration : IEntityTypeConfiguration<RegProcessDeltaTrigger>
{
    public void Configure(EntityTypeBuilder<RegProcessDeltaTrigger> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("ProcessDeltaTrigger", "reg");
    }
}
