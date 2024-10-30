// <copyright file="RegAccountConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RegAccountConfiguration : IEntityTypeConfiguration<RegAccount>
{
    public void Configure(EntityTypeBuilder<RegAccount> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("Account", "reg");
        builder.Property(e => e.LegalName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
    }
        
}
