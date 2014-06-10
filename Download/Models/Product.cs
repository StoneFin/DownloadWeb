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
            public string ProductName { get; set; }
            public virtual ICollection<Version> Versions { get; set; }
            public Product(){
                Versions = new List<Version>();
            }
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
            }
            public int ProductId { get; set; }
            public virtual Product Product { get; set; }
            [DataType(DataType.Date)]
            public DateTimeOffset DateCreated { get; set; }

 

        }
        public class Archive
        {

            public virtual Version Version { get; set; }
            public int VersionId { get; set; }
            public int ArchiveId { get; set; }
            public string Installer { get; set; }
            public string Exe { get; set; }
            public string ReadMe { get; set; }
           [DataType(DataType.Date)]
           public DateTimeOffset DateUploaded { get; set; }
        }
        public class GetMostRecentVersion
        {
            public Version GetMostRecenVersion(System.Collections.Generic.List<Version> versions)
            {
                Version MostRecent = new Version();
                MostRecent = versions[0];
                foreach (var version in versions)
                {
                    if (MostRecent.VersionName.CompareTo(version.VersionName) < 0)
                    {
                        MostRecent = version;
                    }
                }
                return MostRecent;
            }
        }
    
   
    public class ProductDBContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Version> Versions { get; set; }
        public DbSet<Archive> Archives { get; set; }

    }

}