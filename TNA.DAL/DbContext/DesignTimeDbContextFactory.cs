using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TNA.DAL.DbContext
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TNADbContext>
    {
        public TNADbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TNADbContext>();

            var conn = "DefaultConnection\": \"Data Source=TNAAPPDB_TEST.mssql.somee.com;Initial Catalog=TNAAPPDB_TEST;User ID=Crisnialu_SQLLogin_2;Password=s7yh7jpsn5;TrustServerCertificate=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(conn);
            return new TNADbContext(optionsBuilder.Options);
        }
    }
}
