// <copyright file="RegContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RegContactConfiguration : IEntityTypeConfiguration<RegContact>
{
    public void Configure(EntityTypeBuilder<RegContact> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("Contact", "reg");
        builder.Property(e => e.FirstName).HasMaxLength(255);
        builder.Property(e => e.LastName).HasMaxLength(255);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
        builder.Property(e => e.LandPhone).HasMaxLength(255);
        builder.Property(e => e.MobilePhone).HasMaxLength(255);
        builder.Property(e => e.JobDescription).HasMaxLength(255);
        builder.Property(e => e.Source).HasMaxLength(20);

        builder.HasMany(e => e.Roles)
               .WithOne(r => r.Contact)
               .HasForeignKey(r => r.ContactEmail);
    }
        
}
