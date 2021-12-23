using System;
using System.Collections.Generic;
using System.Text;
using Database.Entity;
using Microsoft.EntityFrameworkCore;


namespace Database
{
    public class ContextDb : DbContext
    {
        private static string confStr = "(localdb)\\MSSQLLocalDB;Initial Catalog=Images.db;Integrated Security=True";

        public DbSet<ImageEntity> Images { get; set; }
        public DbSet<LabelEntity> Labels { get; set; }
        public DbSet<BBoxEntity> BBoxes { get; set; }

        public ContextDb()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options
            .UseLazyLoadingProxies()
            .UseSqlServer($"Data Source={confStr}");


    }
}
