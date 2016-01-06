using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SecureCloud.Models;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;


namespace SecureCloud.Controllers
{   
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }       

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}