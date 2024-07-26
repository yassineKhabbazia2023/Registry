// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class CreAccountConfiguration : IEntityTypeConfiguration<CreAccount>
    {
        public void Configure(EntityTypeBuilder<CreAccount> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Account", "cre");
            builder.Property(e => e.LegalName).IsRequired().HasMaxLength(255);
            builder.Property(e => e.AccountNumber).HasMaxLength(50);
        }
            
    }
}
