namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Archives",
                c => new
                    {
                        ArchiveId = c.Int(nullable: false, identity: true),
                        VersionId = c.Int(nullable: false),
                        Installer = c.String(),
                        Exe = c.String(),
                        ReadMe = c.String(),
                        DateUploaded = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.ArchiveId)
                .ForeignKey("dbo.Versions", t => t.VersionId, cascadeDelete: true)
                .Index(t => t.VersionId);
            
            CreateTable(
                "dbo.Versions",
                c => new
                    {
                        VersionId = c.Int(nullable: false, identity: true),
                        VersionName = c.String(nullable: false),
                        ProductId = c.Int(nullable: false),
                        DateCreated = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.VersionId)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        ProductId = c.Int(nullable: false, identity: true),
                        ProductName = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ProductId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Versions", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Archives", "VersionId", "dbo.Versions");
            DropIndex("dbo.Versions", new[] { "ProductId" });
            DropIndex("dbo.Archives", new[] { "VersionId" });
            DropTable("dbo.Products");
            DropTable("dbo.Versions");
            DropTable("dbo.Archives");
        }
    }
}
