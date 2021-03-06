﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Download.Models;
using System.IO;
using System.Diagnostics;
using PagedList;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure;
using System.Configuration;
using System.Web.Configuration;

namespace Download.Controllers
{

    [Authorize(Roles = "admin")]
    public class ProductController : Controller
    {
        //class that saves the search and page number so the back to list links return the user to where they left off


        [AllowAnonymous]
        // GET: /Product/
        public ActionResult Index(string searchString, int? page)
        {



            ViewData["search"] = searchString;

            List<Product> products = new List<Product>();
            //display 10 results per page
            int pageSize = 10;
            //return the value of page from the view, if it's null return 1
            int pageNumber = (page ?? 1);
            if (pageNumber == 0)
            {
                pageNumber = 1;
            }
            //open database connection
            using (var db = new ProductDBContext())
            {

                //blank search, return all products
                if (searchString == "" || searchString == null)
                {
                    //Return all products in Alphabetical Order
                    products = db.Products.OrderBy(p => p.ProductName).Where(x => x.ProductStatus > 0).ToList();

                }
                //user entered a search, return matching products
                else
                {
                    //if the search string is not empty, then only return the matching products to the user
                    //Return the search results in alphabetical order
                    //products = products.Where(s => s.ProductName.Contains(searchString));
                    products = db.Products.Where(s => s.ProductName.Contains(searchString)).Where(x => x.ProductStatus > 0).OrderBy(p => p.ProductName).ToList();
                    page = 1;
                }



            }
            //If there are no products in the database, display a blank Index
            if (products.Count() == 0 && searchString != null)
            {
                //return the ToPagedList to add paging to the display
                ViewBag.Message = "No Matches Found, Please try a different search";
            }
            ViewData["page"] = pageNumber;
            //return the ToPagedList to add paging to the display
            return View(products.ToPagedList(pageNumber, pageSize));
        }
        //Not Used but kept just in case
        // GET: /Product/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            ProductListView ProductModel = new ProductListView();
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
                ProductModel.ProductName = product.ProductName;
                ProductModel.Id = product.ProductId;

                foreach (var version in product.Versions)
                {
                    ProductModel.VersionName.Add(version.VersionName);
                    foreach (var arch in version.Archives)
                    {
                        ProductModel.Exe.Add(arch.Exe);
                        ProductModel.Installer.Add(arch.Installer);
                        ProductModel.ReadMe.Add(arch.ReadMe);
                    }
                }
            }
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(ProductModel);

        }
        [AllowAnonymous]
        // GET: /Product/Display/5
        //Displays a product's readme, and links for downloading
        public ActionResult Display(int? id, int? VersionId, string searchString, int? page)
        {
            //pass these values through the controllers and views so that when the 
            //user returns to the search page, they return where they left off
            ViewData["page"] = page;
            ViewData["search"] = searchString;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            ProductDisplayView ProductModel = new ProductDisplayView();
            //open database connection
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound();
                }
                int ArchiveIndex = 0;
                //If a user somehow figured out the id of a hidden
                //product and types it into the url
                //return back to the index page
                if (product.ProductStatus < 1)
                {
                    return RedirectToAction("Index");
                }
                ProductModel.ProductName = product.ProductName;
                ProductModel.Id = product.ProductId;
                //populate archives so the product can find the corresponding archives
                var archives = (from p in db.Products
                                join v in db.Versions
                                    on p.ProductId equals v.ProductId
                                join a in db.Archives
                                on v.VersionId equals a.VersionId
                                where p.ProductId == id
                                select a).ToList();

                //populate versions so the product can find the corresponding versions
                var Versions = (from p in db.Products
                                join v in db.Versions
                                on p.ProductId equals v.ProductId
                                where p.ProductId == id
                                select v).ToList();
                ProductModel.Versions = GetVisibleVersions(product.Versions.ToList());
                //Reverse the list so the most recent additions are at the lowest indicies 
                ProductModel.Versions.Reverse();
                //initiallize the markdown converter
                var md = new MarkdownDeep.Markdown();
                md.SafeMode = true;
                md.ExtraMode = true;
                //If another version was selected from the display view, display that particular version
                if (ProductModel.Versions.Count > 0)
                {
                    if (VersionId != null)
                    {
                        foreach (var version in ProductModel.Versions)
                        {
                            if (version.VersionId == VersionId)
                            {
                                //Remove the selected version and insert it at the beginning so it is displayed on the page
                                ProductModel.Versions.Remove(version);
                                ProductModel.Versions.Insert(0, version);

                                foreach (var arch in version.Archives)
                                {

                                    //grab the root path from web.config and append the specific extension to it
                                    ArchiveIndex = arch.ArchiveId;
                                    try
                                    {
                                        //Get the readme
                                        var file = GetBlob("file", arch.ReadMe);
                                        //Convert the byte[] to text so the markdown converter will work
                                        var Read = System.Text.Encoding.UTF8.GetString(file);
                                        ViewData["Content"] = md.Transform(Read);
                                    }
                                    catch (Exception ex)
                                    {
                                        ViewData["Content"] = md.Transform("Could Not Find ReadMe");
                                    }

                                }
                                version.ExtraFiles = version.ExtraFiles.Where(x => x.ExFileStatus > 0).ToList();
                                break;
                            }

                        }
                    }
                    //If no version is selected, display the default version which is the most recently added version
                    else
                    {
                        foreach (var version in ProductModel.Versions)
                        {
                            foreach (var arch in version.Archives)
                            {
                                //if there is no readMe, display 'could no find readme'
                                if (arch.ReadMe == null || arch.ReadMe == "")
                                {
                                    ViewData["Content"] = md.Transform("Could not find ReadMe");
                                }
                                else
                                {
                                    //grab the index of the archive to find it in the server
                                    ArchiveIndex = arch.ArchiveId;
                                    try
                                    {
                                        //Get the readme
                                        var file = GetBlob("file", arch.ReadMe);
                                        //Convert the byte[] to text so the markdown converter will work
                                        var Read = System.Text.Encoding.UTF8.GetString(file);
                                        ViewData["Content"] = md.Transform(Read);
                                    }
                                    catch (Exception ex)
                                    {
                                        ViewData["Content"] = md.Transform("Could not find ReadMe");
                                    }
                                }

                                break;
                            }
                            version.ExtraFiles = version.ExtraFiles.Where(x => x.ExFileStatus > 0).ToList();
                            break;
                        }

                    }
                }
                else
                {
                    ViewBag.Message1 = "No Versions Available";
                    ViewBag.Message2 = "Register and get verified to gain acess to this product";
                }
            }
            //check to see if there is an error message
            if (TempData["message"] != null)
            {
                //if there is, pass it into the view
                ViewBag.Message = TempData["message"].ToString();
            }

            if (product == null)
            {
                return HttpNotFound();
            }
            // ViewData["ExFiles"] = ExFiles;
            return View(ProductModel);

        }
        //Adds a version to the selected product
        public ActionResult AddVersion(int? id, string searchString, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            }
            ViewData["search"] = searchString;
            ViewData["page"] = page;
            List<SelectListItem> per = new List<SelectListItem>();
            //populate the select list item with the public or private options for the version
            per.Add(new SelectListItem { Text = "Public", Value = "2" });
            per.Add(new SelectListItem { Text = "Private", Value = "1" });
            ViewData["permissions"] = per;
            AddVersionView version = new AddVersionView();
            Product product = new Product();
            List<SelectListItem> PVersions = new List<SelectListItem>();

            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
                foreach (var vers in product.Versions)
                {
                    //Show only the Visible versions
                    if (vers.VersionStatus > 0)
                    {
                        PVersions.Add(new SelectListItem { Text = vers.VersionName, Value = vers.VersionId.ToString() });
                    }
                }
            }
            PVersions.Add(new SelectListItem { Text = "None", Value = "None" });
            //Reverse the order so the most recently uploaded version comes up first
            PVersions.Reverse();
            ViewData["PrevVersions"] = PVersions;
            if (product == null)
            {
                return HttpNotFound();
            }
            version.ProductName = product.ProductName;
            version.ProductId = product.ProductId;
            return View(version);
        }
        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        //POST: /Product /AddVersion
        public ActionResult AddVersion(AddVersionView version, string VStatus, string PrevVersion, string searchString, int? page)
        {
            if (ModelState.IsValid)
            {

                bool ExFileFlag = false;

                Product prod = new Product();
                Models.Version ProductVersion = new Models.Version();
                Archive ProductArchive = new Archive();
                using (var db = new ProductDBContext())
                {
                    //find the last archive's id number to anticipate the added archive's id before it is added to the database
                    //add one to the last archive id to get this archvie Id before it is put in the database
                    //This string appends the archive Id in front of the name to allow for easy access in the directories

                    prod = db.Products.Find(version.ProductId);
                    //Add the files and extract the version from the file, with the version extracted from the .exe given the highest priority
                    int i = 0;
                    //If a prevVersion was selected from the dropdown list 
                    //grab the extra files and add them to the new version
                    if (PrevVersion != "None")
                    {

                        var PVersion = db.Versions.Find(Convert.ToInt32(PrevVersion));
                        foreach (var ex in PVersion.ExtraFiles)
                        {
                            ProductVersion.ExtraFiles.Add(ex);
                        }
                    }
                    foreach (string FileName in Request.Files)
                    {
                        var fileName = Request.Files[FileName].FileName;
                        try
                        {
                            //Due to the way the multiple file upload works, there are multiple FileNames with 'FileUpload4'. However, each 'FileUpload4' contains all
                            //of the multiple selected files, and the result of iterating over them without breaking after one pass would be uploading the 
                            //number of files selected squared.
                            if (FileName == "FileUpload4")
                            {
                                //Nest the check inside the 'FileUpload4' check so that the else at the end if the first if statement doesn't 
                                //get executed

                                if (i < 1)
                                {
                                    var uploadFiles = Request.Files.GetMultiple(FileName);
                                    foreach (var file in uploadFiles)
                                    {
                                        if (file.FileName.CompareTo("") == 0)
                                        {
                                            //If this is true, then there is no file to upload, and do nothing
                                        }
                                        else
                                        {
                                            ExFileFlag = true;
                                            fileName = file.FileName;
                                            Models.ExtraFile ProductExFiles = new Models.ExtraFile();
                                            ProductExFiles.FileName = fileName;
                                            //convert the content length to bytes, and allow 3 decimal places of accuracy
                                            ProductExFiles.FileSize = ((double)file.ContentLength / 1024).ToString("F3");
                                            Upload("extrafile", file);
                                            ProductExFiles.ExFileStatus = 1;
                                            ProductExFiles.Versions.Add(ProductVersion);
                                            ProductVersion.ExtraFiles.Add(ProductExFiles);
                                            i++;
                                        }
                                    }


                                }
                            }
                            else if (fileName.Contains(".exe"))
                            {
                                ProductArchive.Exe = fileName;
                                ProductArchive.ExeSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                Request.Files[FileName].SaveAs(path);
                                Upload("file", Request.Files[FileName]);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                ProductVersion.VersionName = versionInfo.ProductVersion;
                                System.IO.File.Delete(path);
                            }
                            else if (fileName.Contains("ReadMe"))
                            {
                                ProductArchive.ReadMe = fileName;
                                ProductArchive.ReadMeSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                if (ProductVersion.VersionName == null)
                                {
                                    string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                    Request.Files[FileName].SaveAs(path);
                                    var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                    System.IO.File.Delete(path);
                                }
                                Upload("file", Request.Files[FileName]);

                            }
                            else if (fileName.CompareTo("") == 0)
                            {
                                //if the fileName is blank, then no file was selected and do nothing
                            }
                            else
                            {
                                ProductArchive.Installer = fileName;
                                ProductArchive.InstallerSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                if (ProductVersion.VersionName == null)
                                {
                                    string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                    Request.Files[FileName].SaveAs(path);
                                    var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                    System.IO.File.Delete(path);
                                }
                                Upload("file", Request.Files[FileName]);
                            }

                        }
                        catch (Exception ex)
                        {
                            ViewBag.Message = "No files chosen";
                        }


                        if (ProductVersion.VersionName == null)
                        {
                            ProductVersion.VersionName = "No Info";
                        }

                    }
                    //If there still is no version name, then we have no information on the version, so the default version is 'no Info'
                    if (ProductVersion.VersionName == null)
                    {
                        ProductVersion.VersionName = "No Info";
                    }
                    ProductArchive.DateUploaded = DateTime.Now;
                    ProductVersion.Archives.Add(ProductArchive);
                    ProductVersion.DateCreated = DateTime.Now;
                    ProductVersion.VersionStatus = Convert.ToInt32(VStatus);
                    prod.Versions.Add(ProductVersion);
                    db.SaveChanges();

                }
                if (ExFileFlag == true && ProductVersion.VersionId != 0)
                {
                    return RedirectToAction("Description", new { id = ProductVersion.VersionId, IsEdit = true, searchString = searchString, page = page });
                }
                else
                {
                    //return to the previous page
                    return RedirectToAction("Edit", new { id = prod.ProductId, searchString = searchString, page = page });
                }
            }

            return View(version);
        }

        // GET: /Product/Create
        public ActionResult Create(string searchString, int? page)
        {
            ViewData["page"] = page;
            ViewData["search"] = searchString;
            //populate the permissions drop down list with either public or private
            List<SelectListItem> per = new List<SelectListItem>();
            per.Add(new SelectListItem { Text = "Public", Value = "2" });
            per.Add(new SelectListItem { Text = "Private", Value = "1" });
            ViewData["permissions"] = per;
            return View();
        }

        // POST: /Product/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductId,ProductName,ProductStatus")] ProductCreateView product, string VStatus, string searchString, int? page)
        {
            try
            {
                //refer to the comments of AddVersion to see how most of this works
                if (ModelState.IsValid)
                {
                    int CurrVerId = 0;
                    bool ExFileFlag = false;

                    using (var db = new ProductDBContext())
                    {

                        //add one to the last archive id to get this archvie Id before it is put in the database
                        Product prod = new Product();
                        prod.ProductName = product.ProductName;
                        prod.ProductStatus = 1;
                        Models.Archive ProductArchive = new Models.Archive();
                        Models.Version ProductVersion = new Models.Version();



                        //add one to the get index because 1+LastProduct.ProductId = the new created product's Id
                        int i = 0;

                        foreach (string FileName in Request.Files)
                        {
                            var fileName = Request.Files[FileName].FileName;
                            try
                            {
                                //Due to the way the multiple file upload works, there are multiple FileNames with 'FileUpload4'. However, each 'FileUpload4' contains all
                                //of the multiple selected files, and the result of iterating over them without breaking after one pass would be uploading the 
                                //number of files selected squared.
                                if (FileName == "FileUpload4")
                                {
                                    //Nest the check inside the 'FileUpload4' check so that the else at the end if the first if statement doesn't 
                                    //get executed

                                    if (i < 1)
                                    {
                                        var uploadFiles = Request.Files.GetMultiple(FileName);
                                        foreach (var file in uploadFiles)
                                        {
                                            //increment the LastFileId by one everytime a new ExtraFile is added;
                                            if (file.FileName.CompareTo("") == 0)
                                            {
                                                //If this is true, then there is no file and nothing to be done
                                            }
                                            else
                                            {
                                                ExFileFlag = true;
                                                fileName = file.FileName;
                                                Models.ExtraFile ProductExFiles = new Models.ExtraFile();
                                                ProductExFiles.FileName = fileName;
                                                ProductExFiles.FileSize = ((double)file.ContentLength / 1024).ToString("F3");
                                                Upload("extrafile", file);
                                                ProductExFiles.ExFileStatus = 1;
                                                ProductExFiles.Versions.Add(ProductVersion);
                                                ProductVersion.ExtraFiles.Add(ProductExFiles);
                                                i++;
                                            }
                                        }


                                    }
                                }
                                else if (fileName.Contains(".exe"))
                                {
                                    ProductArchive.Exe = fileName;
                                    ProductArchive.ExeSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                    string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                    Request.Files[FileName].SaveAs(path);
                                    Upload("file", Request.Files[FileName]);
                                    var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                    System.IO.File.Delete(path);
                                }
                                else if (fileName.Contains("ReadMe"))
                                {
                                    ProductArchive.ReadMe = fileName;
                                    ProductArchive.ReadMeSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                    if (ProductVersion.VersionName == null)
                                    {
                                        string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                        Request.Files[FileName].SaveAs(path);
                                        var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                        ProductVersion.VersionName = versionInfo.ProductVersion;
                                        System.IO.File.Delete(path);
                                    }
                                    Upload("file", Request.Files[FileName]);

                                }
                                else if (fileName.CompareTo("") == 0)
                                {
                                    //if the fileName is blank, then no file was selected and do nothing
                                }
                                else
                                {
                                    ProductArchive.Installer = fileName;
                                    ProductArchive.InstallerSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                    if (ProductVersion.VersionName == null)
                                    {
                                        string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                        Request.Files[FileName].SaveAs(path);
                                        var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                        ProductVersion.VersionName = versionInfo.ProductVersion;
                                        System.IO.File.Delete(path);
                                    }
                                    Upload("file", Request.Files[FileName]);
                                }

                            }
                            catch (Exception ex)
                            {
                                ViewBag.Message = "No files chosen";
                            }


                            if (ProductVersion.VersionName == null)
                            {
                                ProductVersion.VersionName = "No Info";
                            }

                        }

                        ProductArchive.DateUploaded = DateTime.Now;
                        ProductVersion.Archives.Add(ProductArchive);
                        ProductVersion.VersionStatus = Convert.ToInt32(VStatus);
                        ProductVersion.DateCreated = DateTime.Now;
                        prod.Versions.Add(ProductVersion);
                        db.Products.Add(prod);
                        db.SaveChanges();
                        //Grab the Id of the version we just added 
                        var CurVer = db.Versions.OrderByDescending(x => x.VersionId).First();
                        CurrVerId = CurVer.VersionId;
                        

                    }
                    if (ExFileFlag == true && CurrVerId != 0)
                    {
                        return RedirectToAction("Description", new { id = CurrVerId, searchString = searchString, page = page, IsEdit = false });
                    }
                    else
                    {
                        return RedirectToAction("Index", new { searchString = searchString, page = page });
                    }
                }
            }
            catch (Exception ex)
            {
                string error = ex.ToString();
                ViewBag.Message = error;
             
            }


            return View(product);
        }
        //GET: /Product/Description/5
        public ActionResult Description(int? id, string searchString, int? page, bool? IsEdit)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewData["page"] = page;
            ViewData["search"] = searchString;
            ViewData["edit"] = IsEdit;
            List<ExtraFile> ExFiles = new List<ExtraFile>();
            Models.Version version = new Models.Version();
            using (var db = new ProductDBContext())
            {
                version = db.Versions.Find(id);
                foreach (var ex in version.ExtraFiles)
                {
                    ExFiles.Add(ex);
                }
            }
            return View(version);
        }
        //POST: /Product/Description/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Description([Bind(Include = "VersionId,ProductId")] Models.Version Version, string searchString, int? page, string[] Description, bool? IsEdit)
        {

            ExtraFile ExFiles = new ExtraFile();
            using (var db = new ProductDBContext())
            {
                var version = db.Versions.Find(Version.VersionId);
                int i = 0;
                foreach (var ex in version.ExtraFiles)
                {
                    if (ex.FileDescription == null)
                    {
                        ex.FileDescription = Description[i];
                        i++;
                    }

                }
                db.SaveChanges();
            }
            //If this is true, then we are coming from the edit version, in which case we want to redirect back to
            // the edit controller
            if (IsEdit == true)
            {
                return RedirectToAction("Edit", new { id = Version.ProductId, searchString = searchString, page = page });
            }
            else
            {
                return RedirectToAction("Index", new { searchString = searchString, page = page });
            }
        }

        // GET: /Product/Edit/5
        public ActionResult Edit(int? id, string searchString, int? page)
        {
            ViewData["page"] = page;
            ViewData["search"] = searchString;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            using (var db = new ProductDBContext())
            {
                //populate the archives and versions so the product can find the corresponding versions and archives
                var AllArchives = (from v in db.Versions
                                   join p in db.Products on v.ProductId equals p.ProductId
                                   where p.ProductId == (int)id
                                   select v.Archives).ToList();

                var allVersions = (from p in db.Products
                                   where p.ProductId == (int)id
                                   select p.Versions).ToList();
                product = db.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound();
                }
                //grab only the visible versions becuase if a version is invisible, then it was removed by the admin, and reverse it to show the most recent uploads first
                product.Versions = GetVisibleVersions(product.Versions.Reverse().ToList());
                foreach (var version in product.Versions)
                {
                    //populate the list.  I found that if I did not do this, the product's versions and archives would be null
                    foreach (var arch in version.Archives)
                    {

                    }
                    version.ExtraFiles = version.ExtraFiles.Where(x => x.ExFileStatus > 0).ToList();
                }

            }

            return View(product);
        }

        // POST: /Product/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductId,ProductName,ProductStatus")] Product product, string searchString, int? page)
        {

            if (ModelState.IsValid)
            {
                using (var db = new ProductDBContext())
                {
                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return RedirectToAction("Index", new { searchString = searchString, page = page });
            }
            return View(product);
        }
        //Edits a selected version that is attached to a product
        public ActionResult EditVersion(int? id, int? page, string searchString)
        {
            ViewData["search"] = searchString;
            ViewData["page"] = page;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.Version version;
            using (var db = new ProductDBContext())
            {
                //populate the archives and version becuase again, I found that If i did not do this, that everything would be null
                var archives = (from v in db.Versions
                                where v.VersionId == id
                                select v.Archives).ToList();
                var ExFiles = (from v in db.Versions
                               where v.VersionId == id
                               select v.ExtraFiles).ToList();

                version = db.Versions.Find(id);
                if (version == null)
                {
                    return HttpNotFound();
                }
                //Populate the Archives
                foreach (var arch in version.Archives)
                {

                }
                //Popluate the Extra Files
                version.ExtraFiles = version.ExtraFiles.Where(x => x.ExFileStatus > 0).ToList();


            }

            List<SelectListItem> per = new List<SelectListItem>();
            //Check to see what current status is, and populate the list so
            //the current one is shown first
            if (version.VersionStatus > 1)
            {
                per.Add(new SelectListItem { Text = "Public", Value = "2" });
                per.Add(new SelectListItem { Text = "Private", Value = "1" });
            }
            else
            {
                per.Add(new SelectListItem { Text = "Private", Value = "1" });
                per.Add(new SelectListItem { Text = "Public", Value = "2" });


            }
            ViewData["permissions"] = per;
            return View(version);
        }

        // POST: /Product/EditVersion/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditVersion([Bind(Include = "VersionName,VersionId")] Models.Version version, string VStatus, string[] Description, string searchString, int? page)
        {
            if (ModelState.IsValid)
            {
                bool ExFileFlag = false;
                string versName = version.VersionName;
                Models.Version vers = new Models.Version();
                List<ExtraFile> ExFile = new List<ExtraFile>();
                Archive arch = new Archive();
                using (var db = new ProductDBContext())
                {

                    int ArchIndex = 0;

                    var AllArchives = (from v in db.Versions
                                       where v.VersionId == version.VersionId
                                       select v.Archives).ToList();

                    var AllFiles = (from v in db.Versions
                                    where v.VersionId == version.VersionId
                                    select v.ExtraFiles).ToList();


                    vers = db.Versions.Find(version.VersionId);

                    foreach (var archive in vers.Archives)
                    {
                        arch = db.Archives.Find(archive.ArchiveId);
                        ArchIndex = archive.ArchiveId;
                    }
                    foreach (var ex in vers.ExtraFiles)
                    {
                        ExFile.Add(ex);
                    }
                    ExFile = ExFile.Where(x => x.ExFileStatus > 0).ToList();

                    //Keeps track of the number of Extra Files added
                    int i = 0;
                    //Keeps track of the index of the Exfiles to correspond with the files in 'UploadFile4'
                    int j = 0;
                    //Makes sure that the 'fileUpload4' only saves once
                    int k = 0;

                    foreach (string FileName in Request.Files)
                    {
                        var fileName = Request.Files[FileName].FileName;
                        try
                        {

                            //Due to the way the multiple file upload works, there are multiple FileNames with 'FileUpload5'. However, each 'FileUpload5' contains all
                            //of the multiple selected files, and the result of iterating over them without breaking after one pass would be uploading the 
                            //number of files selected squared.
                            if (FileName == "FileUpload5" && fileName.CompareTo("") != 0)
                            {
                                //Nest the check inside the 'FileUpload4' check so that the else at the end if the first if statement doesn't 
                                //get executed
                                ExFileFlag = true;
                                if (i < 1)
                                {

                                    var uploadFiles = Request.Files.GetMultiple(FileName);
                                    foreach (var file in uploadFiles)
                                    {
                                        Models.ExtraFile ProductExFiles = new Models.ExtraFile();
                                        ProductExFiles.FileName = fileName;
                                        ProductExFiles.FileSize = ((double)file.ContentLength / 1024).ToString("F3");
                                        Upload("extrafile", file);
                                        ProductExFiles.ExFileStatus = 1;
                                        ProductExFiles.Versions.Add(vers);
                                        vers.ExtraFiles.Add(ProductExFiles);
                                        i++;
                                    }


                                }
                            }
                            else if (FileName == "FileUpload4")
                            {
                                if (k < 1)
                                {
                                    var uploadFiles = Request.Files.GetMultiple(FileName);
                                    foreach (var file in uploadFiles)
                                    {
                                        if (file.FileName.CompareTo("") == 0)
                                        {
                                            ExFile[j].FileDescription = Description[j];
                                            j++;
                                        }
                                        else
                                        {
                                            fileName = file.FileName;
                                            ExtraFile ProductExFiles = new ExtraFile();
                                            ProductExFiles.FileName = fileName;
                                            ProductExFiles.FileSize = ((double)file.ContentLength / 1024).ToString("F3");
                                            ProductExFiles.FileDescription = Description[j];
                                            ProductExFiles.ExFileStatus = 1;
                                            Upload("extrafile", file);
                                            ExFile[j].ExFileStatus = 0;
                                            ProductExFiles.Versions.Add(vers);
                                            vers.ExtraFiles.Add(ProductExFiles);
                                            j++;
                                        }
                                    }
                                    k++;
                                }

                            }
                            else if (fileName.Contains(".exe"))
                            {
                                arch.Exe = fileName;
                                arch.ExeSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                Request.Files[FileName].SaveAs(path);
                                Upload("file", Request.Files[FileName]);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                vers.VersionName = versionInfo.ProductVersion;
                                System.IO.File.Delete(path);
                            }
                            else if (fileName.Contains("ReadMe"))
                            {
                                arch.ReadMe = fileName;
                                arch.ReadMeSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                if (vers.VersionName == null)
                                {
                                    string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                    Request.Files[FileName].SaveAs(path);
                                    var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                    vers.VersionName = versionInfo.ProductVersion;
                                    System.IO.File.Delete(path);
                                }
                                Upload("file", Request.Files[FileName]);
                            }
                            else if (fileName.CompareTo("") == 0)
                            {
                                //if the fileName is blank, then no file was selected and do nothing
                            }
                            else
                            {
                                arch.Installer = fileName;
                                arch.InstallerSize = ((double)Request.Files[FileName].ContentLength / 1024).ToString("F3");
                                if (vers.VersionName == null)
                                {
                                    string path = Path.Combine(Server.MapPath("~/App_Data"), Request.Files[FileName].FileName);
                                    Request.Files[FileName].SaveAs(path);
                                    var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                    vers.VersionName = versionInfo.ProductVersion;
                                    System.IO.File.Delete(path);
                                }
                                Upload("file", Request.Files[FileName]);
                            }

                        }

                        catch (Exception ex)
                        {
                            ViewBag.Message = "No files chosen";
                        }
                    }
                    if (versName != null)
                    {
                        vers.VersionName = versName;
                    }
                    vers.VersionStatus = Convert.ToInt32(VStatus);
                    db.SaveChanges();
                }
                if (ExFileFlag == true && version.VersionId != 0)
                {
                    return RedirectToAction("Description", new { id = version.VersionId, searchString = searchString, page = page, IsEdit = true });
                }
                else
                {
                    return RedirectToAction("Edit", new { id = vers.ProductId, searchString = searchString, page = page });
                }
            }
            return View(version);

        }

        //Not used, put left just in case
        // GET: /Product/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
            }
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }
        //not used
        // POST: /Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product;
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
                db.Products.Remove(product);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        //Hides the product from being seen by all users, without actually deleting it
        // GET: /Product/Remove/5
        public ActionResult Remove(int? id, string searchString, int? page)
        {
            ViewData["page"] = page;
            ViewData["search"] = searchString;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
            }
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: /Product/Remove/5
        [HttpPost, ActionName("Remove")]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveConfirmed(int id, string searchString, int? page)
        {

            Product product;
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
                //change the ProductStatus to 0 which means 'not visible'
                product.ProductStatus = 0;
                db.SaveChanges();
            }
            return RedirectToAction("Index", new { searchString = searchString, page = page });
        }
        //Hides a specified version from all users, without actually deleting it
        // GET: /Product/RemoveVersion/5
        public ActionResult RemoveVersion(int? id, string searchString, int? page)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewData["search"] = searchString;
            ViewData["page"] = page;
            Models.Version version;
            using (var db = new ProductDBContext())
            {
                version = db.Versions.Find(id);
            }
            if (version == null)
            {
                return HttpNotFound();
            }
            return View(version);
        }

        // POST: /Product/RemoveVersion/5
        [HttpPost, ActionName("RemoveVersion")]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveVersionConfirmed(int id, string searchString, int? page, int ProductId)
        {

            Models.Version versions;
            using (var db = new ProductDBContext())
            {
                versions = db.Versions.Find(id);
                //sets the VersionStatus to zero which is invisible to all users
                versions.VersionStatus = 0;
                db.SaveChanges();
            }
            return RedirectToAction("Edit", new { searchString = searchString, page = page, id = ProductId });
        }
        //Hides a specified version from all users, without actually deleting it
        // GET: /Product/RemoveVersion/5
        public ActionResult RemoveFile(int? Vid, int? Fid, string searchString, int? page)
        {
            ViewData["search"] = searchString;
            ViewData["page"] = page;

            if (Vid == null || Fid == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ExtraFile ExFile;
            using (var db = new ProductDBContext())
            {
                ExFile = db.ExtraFiles.Find(Fid);
            }
            if (ExFile == null)
            {
                return HttpNotFound();
            }
            ViewData["Vid"] = Vid;
            return View(ExFile);
        }

        // POST: /Product/RemoveVersion/5
        [HttpPost, ActionName("RemoveFile")]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFileConfirmed(int Fid, int Vid, string searchString, int? page)
        {

            Models.Version versions;
            ExtraFile ExFile;
            using (var db = new ProductDBContext())
            {
                versions = db.Versions.Find(Vid);
                ExFile = db.ExtraFiles.Find(Fid);
                ExFile.ExFileStatus = 0;
                db.SaveChanges();
            }
            return RedirectToAction("EditVersion", new { id = Vid, searchString = searchString, page = page });
        }
        //This was a default method when I created the controller and I'm not sure what it does
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                using (var db = new ProductDBContext())
                {
                    db.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        //Initially created in an earlier implementation of this, but it is not used now
        // GET: /Product/DeleteVersion/5
        public ActionResult DeleteVersion(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.Version version;
            using (var db = new ProductDBContext())
            {
                version = db.Versions.Find(id);
            }
            if (version == null)
            {
                return HttpNotFound();
            }
            return View(version);
        }

        // POST: /Product/DeleteVersion/5
        [HttpPost, ActionName("DeleteVersion")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteVersionConfirmed(int id)
        {
            Models.Version version;
            using (var db = new ProductDBContext())
            {
                version = db.Versions.Find(id);
                db.Versions.Remove(version);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        [AllowAnonymous]
        [HttpPost]
        //Dowload Action which returns the selected file to the user
        public ActionResult Download(string fileName, int id, int verId, int? archId, int? fileId, string searchString, int? page)
        {
            //If there is an arch ID, then the user wants either a .exe or an Installer
            if (archId != null)
            {
                try
                {

                    byte[] fileBytes = GetBlob("file", fileName);
                    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                catch (Exception ex)
                {
                    //error message for file not found
                    TempData["message"] = "Error, could not find file " + fileName;
                    //if there was an error, then redirect to the same page
                }
            }
            //If there is a file ID then the user want an extra file
            else if (fileId != null)
            {
                try
                {
                    byte[] fileBytes = GetBlob("extrafile", fileName);
                    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                catch (Exception ex)
                {
                    //error message for file not found
                    TempData["message"] = "Error, could not find file " + fileName;
                    //if there was an error, then redirect to the same page
                }
            }
            return RedirectToAction("Display", new { id = id, searchString = searchString, page = page, VersionId = verId });
        }

        //this method gets the proper index of the folder that the product is in
        //This method also makes it so a new folder is created for every 1000 products

        public int GetProductIndex(int id)
        {
            int index = 0;
            //extract the index
            index = (int)id / 1000;
            //add one so the name of the folder is inclusive
            index = index + 1;
            //multiply by 1000 to get proper index
            index = index * 1000;
            return index;
        }
        public int GetVersionIndex(int id)
        {
            int index = 0;
            //extract the index
            index = (int)id / 300;
            //add one so the name of the folder is inclusive
            index = index + 1;
            //multiply by 300 to get proper index
            index = index * 300;
            return index;
        }
        //This method return a product list of visible products, e.g., if thier
        //Status number is 1

        //This method figures out which versions should be hidden and which should be available to certain users
        public List<Models.Version> GetVisibleVersions(List<Models.Version> versions)
        {
            List<Models.Version> Vversions = new List<Models.Version>();
            //if the user is an admin or member, they have access to all visible files, e.g. the VersionStatus Number is greater than one
            if (User.IsInRole("admin") == true || User.IsInRole("member") == true)
            {
                foreach (var vers in versions)
                {
                    if (vers.VersionStatus > 0)
                    {
                        Vversions.Add(vers);
                    }
                }
                return Vversions;
            }
            //if they are not a member or admin, then they are either an annonomous user, or a non-validated member, both of wich have the same permissions
            //to view public files, whos VersionStatus number is 2.
            else
            {
                foreach (var vers in versions)
                {
                    if (vers.VersionStatus > 1)
                    {
                        Vversions.Add(vers);
                    }
                }
                return Vversions;
            }

        }
        public void Upload(string containerName, System.Web.HttpPostedFileBase file)
        {
#if DEBUG
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);
#else
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
#endif
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            // Retrieve reference to a blob.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(file.FileName);

            // Create or overwrite the blob with contents from a local file.
            using (var fileStream = file.InputStream)
            {
                blockBlob.UploadFromStream(fileStream);
            }
        }
        public byte[] GetBlob(string containerName, string fileName)
        {
#if DEBUG
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);
#else
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
#endif

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to a blob.
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);


            string path = Path.Combine(Server.MapPath("~/App_Data"), fileName);
            using (var fileStream = System.IO.File.OpenWrite(path))
            {
                blockBlob.DownloadToStream(fileStream);
            }
            byte[] fileBytes = System.IO.File.ReadAllBytes(path);
            System.IO.File.Delete(path);
            return fileBytes;
 
        }

    }


}


