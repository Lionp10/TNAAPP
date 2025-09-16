using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TNA.DAL.DbContext
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TNADbContext>
    {
        public TNADbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TNADbContext>();

            var conn = "";

            optionsBuilder.UseSqlServer(conn);
            return new TNADbContext(optionsBuilder.Options);
        }
    }
}
