using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.EntityConfigurations
{
    public class ProcessDeltaTriggerConfiguration: IEntityTypeConfiguration<ProcessDeltaTrigger>
    {
        public void Configure(EntityTypeBuilder<ProcessDeltaTrigger> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("ProcessDeltaTrigger", "cre");
        }
    }
}
