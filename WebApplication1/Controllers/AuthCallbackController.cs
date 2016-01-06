using Dropbox.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SecureCloud.Models;
using Microsoft.AspNet.Identity;

namespace SecureCloud.Controllers
{
    [Authorize]
    public class AuthCallbackController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Redirect(string code, string state)
        {
            var Response = await DropBox.Handle(code, state);

            db.TokenStores.Add(new TokenStore
            {
                ID = User.Identity.GetUserId(),
                Token = Response.AccessToken,
                UId = Response.Uid
            });

            await db.SaveChangesAsync();

            return RedirectToAction("Index", "Files");
        }

        public ActionResult Connect()
        {
            return Redirect(DropBox.Connect());
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