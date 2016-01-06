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
    public class FilesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Files
        public async Task<ActionResult> Index()
        {
            var Token = await db.TokenStores.FindAsync(User.Identity.GetUserId());

            if (Token == null)
            {
                return RedirectToAction("Connect");
            }

            var ID = User.Identity.GetUserId();

            return View((await db.UserFileStores.Include(x => x.FileStore).Where(x => x.User.Id == ID).ToListAsync()).OrderByDescending(x=>x.FileStore.CreatedAt));
        }

        public ActionResult Connect()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Upload(IEnumerable<HttpPostedFileBase> Files)
        {
            var file = Files.FirstOrDefault();

            try
            {
                var TokenStore = await db.TokenStores.FindAsync(User.Identity.GetUserId());

                if (TokenStore == null)
                {
                    throw new Exception("AccessTokenNotFound");
                }

                var ID = User.Identity.GetUserId();
                var Key = KeyGenerator.CreateKey();
                var ApplicationUser = await db.Users.Where(x => x.Id == ID).FirstOrDefaultAsync();

                if (ApplicationUser != null)
                {
                    var FileStore = new FileStore
                    {
                        ID = Guid.NewGuid().ToString(),
                        Name = file.FileName,
                        Size = file.ContentLength,
                        Type = file.ContentType,
                        CreatedAt = DateTime.Now
                    };

                    var UserFileStore = new UserFileStore
                    {
                        ID = Guid.NewGuid().ToString(),
                        FileStore = FileStore,
                        Key = Key,
                        User = ApplicationUser
                    };

                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        if (await DropBox.Upload(TokenStore.Token, UserFileStore.ID, Crypto.AES_Encrypt(binaryReader.ReadBytes(Request.Files[0].ContentLength), Crypto.GetBytes(Key))))
                        {
                            db.FileStores.Add(FileStore);
                            db.UserFileStores.Add(UserFileStore);
                            await db.SaveChangesAsync();

                            return Json(new { name = file.FileName });
                        }
                    }
                }
                else
                {
                    return Json(new { status = "NotFound", name = file.FileName });
                }


            }
            catch (Exception ex)
            {
                return Json(new { status = ex.Message, name = file.FileName });
            }

            return Json(new { status = "ProcessFailed", name = file.FileName });
        }

        // GET: Files/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ID = User.Identity.GetUserId();

            UserFileStore userFileStore = await db.UserFileStores.Include(x => x.FileStore).Where(x => x.ID == id && x.User.Id == ID).FirstOrDefaultAsync();
            if (userFileStore == null)
            {
                return HttpNotFound();
            }
            return View(userFileStore);
        }

        // GET: Files/Details/5
        public async Task<ActionResult> Share(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ID = User.Identity.GetUserId();

            UserFileStore userFileStore = await db.UserFileStores.Include(x => x.FileStore).Where(x => x.ID == id && x.User.Id == ID).FirstOrDefaultAsync();
            if (userFileStore == null)
            {
                return HttpNotFound();
            }

            return View(new ShareUserFileViewModel
            {
                ID = userFileStore.ID,
                Email = "",
                UserFileStore = userFileStore
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Share([Bind(Include = "")] ShareUserFileViewModel shareUserFileViewModel)
        {
            var ID = User.Identity.GetUserId();

            UserFileStore userFileStore = await db.UserFileStores.Include(x => x.FileStore).Where(x => x.ID == shareUserFileViewModel.ID && x.User.Id == ID).FirstOrDefaultAsync();
            if (userFileStore == null)
            {
                return HttpNotFound();
            }

            try
            {

                if (ModelState.IsValid)
                {
                    var ApplicationUser = await db.Users.Where(x => x.Email == shareUserFileViewModel.Email).FirstOrDefaultAsync();

                    if (ApplicationUser == null)
                    {
                        ModelState.AddModelError("Email", "Specified user not found.");
                    }
                    else
                    {
                        if (ApplicationUser.Id != ID)
                        {
                            var KeyPairs = await db.UserKeyPairStores.Where(x => x.ID == ApplicationUser.Id || x.ID == ID).ToListAsync();

                            if (KeyPairs.Count() > 1)
                            {

                                var ShareKeyPair = KeyPairs.Where(x => x.ID == ApplicationUser.Id).FirstOrDefault();
                                var FileStore = await db.FileStores.FindAsync(userFileStore.FileStore.ID);                               

                                db.ShareFile.Add(new ShareFile
                                {
                                    ID = Guid.NewGuid().ToString(),
                                    FileStore = FileStore,
                                    Key = Convert.ToBase64String(Crypto.RSA_Encrypt(ShareKeyPair.PublicKey, Crypto.GetBytes(userFileStore.Key))),
                                    User = ApplicationUser,
                                    CreatedAt = DateTime.Now
                                });

                                await db.SaveChangesAsync();

                                return RedirectToAction("Index");

                            }
                            else
                            {
                                ModelState.AddModelError("Email", "Security key pairs not found.");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("Email", "Cannot share the file to the same user.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Email", ex.Message);
            }

            return View(new ShareUserFileViewModel
            {
                ID = userFileStore.ID,
                Email = shareUserFileViewModel.Email,
                UserFileStore = userFileStore
            });
        }

        // GET: Files/Details/5
        public async Task<ActionResult> Download(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ID = User.Identity.GetUserId();

            UserFileStore userFileStore = await db.UserFileStores.Include(x => x.FileStore).Where(x => x.ID == id && x.User.Id == ID).FirstOrDefaultAsync();
            if (userFileStore == null)
            {
                return HttpNotFound();
            }

            var TokenStore = await db.TokenStores.FindAsync(User.Identity.GetUserId());

            if (TokenStore == null)
            {
                return HttpNotFound();
            }

            var FileData = await DropBox.Download(TokenStore.Token, userFileStore.ID);

            if (FileData == null)
            {
                return HttpNotFound();
            }

            var Content = new System.Net.Mime.ContentDisposition
            {
                FileName = System.Text.RegularExpressions.Regex.Replace(userFileStore.FileStore.Name, @"\s+", "_"),
                Inline = false,
            };

            Response.AppendHeader("Content-Disposition", Content.ToString());
            return File(Crypto.AES_Decrypt(FileData, Encoding.UTF8.GetBytes(userFileStore.Key)), userFileStore.FileStore.Type);
        }

        // GET: Files/Create
        public async Task<ActionResult> Create()
        {
            var Token = await db.TokenStores.FindAsync(User.Identity.GetUserId());

            if (Token == null)
            {
                return RedirectToAction("Connect");
            }

            return View();
        }


        // GET: Files/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ID = User.Identity.GetUserId();

            UserFileStore userFileStore = await db.UserFileStores.Include(x => x.FileStore).Where(x => x.ID == id && x.User.Id == ID).FirstOrDefaultAsync();
            if (userFileStore == null)
            {
                return HttpNotFound();
            }
            return View(userFileStore);
        }

        // POST: Files/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            var TokenStore = await db.TokenStores.FindAsync(User.Identity.GetUserId());

            if (TokenStore == null)
            {
                return HttpNotFound();
            }

            var ID = User.Identity.GetUserId();

            UserFileStore userFileStore = await db.UserFileStores.Include(x => x.FileStore).Where(x => x.ID == id && x.User.Id == ID).FirstOrDefaultAsync();

            if (await DropBox.Delete(TokenStore.Token, userFileStore.ID))
            {
                db.FileStores.Remove(userFileStore.FileStore);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
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
