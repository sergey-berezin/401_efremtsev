using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication.Entity;

namespace WebApplication
{
    public class ContextDb : DbContext
    {
        public DbSet<ImageEntity> Images { get; set; }
        public DbSet<LabelEntity> Labels { get; set; }
        public DbSet<BBoxEntity> BBoxes { get; set; }

        public ContextDb(DbContextOptions<ContextDb> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
