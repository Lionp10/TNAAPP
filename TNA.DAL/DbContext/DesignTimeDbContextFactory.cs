using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TNA.DAL.DbContext
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TNADbContext>
    {
        public TNADbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TNADbContext>();
            var conn = "Data Source=LIONP_\\SQLExpress;Initial Catalog=TNAAPP;Integrated Security=True;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(conn);
            return new TNADbContext(optionsBuilder.Options);
        }
    }
}
