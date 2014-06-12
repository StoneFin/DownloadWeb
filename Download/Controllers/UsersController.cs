using Download.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
namespace Download.Controllers
{
    [Authorize(Roles = "admin")]
    public class UsersController : Controller
    {
        //Shows all the users on a page
        // GET: /Users/
        public ActionResult Index()
        {
            //add paging to the user index 
            List<UserViewModel> AllUsers = new List<UserViewModel>();
            
            List<ApplicationUser> users;
            using (var db = new ApplicationDbContext())
            {
                users = db.Users.ToList();

                foreach (var User in users)
                {
                    UserViewModel UserModel = new UserViewModel();
                    UserModel.UserName = User.UserName;
                    UserModel.Email = User.Email;
                    UserModel.Id = User.Id;
                    foreach (var role in User.Roles)
                    {
                        UserModel.roleNames.Add(role.Role.Name);
                    }
                    AllUsers.Add(UserModel);
                }

            }
            
            return View(AllUsers);
        }

        // GET: /Product/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser User;
            using (var db = new ApplicationDbContext()){
            User = db.Users.Find(id);
            }
            if (User == null)
            {
                return HttpNotFound();
            }
            return View(User);
        }

        // GET: /Product/Create
        public ActionResult Create()
        {
            return View();
        }
        //NOT USED because creating a user is as easy as registering
        // POST: /Product/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserName,Roles,Email,ID")] ApplicationUser User, string roleName)
        {


            if (ModelState.IsValid)
            {
                using (var db = new ApplicationDbContext()){
            var userStore = new UserStore<ApplicationUser>(db);
            var userManager = new UserManager<ApplicationUser>(userStore);
            var UserRole = User;
                db.Users.Add(User);
                if (userManager.IsInRole(UserRole.Id, roleName) == false)
                {
                    userManager.AddToRole(UserRole.Id, roleName);
                }
                db.SaveChanges();
                }
                return RedirectToAction("Index");
            }

            return View(User);
        }

        // GET: /Product/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser User;
            List<IdentityRole> roles;
            using (var db = new ApplicationDbContext()){
            User = db.Users.Find(id);

            if (User == null)
            {
                return HttpNotFound();
            }
                //find all roles to be used in the drop down list in the view
            roles = db.Roles.ToList();
                //find the current role of the selected use, put it at the beginning of the list so that
                //the current role is the default selection of the dropdown lsit
            foreach (var role in User.Roles)
            {
                roles.Remove(role.Role);
                roles.Insert(0, role.Role);
            }
            }
            ViewData["AvailableRoles"] = roles;
            

            return View(User);
        }

        // POST: /Product/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserName, ID, Email")] ApplicationUser User, string RoleName)
        {



            //var authenticationManager = HttpContext.GetOwinContext().Authentication;

            if (ModelState.IsValid)
            {
                using (var db = new ApplicationDbContext())
                {
                    //Use Microsoft's User manager to handle the roles
                    var userStore = new UserStore<ApplicationUser>(db);
                    var userManager = new UserManager<ApplicationUser>(userStore);
                    var dbUser = db.Users.First(u => u.Id == User.Id);

                    dbUser.Email = User.Email;
                    dbUser.UserName = User.UserName;
                    //If the user somehow has no roles, assign the seleted one and be done
                    if (dbUser.Roles.Count() == 0)
                    {
                        userManager.AddToRole(dbUser.Id, RoleName);
                    }
                        //otherwise, in order to keep each user to only one role, remove all other roles, and add only the selected role
                    else
                    {
                        foreach (var role in dbUser.Roles)
                        {
                            if (role.Role.Name != RoleName)
                            {
                                userManager.RemoveFromRole(dbUser.Id, role.Role.Name);
                                userManager.AddToRole(dbUser.Id, RoleName);
                                break;
                            }
                            else
                            {

                            }
                        }
                    }
                     db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            return View(User);
        }
        //Used to Perminately delete a user
        //// GET: /Product/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser User;
            using (var db = new ApplicationDbContext()){
            User = db.Users.Find(id);
            }
            if (User == null)
            {
                return HttpNotFound();
            }
            return View(User);
        }

        // POST: /Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            using (var db = new ApplicationDbContext()){
            ApplicationUser User = db.Users.Find(id);
            db.Users.Remove(User);
            db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        //This was a default method that was created when I made the controller, I'm not sure what it does
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                using (var db = new ApplicationDbContext()){
                db.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        //This Action handles the MakeMember button
        public ActionResult MakeMember(string id)
        {
            using (var db = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(db);
                var userManager = new UserManager<ApplicationUser>(userStore);
                var dbUser = db.Users.First(u => u.Id == id);


                if (dbUser.Roles.Count() == 0)
                {
                    userManager.AddToRole(dbUser.Id, "member");
                }
                else
                {
                    foreach (var role in dbUser.Roles)
                    {
                        if (role.Role.Name != "member")
                        {
                            userManager.RemoveFromRole(dbUser.Id, role.Role.Name);
                            userManager.AddToRole(dbUser.Id, "member");
                            break;
                        }
                        else
                        {

                        }
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        //This action handles the MakeAdmin button
        public ActionResult MakeAdmin(string id)
        {
            using (var db = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(db);
                var userManager = new UserManager<ApplicationUser>(userStore);
                var dbUser = db.Users.First(u => u.Id == id);


                if (dbUser.Roles.Count() == 0)
                {
                    userManager.AddToRole(dbUser.Id, "admin");
                }
                else
                {
                    foreach (var role in dbUser.Roles)
                    {
                        if (role.Role.Name != "admin")
                        {
                            userManager.RemoveFromRole(dbUser.Id, role.Role.Name);
                            userManager.AddToRole(dbUser.Id, "admin");
                            break;
                        }
                        else
                        {

                        }
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        //This action demotes the user to a non-validated member, essentially deactivating thier account becuase non-vailidated members
        //have the same permmissions and annonomous users
        public ActionResult Deactivate(string id)
        {
            using (var db = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(db);
                var userManager = new UserManager<ApplicationUser>(userStore);
                var dbUser = db.Users.First(u => u.Id == id);


                if (dbUser.Roles.Count() == 0)
                {
                    userManager.AddToRole(dbUser.Id, "non-validated member");
                }
                else
                {
                    foreach (var role in dbUser.Roles)
                    {
                        if (role.Role.Name != "non-validated member")
                        {
                            userManager.RemoveFromRole(dbUser.Id, role.Role.Name);
                            userManager.AddToRole(dbUser.Id, "non-validated member");
                            break;
                        }
                        else
                        {

                        }
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
