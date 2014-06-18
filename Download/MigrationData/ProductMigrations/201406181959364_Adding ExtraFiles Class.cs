namespace Download.MigrationData.ProductMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingExtraFilesClass : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ExtraFiles", "Archive_ArchiveId", "dbo.Archives");
            DropIndex("dbo.ExtraFiles", new[] { "Archive_ArchiveId" });
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
            
            DropColumn("dbo.ExtraFiles", "VersionId");
            DropColumn("dbo.ExtraFiles", "Archive_ArchiveId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ExtraFiles", "Archive_ArchiveId", c => c.Int());
            AddColumn("dbo.ExtraFiles", "VersionId", c => c.Int(nullable: false));
            DropForeignKey("dbo.ExtraFileVersions", "Version_VersionId", "dbo.Versions");
            DropForeignKey("dbo.ExtraFileVersions", "ExtraFile_ExtraFileId", "dbo.ExtraFiles");
            DropIndex("dbo.ExtraFileVersions", new[] { "Version_VersionId" });
            DropIndex("dbo.ExtraFileVersions", new[] { "ExtraFile_ExtraFileId" });
            DropTable("dbo.ExtraFileVersions");
            CreateIndex("dbo.ExtraFiles", "Archive_ArchiveId");
            AddForeignKey("dbo.ExtraFiles", "Archive_ArchiveId", "dbo.Archives", "ArchiveId");
        }
    }
}
