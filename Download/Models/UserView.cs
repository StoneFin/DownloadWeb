using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Download.Models;

namespace Download.Models
{
    public class UserViewModel
    {
        public UserViewModel() { roleNames = new List<string>(); }
        public string UserName { get; set; }
        public List<string> roleNames { get; set; }
        public string Email { get; set; }
        public string Id { get; set; }
    }

}