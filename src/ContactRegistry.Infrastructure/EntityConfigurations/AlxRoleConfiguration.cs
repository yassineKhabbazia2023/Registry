// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class AlxRoleConfiguration : IEntityTypeConfiguration<AlxRole>
    {
        public void Configure(EntityTypeBuilder<AlxRole> builder)
        {
            builder.HasKey(e => e.RoleId);
            builder.ToTable("Role", "alx");

            builder.Property(e => e.ContactId).IsRequired();
            builder.Property(e => e.AccountId).IsRequired();

            builder.Property(e => e.Onboarded);

            builder.HasOne(e => e.Contact)
                   .WithMany(c => c.Roles)
                   .HasForeignKey(e => e.ContactId);

            builder.HasOne(e => e.Account)
                   .WithMany()
                   .HasForeignKey(e => e.AccountId);
        }
    }            
}
