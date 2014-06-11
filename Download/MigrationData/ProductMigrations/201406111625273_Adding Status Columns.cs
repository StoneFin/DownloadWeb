namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingStatusColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Versions", "VersionStatus", c => c.Int(nullable: false));
            AddColumn("dbo.Products", "ProductStatus", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Products", "ProductStatus");
            DropColumn("dbo.Versions", "VersionStatus");
        }
    }
}
