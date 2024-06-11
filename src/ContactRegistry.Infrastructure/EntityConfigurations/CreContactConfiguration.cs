// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class CreContactConfiguration : IEntityTypeConfiguration<CreContact>
    {
        public void Configure(EntityTypeBuilder<CreContact> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Contact", "cre");
            builder.Property(e => e.FirstName).HasMaxLength(255);
            builder.Property(e => e.LastName).HasMaxLength(255);
            builder.Property(e => e.Email).HasMaxLength(255);
            builder.Property(e => e.LandPhone).HasMaxLength(255);
            builder.Property(e => e.MobilePhone).HasMaxLength(255);
            builder.Property(e => e.JobDescription).HasMaxLength(255);
            builder.Property(e => e.Source).HasMaxLength(20);

            builder.HasMany(e => e.Roles)
                   .WithOne(r => r.Contact)
                   .HasForeignKey(r => r.ContactId);
        }
            
    }
}
