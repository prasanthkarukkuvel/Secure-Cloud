using Dropbox.Api;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Drive.v2;
using Google.Apis.Json;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNet.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;


namespace SecureCloud.Models
{

   
    public class AppFileDataStore2 : IDataStore
    {
        public Task StoreAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key 1 MUSTS have a value");
            }

            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                db.TokenStores.Add(new TokenStore());
                db.SaveChanges();
            }

            return TaskEx.Delay(0);
        }
        public Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key 2 MUSTS have a value");
            }

            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var token = db.TokenStores.Find(GenerateStoredKey(key, typeof(T)));

                if (token != null)
                {
                    db.TokenStores.Remove(token);

                    db.SaveChanges();
                }
            }

            return TaskEx.Delay(0);
        }
        public Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key 3 MUSTS have a value");
            }

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var token = db.TokenStores.Find(GenerateStoredKey(key, typeof(T)));

                if (token != null)
                {
                    try
                    {
                        tcs.SetResult(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(token.Token));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
                else
                {
                    tcs.SetResult(default(T));
                }
            }
            return tcs.Task;
        }
        public Task ClearAsync()
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                var tokens = db.TokenStores.ToList();

                foreach (var token in tokens)
                {
                    db.TokenStores.Remove(token);
                }

                db.SaveChanges();
            }

            return TaskEx.Delay(0);
        }
        public static string GenerateStoredKey(string key, Type t)
        {
            var a = string.Format("{0}-{1}", t.FullName, key);
            System.Diagnostics.Debug.WriteLine(a);
            return a;
        }
    }

    public class AppDataFileStore : IDataStore
    {
        readonly string folderPath;
        public string FolderPath { get { return folderPath; } }
        public AppDataFileStore(string folder)
        {
            folderPath = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data/"), folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
        public Task StoreAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            var serialized = NewtonsoftJsonSerializer.Instance.Serialize(value);
            var filePath = Path.Combine(folderPath, GenerateStoredKey(key, typeof(T)));

            System.Diagnostics.Debug.WriteLine("RIE " + serialized);

            File.WriteAllText(filePath, serialized);
            return TaskEx.Delay(0);
        }
        public Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            var filePath = Path.Combine(folderPath, GenerateStoredKey(key, typeof(T)));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return TaskEx.Delay(0);
        }
        public Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            var filePath = Path.Combine(folderPath, GenerateStoredKey(key, typeof(T)));

            System.Diagnostics.Debug.WriteLine("GIE " + filePath);

            if (File.Exists(filePath))
            {
                try
                {
                    var obj = File.ReadAllText(filePath);
                    tcs.SetResult(NewtonsoftJsonSerializer.Instance.Deserialize<T>(obj));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            else
            {
                tcs.SetResult(default(T));
            }
            return tcs.Task;
        }
        public Task ClearAsync()
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Directory.CreateDirectory(folderPath);
            }

            return TaskEx.Delay(0);
        }
        public static string GenerateStoredKey(string key, Type t)
        {
            return string.Format("{0}-{1}", t.FullName, key);
        }
    }

    public class AppFlowMetaData : FlowMetadata
    {
        private readonly IAuthorizationCodeFlow flow = null;
        public AppFlowMetaData()
        {
            flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = "59505721040-3aul7o8mp6fsnetfv2s53e4spuna6l4q.apps.googleusercontent.com",
                    ClientSecret = "Mqryn9XfAK_29P5Vbc1_eXWI"
                },
                Scopes = new[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore("DataStore")
            });
        }
        public override string GetUserId(System.Web.Mvc.Controller controller)
        {
            var a = controller.User.Identity.GetUserId();
            System.Diagnostics.Debug.WriteLine(a);
            return a;
        }
        public override IAuthorizationCodeFlow Flow
        {
            get { return flow; }
        }
    }

    public class DropBox
    {
        private const string AppKey = "bg62wcr3o1xvxun";
        private const string AppSecret = "fek37p8pnj8qqjy";
        private const string Redirect = "http://localhost:56698/AuthCallback/Redirect";

        public static DropboxClient Get(string AccessToken)
        {
            return new DropboxClient(AccessToken);
        }
        public async static Task<Dropbox.Api.Users.FullAccount> Info(string Token)
        {
            using (var dbx = Get(Token))
            {
                return await dbx.Users.GetCurrentAccountAsync();
            }
        }
        public async static Task<bool> Upload(string Token, string Filename, byte[] Content)
        {
            Dropbox.Api.Files.FileMetadata Updated = null;

            using (var dbx = Get(Token))
            {
                using (var mem = new MemoryStream(Content))
                {
                    Updated = await dbx.Files.UploadAsync("/SecureCloud/" + Filename + ".dat", Dropbox.Api.Files.WriteMode.Overwrite.Instance, body: mem);
                }
            }

            return Updated != null;
        }
        public static async Task<bool> Delete(string Token, string Filename)
        {
            Dropbox.Api.Files.Metadata Deleted = null;

            using (var dbx = Get(Token))
            {
                Deleted = await dbx.Files.DeleteAsync("/SecureCloud/" + Filename + ".dat");
            }

            return Deleted != null;
        }
        public static async Task<byte[]> Download(string Token, string Filename)
        {
            using (var dbx = Get(Token))
            {
                var Downloaded = await dbx.Files.DownloadAsync("/SecureCloud/" + Filename + ".dat");

                if (Downloaded != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        (await Downloaded.GetContentAsStreamAsync()).CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }

            return null;
        }
        public static async Task<OAuth2Response> Handle(string Code, string State)
        {
            return await DropboxOAuth2Helper.ProcessCodeFlowAsync(Code, AppKey, AppSecret, Redirect);
        }
        public static string Connect()
        {
            return DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, AppKey, Redirect, Guid.NewGuid().ToString("N")).ToString();
        }
    }

    public class Drive
    {
        static String APP_USER_AGENT = "Secure Cloud";
        private static async Task<BaseClientService.Initializer> GetCredentials(System.Web.Mvc.Controller Controller, HttpServerUtilityBase Server)
        {
            var result = await new AuthorizationCodeMvcApp(Controller, new AppFlowMetaData()).AuthorizeAsync(CancellationToken.None);

            if (result.Credential != null)
            {

                BaseClientService.Initializer initializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = result.Credential,
                    ApplicationName = APP_USER_AGENT,

                };

                return initializer;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(result.RedirectUri);
            }

            return null;
        }
        public static async Task<DriveService> BuildService(System.Web.Mvc.Controller Controller, HttpServerUtilityBase Server)
        {
            return new DriveService(await GetCredentials(Controller, Server));
        }
        private static String getUserId(IUser User)
        {
            return User.Id;
        }
        private static IDataStore getPersistentCredentialStore(HttpServerUtilityBase Server)
        {
            return new FileDataStore(Server.MapPath("DataStore"));
        }
    }
}