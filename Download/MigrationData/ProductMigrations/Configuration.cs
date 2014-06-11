namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Download.Models;
    using System.Collections.Generic;

    internal sealed class Configuration : DbMigrationsConfiguration<Download.Models.ProductDBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            MigrationsDirectory = @"MigrationData\ProductMigrations";
        }

        protected override void Seed(Download.Models.ProductDBContext context)
        {
            var product = new Product { ProductName = "TextPad", ProductStatus = 1};

            var version1 = new Models.Version { VersionName = "1.0.0", Product = product, VersionStatus = 2 };
            var version2 = new Models.Version { VersionName = "2.0.0", Product = product, VersionStatus = 2 };
            var archive1 = new Archive { Exe = "Exe Path1", Installer = "InstallerPath1", ReadMe = "Add ReadMe1", Version = version1 };
            var archive2 = new Archive { Exe = "Exe Path2", Installer = "InstallerPath2", ReadMe = "Add ReadMe2", Version = version1 };



            context.Products.Add(product);
            context.Versions.Add(version1);
            context.Versions.Add(version2);
            context.Archives.Add(archive1);
            context.Archives.Add(archive2);

            context.SaveChanges();
        }
    }
}
