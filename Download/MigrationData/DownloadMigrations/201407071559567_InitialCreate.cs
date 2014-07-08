namespace Download.MigrationData.DownloadMigrations
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
                        ExeSize = c.String(),
                        InstallerSize = c.String(),
                        ReadMeSize = c.String(),
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
                        VersionStatus = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.VersionId)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId);
            
            CreateTable(
                "dbo.ExtraFiles",
                c => new
                    {
                        ExtraFileId = c.Int(nullable: false, identity: true),
                        FileName = c.String(),
                        FileDescription = c.String(),
                        FileSize = c.String(),
                        ExFileStatus = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ExtraFileId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        ProductId = c.Int(nullable: false, identity: true),
                        ProductName = c.String(nullable: false, maxLength: 200),
                        ProductStatus = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ProductId);
            
            CreateTable(
                "dbo.ExtraFileVersions",
                c => new
                    {
                        ExtraFile_ExtraFileId = c.Int(nullable: false),
                        Version_VersionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ExtraFile_ExtraFileId, t.Version_VersionId })
                .ForeignKey("dbo.ExtraFiles", t => t.ExtraFile_ExtraFileId, cascadeDelete: true)
                .ForeignKey("dbo.Versions", t => t.Version_VersionId, cascadeDelete: true)
                .Index(t => t.ExtraFile_ExtraFileId)
                .Index(t => t.Version_VersionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Versions", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ExtraFileVersions", "Version_VersionId", "dbo.Versions");
            DropForeignKey("dbo.ExtraFileVersions", "ExtraFile_ExtraFileId", "dbo.ExtraFiles");
            DropForeignKey("dbo.Archives", "VersionId", "dbo.Versions");
            DropIndex("dbo.Versions", new[] { "ProductId" });
            DropIndex("dbo.ExtraFileVersions", new[] { "Version_VersionId" });
            DropIndex("dbo.ExtraFileVersions", new[] { "ExtraFile_ExtraFileId" });
            DropIndex("dbo.Archives", new[] { "VersionId" });
            DropTable("dbo.ExtraFileVersions");
            DropTable("dbo.Products");
            DropTable("dbo.ExtraFiles");
            DropTable("dbo.Versions");
            DropTable("dbo.Archives");
        }
    }
}
