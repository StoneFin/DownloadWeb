using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Download.Models
{
    public class ProductListView
    {
        public ProductListView()
        {
            VersionName = new List<string>();
            ReadMe = new List<string>();
            Installer = new List<string>();
            Exe = new List<string>();
        }
        public int Id { get; set; }
        [Display(Name = "Product")]
        public string ProductName { get; set; }
        [Display(Name ="Version")]
        public List<string> VersionName { get; set; }
        public List<string> ReadMe { get; set; }
        public List<string> Installer { get; set; }
        public List<string> Exe { get; set; }
        [DataType(DataType.DateTime)]
        public List<DateTimeOffset> DateUploaded { get; set; }
        [DataType(DataType.DateTime)]
        public DateTimeOffset DateCreated { get; set; }
        
    }
    public class ProductCreateView
    {
               public ProductCreateView() { }
        public int Id { get; set; }
        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }
        [Display(Name = "Version")]
        public string VersionName { get; set; }
        public string ReadMe { get; set; }
        public string Installer { get; set; }
        public string Exe { get; set; }
        public int ProductStatus { get; set; }
        public int VersionStatus { get; set; }
        public string ExtraFiles { get; set; }
    }
    public class ProductDisplayView
    {
        public ProductDisplayView()
        {
            Versions = new List<Version>();
        }
        public int Id { get; set; }
        [Display(Name = "Product")]
        public string ProductName { get; set; }
        [Display(Name = "Version")]
        public List<Version> Versions { get; set; }
        
    }
    public class AddVersionView
    {
        [Display(Name = "Version")]
        public string VersionName { get; set; }
        public string Installer { get; set; }
        public string Exe { get; set; }
        public string ReadMe { get; set; }
        [DataType(DataType.DateTime)]
        public DateTimeOffset DatUploaded { get; set; }
        public string ProductName { get; set; }
        public int ProductId { get; set; }
        public int VersionStatus { get; set; }
 
    }

    
}