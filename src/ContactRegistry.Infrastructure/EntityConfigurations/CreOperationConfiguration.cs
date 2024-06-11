// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.EntityConfigurations
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class CreOperationConfiguration : IEntityTypeConfiguration<CreOperation>
    {
        public void Configure(EntityTypeBuilder<CreOperation> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("Operations", "cre");
            builder.Property(e => e.Type).IsRequired().HasMaxLength(10);
            builder.Property(e => e.Operation).IsRequired().HasMaxLength(10);
            builder.Property(e => e.PublishedAt).IsRequired(false);
            builder.Property(e => e.EntityId).IsRequired();
        }

    }
}
