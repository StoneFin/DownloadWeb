namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingFileSize : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Archives", "ExeSize", c => c.String());
            AddColumn("dbo.Archives", "InstallerSize", c => c.String());
            AddColumn("dbo.Archives", "ReadMeSize", c => c.String());
            AddColumn("dbo.ExtraFiles", "FileSize", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ExtraFiles", "FileSize");
            DropColumn("dbo.Archives", "ReadMeSize");
            DropColumn("dbo.Archives", "InstallerSize");
            DropColumn("dbo.Archives", "ExeSize");
        }
    }
}
