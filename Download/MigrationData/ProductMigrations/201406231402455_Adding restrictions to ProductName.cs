namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingrestrictionstoProductName : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Products", "ProductName", c => c.String(nullable: false, maxLength: 200));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Products", "ProductName", c => c.String(nullable: false));
        }
    }
}
