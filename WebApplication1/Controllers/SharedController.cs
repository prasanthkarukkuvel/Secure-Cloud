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
    public class SharedController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Shared
        public async Task<ActionResult> Index()
        {
            var ID = User.Identity.GetUserId();
            var FileStores = await db.UserFileStores.Where(x => x.User.Id == ID).Select(x => x.FileStore.ID).ToListAsync();
            var Shared = await db.ShareFile.Include(x => x.FileStore).Include(x => x.User).Where(x => FileStores.Contains(x.FileStore.ID)).ToListAsync();

            return View(Shared);
        }

        public async Task<ActionResult> Revoke(string id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var Shared = await db.ShareFile.FindAsync(id);

            db.ShareFile.Remove(Shared);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");

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