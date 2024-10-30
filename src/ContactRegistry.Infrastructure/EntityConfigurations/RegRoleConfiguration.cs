// <copyright file="RegRoleConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RegRoleConfiguration : IEntityTypeConfiguration<RegRole>
{
    public void Configure(EntityTypeBuilder<RegRole> builder)
    {
        builder.HasKey(e => e.RoleId);
        builder.ToTable("Role", "reg");

        builder.Property(e => e.ContactEmail).IsRequired();
        builder.Property(e => e.AccountNumber).IsRequired();
        builder.Property(e => e.AccountId).IsRequired();
        builder.Property(e => e.ContactId).IsRequired();
        builder.Property(e => e.Deleted).IsRequired(false);

        builder.HasOne(e => e.Contact)
               .WithMany(c => c.Roles)
               .HasForeignKey(e => e.ContactEmail);

        builder.HasOne(e => e.Account)
               .WithMany()
               .HasForeignKey(e => e.AccountNumber);
    }
}
