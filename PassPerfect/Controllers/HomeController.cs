using Newtonsoft.Json;
using PassPerfect.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace PassPerfect.Controllers
{
    public class HomeController : Controller
    {
        public string BaseApiUrl { get; set; }

        public HomeController()
        {
            this.BaseApiUrl = ConfigurationManager.AppSettings["BaseApiUrl"];
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> GetAuthToken(string username, string password)
        {
            using(var client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.BaseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("grant_type", "password")
                });
                try
                {
                    var response = await client.PostAsync("/oauth/token", content);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<AuthTokenResponse>(json);
                    
                    SetAccessToken(data.AccessToken);
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                catch
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPGPMessage(string username)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.BaseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken());
                try
                {
                    var response = await client.GetAsync(string.Format("/api/accounts/onetimepassword/{0}", username));
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();
                    return Json(new { ciphertext = data }, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(new { ciphertext = string.Empty }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpGet]
        public async Task<JsonResult> Login(string username, string password)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.BaseApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken());

                var model = new LoginModel
                {
                    UserName = username,
                    Password = password
                };

                try
                {
                    var response = await client.PostAsJsonAsync("/api/accounts/login", model);
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();
                    return Json(new { url = "Home/Welcome" }, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(new { url = "Home/Error" }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpGet]
        public ActionResult Welcome()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Error()
        {
            return View();
        }

        private void SetAccessToken(string accessToken)
        {
            var cookie = new HttpCookie("AccessToken");
            cookie.Value = accessToken;
            cookie.Expires = DateTime.Now.AddMinutes(10);
            Response.Cookies.Add(cookie);
        }

        private string GetAccessToken()
        {
            var value = string.Empty;
            if(Request == null)
            {
                return value;
            }
            if(Request.Cookies["AccessToken"] != null)
            {
                value = Request.Cookies["AccessToken"].Value;
            }
            return value;
        }
    }
}