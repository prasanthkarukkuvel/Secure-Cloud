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
using System.Text;

namespace SecureCloud.Controllers
{
    [Authorize]
    public class IncomingController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Incoming
        public async Task<ActionResult> Index()
        {
            var ID = User.Identity.GetUserId();

            var ShareViewModels = await (from Shared in db.ShareFile
                                         join UserStore in db.UserFileStores on Shared.FileStore.ID equals UserStore.FileStore.ID
                                         where Shared.User.Id == ID
                                         select new SharedUserStoreViewModel { UserFileStore = UserStore, ShareFile = Shared }).ToListAsync();

            var ShareList = ShareViewModels.Select(x => x.UserFileStore.ID).ToList();
            var UserStores = await db.UserFileStores.Include(x => x.FileStore).Include(x => x.User).Where(x => ShareList.Contains(x.ID)).ToListAsync();

            foreach (var item in ShareViewModels)
            {
                item.UserFileStore = UserStores.Where(x => x.ID == item.UserFileStore.ID).FirstOrDefault();
            }

            return View(ShareViewModels.OrderByDescending(x=>x.ShareFile.CreatedAt));
        }

        public async Task<ActionResult> Download(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ID = User.Identity.GetUserId();

            var ShareFile = await db.ShareFile.Include(x => x.FileStore).Where(x => x.ID == id).FirstOrDefaultAsync();

            if (ShareFile == null)
            {
                return HttpNotFound();
            }

            var UserStore = await db.UserFileStores.Include(x => x.User).Where(x => x.FileStore.ID == ShareFile.FileStore.ID).FirstOrDefaultAsync();

            if (UserStore == null)
            {
                return HttpNotFound();
            }

            var TokenStore = await db.TokenStores.FindAsync(UserStore.User.Id);

            if (TokenStore == null)
            {
                return HttpNotFound();
            }

            var FileData = await DropBox.Download(TokenStore.Token, UserStore.ID);

            if (FileData == null)
            {
                return HttpNotFound();
            }

            var Content = new System.Net.Mime.ContentDisposition
            {
                FileName = System.Text.RegularExpressions.Regex.Replace(UserStore.FileStore.Name, @"\s+", "_"),
                Inline = false,
            };

            var KeyPairs = await db.UserKeyPairStores.Where(x => x.ID == UserStore.User.Id || x.ID == ID).ToListAsync();

            if (KeyPairs.Count() > 1)
            {              
                var UserKeyPair = KeyPairs.Where(x => x.ID == ID).FirstOrDefault();

                if (UserKeyPair == null)
                {
                    return HttpNotFound();
                }
                else
                {
                    var ShareKeyPair = KeyPairs.Where(x => x.ID == UserStore.User.Id).FirstOrDefault();                    

                    Response.AppendHeader("Content-Disposition", Content.ToString());
                    return File(Crypto.AES_Decrypt(FileData, Crypto.RSA_Decrypt(UserKeyPair.PrivateKey, Convert.FromBase64String(ShareFile.Key))), ShareFile.FileStore.Type);
                }
            }
            return HttpNotFound();
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