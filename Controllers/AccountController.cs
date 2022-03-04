using OnlineStoreDataAccess.models;
using System;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using OnlineStore.App_Start;

namespace OnlineStore.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Account
        public ActionResult Login()
        {
                return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(usermodel user)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    string api = ConfigurationManager.AppSettings["WebAPIurl"].ToString();

                    client.BaseAddress = new Uri(@"" + api + @"api/security/login");
                    string apiMethod = "login";
                    var response = client.PostAsync(apiMethod, new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json")).Result;
                    var contents = await response.Content.ReadAsStringAsync();
                    contents = contents.TrimStart('\"');
                    contents = contents.TrimEnd('\"');
                    contents = contents.Replace("\\", "");
                    var userModel = JsonConvert.DeserializeObject<usermodel>(contents);

                    if (userModel != null)
                    {
                        //store user credentials to application cookie
                        var identity = new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Email, userModel.username),
                            new Claim(ClaimTypes.NameIdentifier, userModel.userid.ToString()),
                            new Claim(ClaimTypes.Expiration, DateTime.UtcNow.AddHours(8).ToString()),
                        }, "ApplicationCookie");

                        var ctx = Request.GetOwinContext();

                        var authManager = ctx.Authentication;
                        //authManager.SignIn(identity);
                        authManager.SignIn(new AuthenticationProperties() { IsPersistent = true, ExpiresUtc = DateTime.UtcNow.AddDays(1) }, identity);

                        var claimsPrincipal = new ClaimsPrincipal(identity);
                        // Set current principal
                        Thread.CurrentPrincipal = claimsPrincipal;
                        return Redirect(GetRedirectUrl(string.Empty));
                    }
                    else
                    {
                        // user authN failed
                        ModelState.AddModelError("", "Invalid email or password");
                        return View();
                    }
                }
            }
            return View();
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Register(RegisterViewModel register)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    string api = ConfigurationManager.AppSettings["WebAPIurl"].ToString();

                    client.BaseAddress = new Uri(@"" + api + @"api/security/register");
                    string apiMethod = "register";
                    var playload = new usermodel
                    {
                        username = register.Email,
                        password = register.Password
                    };
                    var response = client.PostAsync(apiMethod, new StringContent(JsonConvert.SerializeObject(playload), Encoding.UTF8, "application/json")).Result;
                    var contents = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        contents = contents.TrimStart('\"');
                        contents = contents.TrimEnd('\"');
                        contents = contents.Replace("\\", "");

                        ViewBag.Result = contents.Replace("\\","");
                    }
                    else
                    {
                        ViewBag.Result = "Registered successfully!!!";
                    }
                }
            }

            return View();
        }

        private string GetRedirectUrl(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return Url.Action("index", "home");
            }

            return returnUrl;
        }

        [AllowAnonymous]
        public ActionResult LogOut()
        {
            Request.GetOwinContext().Authentication.SignOut();
            return RedirectToAction("Login", "Account");
        }
    }
}