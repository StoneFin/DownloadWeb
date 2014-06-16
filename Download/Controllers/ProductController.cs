using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Download.Models;
using System.IO;
using System.Diagnostics;
using System.Text;
using PagedList;
using Microsoft.AspNet.Identity;

namespace Download.Controllers
{

    [Authorize(Roles = "admin")]
    public class ProductController : Controller
    {
        //class that saves the search and page number so the back to list links return the user to where they left off
        public class SaveInfo
        {
            public static int? page;
            public static string search;
        }

        [AllowAnonymous]
        // GET: /Product/
        public ActionResult Index(string searchString, int? page)
        {
            //if null, then check to see if there is any data in the saved search
            if (searchString == null)
            {
                searchString = SaveInfo.search;
            }
                //if they are the same, the the user re-typed the same seach, and redirect back to page one
            else if(searchString == SaveInfo.search)
            {
                page = 1;
            }
                //The user entered a new search, start out on page one
            else
            {
                SaveInfo.search = searchString;
                page = 1;
            }
            //if page is null, then check to see if there is a saved page
            if (page == null)
            {
                page = SaveInfo.page;
            }
                //else display selected page and save the page
            else
            {
                SaveInfo.page = page;
            }
            ViewData["search"] = searchString;
            List<Product> products = new List<Product>();
            //display 10 results per page
            int pageSize = 10;
            //return the value of page from the view, if it's null return 1
            int pageNumber = (page ?? 1);
            //open database connection
            using (var db = new ProductDBContext())
            {
                //Selects all products in the database
                var Sproducts = from p in db.Products
                                select p;
                //blank search, return all products
                if (searchString == "")
                {
                    //Return all products in Alphabetical Order
                    products = db.Products.OrderBy(p => p.ProductName).ToList();

                }
                    //user entered a search, return matching products
                else if (searchString != null)
                {
                    //if the search string is not empty, then only return the matching products to the user
                    //Return the search results in alphabetical order
                    Sproducts = Sproducts.Where(s => s.ProductName.Contains(searchString));
                    products = Sproducts.OrderBy(p => p.ProductName).ToList();
                    page = 1;
                }
                    //page was just initialized and display nothing
                else
                {
                }
                //call show only visible products to hide the products that were removed
                products = ShowOnlyVisible(products);
                //If there are no products in the database, display a blank Index
                if (products.Count() == 0 && searchString != null)
                {

                    //return the ToPagedList to add paging to the display
                    ViewBag.Message = "No Matches Found, Please try a different search";
                    return View(products.ToPagedList(pageNumber, pageSize));
                }

            }
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
        public ActionResult Display(int? id, int? VersionId)
        {


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
                int ArchiveIndex = 0;
                //If a user somehow figured out the id of a hidden
                //product and types it into the url
                //return back to the index page
                if (product.ProductStatus < 1)
                {
                    return RedirectToAction("index");
                }
                //populate archives so the product can find the corresponding archives
                var archives = db.Archives.ToList();
                ProductModel.ProductName = product.ProductName;
                ProductModel.Id = product.ProductId;
                //populate versions so the product can find the corresponding versions
                var Versions = db.Versions.ToList();
                ProductModel.Versions = GetVisibleVersions(product.Versions.ToList());
                //Reverse the list so the most recent additions are at the lowest indicies 
                ProductModel.Versions.Reverse();
                //initiallize the markdown converter
                var md = new MarkdownDeep.Markdown();
                md.SafeMode = true;
                md.ExtraMode = true;
                //If another version was selected from the display view, display that particular version
                string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + GetIndex(product.ProductId).ToString("D8") + "\\";
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
                                string fileName = filePath + GetIndex(ArchiveIndex).ToString("D8") + "\\" + arch.ArchiveId.ToString() + "_" + arch.ReadMe.ToString();
                                try
                                {
                                    var file = System.IO.File.ReadAllText(fileName);
                                    ViewData["Content"] = md.Transform(file);
                                }
                                catch (Exception ex)
                                {
                                    ViewData["Content"] = md.Transform("Could Not Find ReadMe");
                                }
                            }
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
                                string fileName = filePath + GetIndex(ArchiveIndex).ToString("D8") + "\\" + arch.ArchiveId.ToString() + "_" + arch.ReadMe.ToString();
                                try
                                {
                                    var file = System.IO.File.ReadAllText(fileName);
                                    //convert the markdown readme into Html
                                    ViewData["Content"] = md.Transform(file);
                                }
                                catch (Exception ex)
                                {
                                    ViewData["Content"] = md.Transform("Could not find ReadMe");
                                }
                            }
                            break;
                        }
                        break;
                    }

                }
            }



            if (product == null)
            {
                return HttpNotFound();
            }
            return View(ProductModel);

        }
        //Adds a version to the selected product
        public ActionResult AddVersion(int id)
        {
            List<SelectListItem> per = new List<SelectListItem>();
            //populate the select list item with the public or private options for the version
            per.Add(new SelectListItem { Text = "Public", Value = "2" });
            per.Add(new SelectListItem { Text = "Private", Value = "1" });
            ViewData["permissions"] = per;
            AddVersionView version = new AddVersionView();
            Product product = new Product();
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
            }
            version.ProductName = product.ProductName;
            version.ProductId = product.ProductId;
            return View(version);
        }
        [HttpPost]
        public ActionResult AddVersion(AddVersionView version, string VStatus)
        {
            if (ModelState.IsValid)
            {
                Product prod = new Product();
                Models.Version ProductVersion = new Models.Version();
                Archive ProductArchive = new Archive();
                using (var db = new ProductDBContext())
                {
                    //find the last archive's id number to anticipate the added archive's id before it is added to the database
                    var LastArchive = db.Archives.ToList().Last();
                    //add one to the last archive id to get this archvie Id before it is put in the database
                    //This string appends the archive Id in front of the name to allow for easy access in the directories
                    string CurrArchId = (LastArchive.ArchiveId + 1).ToString() + "_";
                    int ArchIndex = LastArchive.ArchiveId + 1;
                    prod = db.Products.Find(version.ProductId);
                    //Add the files and extract the version from the file, with the version extracted from the .exe given the highest priority
                    string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + GetIndex(prod.ProductId).ToString("D8") + "\\" + GetIndex(ArchIndex).ToString("D8") + "\\";
                    foreach (string file in Request.Files)
                    {
                        try
                        {
                            var fileName = Request.Files[file].FileName;
                            if (fileName.Contains(".exe"))
                            {
                                ProductArchive.Exe = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                //grab the version out of the .exe file
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                ProductVersion.VersionName = versionInfo.ProductVersion;
                            }
                            else if (fileName.Contains("ReadMe"))
                            {
                                ProductArchive.ReadMe = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                //If there is no version try to get it out of the ReadMe
                                if (ProductVersion.VersionName == null)
                                {
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                }
                            }
                            else if (fileName.CompareTo("") == 0)
                            {
                                //If the fileName is blank, then no file was selected, and do nothing

                            }
                            else
                            {
                                ProductArchive.Installer = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                //if there still is no version, try to grap it out of the installer
                                if (ProductVersion.VersionName == null)
                                {
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                }
                            }

                        }

                        catch (Exception ex)
                        {
                            ViewBag.Message = "No files chosen";
                        }
                    }
                    //If there still is no version name, then we have no information on the version, so the default version is 'no info'
                    if (ProductVersion.VersionName == null)
                    {
                        ProductVersion.VersionName = "No info";
                    }
                    ProductArchive.DateUploaded = DateTime.Now;
                    ProductVersion.Archives.Add(ProductArchive);
                    ProductVersion.DateCreated = DateTime.Now;
                    ProductVersion.VersionStatus = Convert.ToInt32(VStatus);
                    prod.Versions.Add(ProductVersion);
                    db.SaveChanges();

                }
                //return to the previous page
                return RedirectToAction("Edit/" + version.ProductId.ToString());
            }

            return View(version);
        }

        // GET: /Product/Create
        public ActionResult Create()
        {
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
        public ActionResult Create([Bind(Include = "ProductId,ProductName,ProductStatus")] ProductCreateView product, string VStatus)
        {
            //refer to the comments of AddVersion to see how most of this works
            if (ModelState.IsValid)
            {

                using (var db = new ProductDBContext())
                {
                    Product prod = new Product();
                    prod.ProductName = product.ProductName;
                    //grab the last product to anticipate where the new product will go before putting it in the database
                    var LastProduct = db.Products.ToList().Last();
                    var LastArchive = db.Archives.ToList().Last();
                    int ArchIndex = LastArchive.ArchiveId + 1;
                    //add one to the last archive id to get this archvie Id before it is put in the database
                    string CurrArchId = (LastArchive.ArchiveId + 1).ToString() + "_";
                    prod.ProductStatus = 1;
                    Models.Archive ProductArchive = new Models.Archive();
                    Models.Version ProductVersion = new Models.Version();

                    //add one to the get index because 1+LastProduct.ProductId = the new created product's Id
                    string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + GetIndex((LastProduct.ProductId + 1)).ToString("D8") + "\\" + GetIndex(ArchIndex).ToString("D8") + "\\";
                    foreach (string file in Request.Files)
                    {
                        try
                        {
                            var fileName = Request.Files[file].FileName;
                            if (fileName.Contains(".exe"))
                            {
                                ProductArchive.Exe = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                ProductVersion.VersionName = versionInfo.ProductVersion;
                            }
                            else if (fileName.Contains("ReadMe"))
                            {
                                ProductArchive.ReadMe = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                if (ProductVersion.VersionName == null)
                                {
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                }
                            }
                            else if (fileName.CompareTo("") == 0)
                            {
                                //if the fileName is blank, then no file was selected and do nothing
                            }
                            else
                            {
                                ProductArchive.Installer = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                if (ProductVersion.VersionName == null)
                                {
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                }
                            }


                        }

                        catch (Exception ex)
                        {
                            ViewBag.Message = "No files chosen";
                        }
                    }
                    if (ProductVersion.VersionName == null)
                    {
                        ProductVersion.VersionName = "No info";
                    }
                    ProductArchive.DateUploaded = DateTime.Now;
                    ProductVersion.Archives.Add(ProductArchive);
                    ProductVersion.VersionStatus = Convert.ToInt32(VStatus);
                    ProductVersion.DateCreated = DateTime.Now;
                    prod.Versions.Add(ProductVersion);
                    db.Products.Add(prod);
                    db.SaveChanges();

                }
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // GET: /Product/Edit/5
        public ActionResult Edit(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            using (var db = new ProductDBContext())
            {
                //populate the archives and versions so the product can find the corresponding versions and archives
                var archives = db.Archives.ToList();
                var allVersions = db.Versions.ToList();
                product = db.Products.Find(id);
                //grab only the visible versions becuase if a version is invisible, then it was removed by the admin, and reverse it to show the most recent uploads first
                product.Versions = GetVisibleVersions(product.Versions.Reverse().ToList());
                foreach (var version in product.Versions)
                {
                    //populate the list.  I found that if I did not do this, the product's versions and archives would be null
                    foreach (var arch in version.Archives)
                    {

                    }
                }

            }
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: /Product/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductId,ProductName,ProductStatus")] Product product)
        {
            if (ModelState.IsValid)
            {
                using (var db = new ProductDBContext())
                {
                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            return View(product);
        }
        //Edits a selected version that is attached to a product
        public ActionResult EditVersion(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.Version version;
            using (var db = new ProductDBContext())
            {
                //populate the archives and version becuase again, I found that If i did not do this, that everything would be null
                var archives = db.Archives.ToList();
                var allVersions = db.Versions.ToList();
                version = db.Versions.Find(id);

                foreach (var arch in version.Archives)
                {

                }


            }
            if (version == null)
            {
                return HttpNotFound();
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
        public ActionResult EditVersion([Bind(Include = "VersionName, VersionId")] Models.Version version, string VStatus)
        {
            if (ModelState.IsValid)
            {
                string versName = version.VersionName;
                Models.Version vers = new Models.Version();
                Archive arch = new Archive();
                using (var db = new ProductDBContext())
                {
                    int ArchIndex = 0;
                    var AllVersions = db.Versions.ToList();
                    var AllArchives = db.Archives.ToList();
                    vers = db.Versions.Find(version.VersionId);
                    foreach (var archive in vers.Archives)
                    {
                        arch = db.Archives.Find(archive.ArchiveId);
                        ArchIndex = archive.ArchiveId;
                    }
                    string CurrArchId = ArchIndex.ToString() + "_";
                    //Add the files to the appropriate folder based on the name, such as '.exe', or 'ReadMe'
                    string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + GetIndex(vers.ProductId).ToString("D8") + "\\" + GetIndex(ArchIndex).ToString("D8") + "\\";
                    foreach (string file in Request.Files)
                    {
                        try
                        {
                            var fileName = Request.Files[file].FileName;
                            if (fileName.Contains(".exe"))
                            {
                                arch.Exe = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                //grab the version out of the .exe file
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                versName = versionInfo.ProductVersion;
                            }
                            else if (fileName.Contains("ReadMe"))
                            {
                                arch.ReadMe = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                //If there is no version try to get it out of the ReadMe
                                if (versName == null)
                                {
                                    versName = versionInfo.ProductVersion;
                                }
                            }
                            else if (fileName.CompareTo("") == 0)
                            {
                                //If the fileName is blank, then no file was selected, and do nothing

                            }
                            else
                            {
                                arch.Installer = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                                fileName = CurrArchId + fileName;
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                //if there still is no version, try to grap it out of the installer
                                if (versName == null)
                                {
                                    versName = versionInfo.ProductVersion;
                                }
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
                return RedirectToAction("Edit/" + vers.ProductId.ToString());
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
        public ActionResult Remove(int? id)
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

        // POST: /Product/Remove/5
        [HttpPost, ActionName("Remove")]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveConfirmed(int id)
        {

            Product product;
            using (var db = new ProductDBContext())
            {
                product = db.Products.Find(id);
                //change the ProductStatus to 0 which means 'not visible'
                product.ProductStatus = 0;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        //Hides a specified version from all users, without actually deleting it
        // GET: /Product/RemoveVersion/5
        public ActionResult RemoveVersion(int? id)
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

        // POST: /Product/RemoveVersion/5
        [HttpPost, ActionName("RemoveVersion")]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveVersionConfirmed(int id)
        {

            Models.Version versions;
            using (var db = new ProductDBContext())
            {
                versions = db.Versions.Find(id);
                //sets the VersionStatus to zero which is invisible to all users
                versions.VersionStatus = 0;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
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
        //Dowload Action which returns the selected file to the user
        public ActionResult Download(string fileName, int id, int archId)
        {

            try
            {
                //append the archive id to the front of the fileName so the file can be found
                fileName = archId.ToString() + "_" + fileName;
                //add the appropriate extensions on to the root path
                string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + GetIndex(id).ToString("D8") + "\\" + GetIndex(archId).ToString("D8") + "\\";
                string FullFilePath = filePath + fileName;
                byte[] fileBytes = System.IO.File.ReadAllBytes(FullFilePath);
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error, could not find file";
                //if there was an error, then redirect to the same page
                return RedirectToAction("Display/" + id.ToString());
            }
        }

        //this method gets the proper index of the folder that the product is in
        //This method also makes it so a new folder is created for every 1000 products

        public int GetIndex(int id)
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
        //This method return a product list of visible products, e.g., if thier
        //Status number is 1
        public List<Product> ShowOnlyVisible(List<Product> products)
        {
            List<Product> visible = new List<Product>();
            foreach (var product in products)
            {
                if (product.ProductStatus > 0)
                {
                    visible.Add(product);
                }
            }
            return visible;
        }
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
    }


}
    

