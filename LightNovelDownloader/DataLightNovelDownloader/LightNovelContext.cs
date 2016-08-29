using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLightNovelDownloader
{
    public class LightNovelContext : DbContext
    {
        public virtual DbSet<Book> Books { get; set; }
        public virtual DbSet<Chapter> Chapters { get; set; }
        public LightNovelContext() : base("Name=LightNovelDb")
        {
            Database.SetInitializer(new LightNovelContextInitializer());
        }

        private class LightNovelContextInitializer : DropCreateDatabaseAlways<LightNovelContext>
        {
            protected override void Seed(LightNovelContext context)
            {
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
        }

    }

    public class LightNovelRepository<T> : EFRepository<T, LightNovelContext> where T : class { }
}

