namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingExFilesStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ExtraFiles", "ExFileStatus", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ExtraFiles", "ExFileStatus");
        }
    }
}
