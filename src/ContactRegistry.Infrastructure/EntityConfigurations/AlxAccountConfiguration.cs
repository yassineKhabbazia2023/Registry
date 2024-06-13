// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class AlxAccountConfiguration : IEntityTypeConfiguration<AlxAccount>
    {
        public void Configure(EntityTypeBuilder<AlxAccount> builder)
        {
            builder.HasKey(e => e.AccountGlobalUniqueIdentifier);
            builder.ToTable("Account", "alx");
            builder.Property(e => e.LegalName).IsRequired().HasMaxLength(255);
            builder.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
        }
            
    }
}
