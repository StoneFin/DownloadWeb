using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Download.Models
{

        public class Product
        {
            public int ProductId { get; set; }
            [Required]
            [Display(Name = "Product Name")]
            [StringLength(200, MinimumLength=1)]
            public string ProductName { get; set; }
            public virtual ICollection<Version> Versions { get; set; }
            public Product(){
                Versions = new List<Version>();
            }
            [Required]
            public int ProductStatus { get; set; }
        }
        public class Version
        {

            public int VersionId { get; set; }
            [Required]
            [Display(Name = "Version")]
            public string VersionName { get; set; }
            [Required]
            public ICollection<Archive> Archives { get; set; }
            public Version(){
                Archives = new List<Archive>();
                ExtraFiles = new List<ExtraFile>();
            }
            public int ProductId { get; set; }
            public virtual Product Product { get; set; }
            [DataType(DataType.Date)]
            public DateTimeOffset DateCreated { get; set; }
            [Required]
            public int VersionStatus { get; set; }
            public virtual ICollection<ExtraFile> ExtraFiles { get; set; }

 

        }
        public class Archive
        {


            public virtual Version Version { get; set; }
            public int VersionId { get; set; }
            public int ArchiveId { get; set; }
            public string Installer { get; set; }
            public string Exe { get; set; }
            public string ReadMe { get; set; }
            public string ExeSize { get; set; }
            public string InstallerSize { get; set; }
            public string ReadMeSize { get; set; }
           [DataType(DataType.Date)]
           public DateTimeOffset DateUploaded { get; set; }

        }
        public class ExtraFile
        {
            public ExtraFile()
            {
                Versions = new List<Version>();
            }
            public virtual ICollection<Version> Versions { get; set; }
            public int ExtraFileId { get; set; }
            public string FileName { get; set; }
            public string FileDescription { get; set; }
            public string FileSize { get; set; }
        }

    
   
    public class ProductDBContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Version> Versions { get; set; }
        public DbSet<Archive> Archives { get; set; }
        public DbSet<ExtraFile> ExtraFiles { get; set; }

    }

}