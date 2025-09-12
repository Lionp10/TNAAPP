using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TNA.DAL.DbContext
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TNADbContext>
    {
        public TNADbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TNADbContext>();

            // Local
            //var conn = "Data Source=LIONP_\\SQLExpress;Initial Catalog=TNAAPP;Integrated Security=True;TrustServerCertificate=True";

            // Testing
            var conn = "Data Source=TNAAPPDB_TEST.mssql.somee.com;Initial Catalog=TNAAPPDB_TEST;User ID=Crisnialu_SQLLogin_2;Password=s7yh7jpsn5;TrustServerCertificate=True;MultipleActiveResultSets=true";

            // Production
            //var conn = "Data Source=TNAAPPDB.mssql.somee.com;Initial Catalog=TNAAPPDB;User ID=Lionp__SQLLogin_1;Password=8s86qq9i2o;TrustServerCertificate=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(conn);
            return new TNADbContext(optionsBuilder.Options);
        }
    }
}
