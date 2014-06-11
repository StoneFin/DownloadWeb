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

namespace Download.Controllers
{
    [Authorize(Roles = "admin")]
    public class ProductController : Controller
    {
        
        [AllowAnonymous]
        // GET: /Product/
        public ActionResult Index(string searchString, int? page)
        {
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

                if (searchString != null)
                {
                    //if the search string is not empty, then only return the matching products to the user
                    Sproducts = Sproducts.Where(s => s.ProductName.Contains(searchString));
                    products = Sproducts.ToList();
                    page = 1;
                }
                else if(searchString == "")
                {
                    products = db.Products.ToList();
                }
                else
                {
                    //do nothing and return no products
                }
                //If there are no products in the database, display a blank Index
              if (products.Count() == 0)
               {

                   return View(products.ToPagedList(pageNumber, pageSize));
               }




            }

            return View(products.ToPagedList(pageNumber, pageSize));
        }

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
                //populate archives so the product can find the corresponding archives
                var archives = db.Archives.ToList();
                ProductModel.ProductName = product.ProductName;
                ProductModel.Id = product.ProductId;
                //populate versions so the product can find the corresponding versions
                var Versions = db.Versions.ToList();
                
                ProductModel.Versions = product.Versions.ToList();
                //Reverse the list so the most recent additions are at the lowest indicies 
                ProductModel.Versions.Reverse();
                //initiallize the markdown converter
                var md = new MarkdownDeep.Markdown();
                md.SafeMode = true;
                md.ExtraMode = true;
                //If another version was selected from the display view, display that particular version
                string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + "(" + GetIndex(product.ProductId).ToString() +")\\" + product.ProductName.ToString() + "\\";
                if (VersionId != null)
                {
                    foreach (var version in ProductModel.Versions)
                    {
                        if (version.VersionId == VersionId)
                        {
                            //Remove the version and insert it at the beginning so it is displayed on the page
                            ProductModel.Versions.Remove(version);
                            ProductModel.Versions.Insert(0, version);
                            foreach (var arch in version.Archives)
                            {
                                //grab the root path from web.config and append the specific extension to it

                                string fileName = filePath + arch.ReadMe.ToString();
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
                                string fileName = filePath + arch.ReadMe.ToString();
                                try
                                {
                                    var file = System.IO.File.ReadAllText(fileName);
                                    ViewData["Content"] = md.Transform(file);
                                }
                                catch (Exception ex)
                                {
                                    ViewData["Content"] = md.Transform("Could not find ReadMe");
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
            AddVersionView version = new AddVersionView();
            Product product = new Product();
            using(var db = new ProductDBContext()){
                product = db.Products.Find(id);
            }
            version.ProductName = product.ProductName;
            version.ProductId = product.ProductId;
            return View(version);
        }
        [HttpPost]
        public ActionResult AddVersion(AddVersionView version)
        {
            if(ModelState.IsValid){
            Product prod = new Product();
            Models.Version ProductVersion = new Models.Version();
            Archive ProductArchive = new Archive();
            using (var db = new ProductDBContext())
            {
                prod = db.Products.Find(version.ProductId);
                //Add the files to the appropriate folder based on the name, such as '.exe', or 'ReadMe'
                string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + "(" + GetIndex(prod.ProductId).ToString()+ ")\\" + prod.ProductName.ToString();
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
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                //grap the version out of the .exe file
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
                                var path = Path.Combine(filePath, fileName);
                                Request.Files[file].SaveAs(path);
                                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                                //If there is no version try to get it out of the ReadMe
                                if (ProductVersion.VersionName == null)
                                {
                                    ProductVersion.VersionName = versionInfo.ProductVersion;
                                }
                            }
                            else if(fileName.CompareTo("") == 0){
                                //If the fileName is blank, then no file was selected, and do nothing

                            }
                            else
                            {
                                ProductArchive.Installer = fileName;
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
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
            return View();
        }

        // POST: /Product/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductId,ProductName")] ProductCreateView product)
        {
           //refer to the comments of AddVersion to see how most of this works
            if (ModelState.IsValid)
            {
                using (var db = new ProductDBContext())
                {
                    Product prod = new Product();
                    prod.ProductName = product.ProductName;
                    var LastProduct = db.Products.ToList().Last();
                    Models.Archive ProductArchive = new Models.Archive();
                    Models.Version ProductVersion = new Models.Version();


                    string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + "(" + GetIndex(LastProduct.ProductId + 1).ToString() + ")\\" + product.ProductName.ToString();
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
            using (var db = new ProductDBContext()){
                var archives = db.Archives.ToList();
                var allVersions = db.Versions.ToList();
               product = db.Products.Find(id);
               foreach (var version in product.Versions)
               {
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
        public ActionResult Edit([Bind(Include="ProductId,ProductName")] Product product)
        {
            if (ModelState.IsValid)
            {
                using (var db = new ProductDBContext()){
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
            return View(version);
        }

        // POST: /Product/EditVersion/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditVersion([Bind(Include = "VersionName, VersionId")] Models.Version version)
        {
            if (ModelState.IsValid)
            {
                string versName = version.VersionName;
                Models.Version vers = new Models.Version();
                Archive arch = new Archive();
                using (var db = new ProductDBContext())
                {
                    var AllVersions = db.Versions.ToList();
                    var AllArchives = db.Archives.ToList();
                    vers = db.Versions.Find(version.VersionId);
                    foreach (var archive in vers.Archives)
                    {
                        arch = db.Archives.Find(archive.ArchiveId);
                    }
                    //Add the files to the appropriate folder based on the name, such as '.exe', or 'ReadMe'
                    string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + "(" + GetIndex(vers.ProductId).ToString() + ")\\" + vers.Product.ProductName.ToString();
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
                    db.SaveChanges();

                }
                return RedirectToAction("Edit/" + vers.ProductId.ToString());
            }
                return View(version);
            
        }
        

        // GET: /Product/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product;
            using (var db = new ProductDBContext()){
            product = db.Products.Find(id);
            }
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: /Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product;
            using (var db = new ProductDBContext()){
            product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                using (var db = new ProductDBContext()){
                db.Dispose();
                }
            }
            base.Dispose(disposing);
        }
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
        public ActionResult Download(string fileName, int id, string extension)
        {
            try
            {
                string filePath = System.Configuration.ConfigurationManager.AppSettings["Path"].ToString() + "(" + GetIndex(id).ToString() + ")\\";
                string FullFilePath = filePath + extension + fileName;
                byte[] fileBytes = System.IO.File.ReadAllBytes(FullFilePath);
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Message = "ERROR " + ex.ToString();
                return RedirectToAction("Display/" + id.ToString());
            }
        }
        //this method gets the proper index of the folder that the product is in
        //This method also makes it so a new folder is created for every 1000 products

        public int GetIndex(int id)
        {
            return (int)id / 1000;
        }
           
            }

        }
    

