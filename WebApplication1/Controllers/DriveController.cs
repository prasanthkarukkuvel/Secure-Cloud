using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SecureCloud.Models;
using System.IO;
using Microsoft.AspNet.Identity;

namespace SecureCloud.Controllers
{
    [Authorize]
    public class DriveController : Controller
    {
        // GET: Drive

        private ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var ID = User.Identity.GetUserId();

            var Token = await db.TokenStores.FindAsync(ID);

            if (Token == null)
            {
                return RedirectToAction("Connect", "Files");
            }
            
            return View(await DropBox.Info(Token.Token));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}