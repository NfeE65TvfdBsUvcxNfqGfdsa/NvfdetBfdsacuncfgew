using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using dotNetRoles = System.Web.Security.Roles;
using System.Web.Routing;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;

using HOHO18.Common;
using MorSun.Model;
using HOHO18.Common.Web;
using MorSun.Bll;
using MorSun.Common.类别;
using HOHO18.Common.WEB;
using HOHO18.Common.SSO;
using MorSun.Common.配置;

namespace MorSun.Controllers
{
    [Authorize]
    [HandleError]
    //[InitializeSimpleMembership]
    public class AccountController : BasisController
    {
        //图片获取测试 已经测试成功，留着，以后用。
        //[AllowAnonymous]
        //public void GetImg()
        //{
        //    //图片已经可以拿下来，文件格式什么的无所谓，跟MediaId关联即可。
        //    string url = "http://mmbiz.qpic.cn/mmbiz/fmwArEibATSJrvPO8NjAdE11a8hoXP94sTNGazUSZkMvA6JfnDclOFVQicrGibnnicpuYrl5r8vu8UicibyTRRicJ7Srw/0";
        //    string filepath = "c:\\pic123421";
        //    WebClient mywebclient = new WebClient();
        //    mywebclient.DownloadFile(url, filepath);           
        //}

        #region 基本方法
        public IFormsAuthenticationService FormsService { get; set; }
        public IMembershipService MembershipService { get; set; }

        protected override void Initialize(RequestContext requestContext)
        {
            if (FormsService == null) { FormsService = new FormsAuthenticationService(); }
            if (MembershipService == null) { MembershipService = new AccountMembershipService(); }

            base.Initialize(requestContext);
        }
        
        /// <summary>
        /// 用户锁定验证
        /// </summary>
        /// <param name="model"></param>
        private void validateLockedUser(LoginModel model)
        {
            var user = Membership.GetUser(model.UserName);

            if (user != null)
            {
                if ("UnlockingFlag".GX() == "true")
                {
                    var lockedDate = user.LastLockoutDate;
                    var days = "UnlockingDay".GX();
                    if (NumHelp.IsNum(days))
                    {
                        lockedDate = lockedDate.AddDays(double.Parse(days));
                    }
                    if (DateTime.Now > lockedDate && user.IsLockedOut)
                    {
                        user.UnlockUser();
                    }
                    else if (DateTime.Now <= lockedDate && user.IsLockedOut)
                    {
                        "UserName".AE("用户已被锁定，" + days + "天后自动解锁或请联系管理员解锁", ModelState);
                    }
                }

                if (user.IsLockedOut)
                {
                    "UserName".AE("用户已被锁定，请联系管理员解锁", ModelState);
                }
            }
            else if (user == null)
            {
                "UserName".AE("提供的用户名或密码不正确", ModelState);
            }
        }
        #endregion

        #region 登录
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.OpenVerificationCode = "VerificationCode".GX().ToAs<bool>();
            return View();
        }

        [AllowAnonymous]
        public ActionResult AjaxLogin(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.OpenVerificationCode = "VerificationCode".GX().ToAs<bool>();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxLogin(LoginModel model, string returnUrl)
        {
            System.Web.HttpContext.Current.Session.Abandon();
            if (Request.IsAuthenticated)
                FormsService.SignOut();
            var oper = new OperationResult(OperationResultType.Error, "登录失败");
            validateVerifyCode(model.Verifycode, model.VerifycodeRandom, "LoginVerificationCode");
            validateLockedUser(model);
            //SSO也要用到，所以从if(AccountActive)里取出来。
            var user = new BaseBll<aspnet_Users>().All.FirstOrDefault(p => p.LoweredUserName == model.UserName.ToLower());
            //判断账号是否激活,时间长未激活的话，系统再发送激活邮件
            if ("AccountActive".GX() == "true")
            {                
                if (user != null && user.wmfUserInfo.FlagActive == false)
                { 
                    //检查该用户的激活邮件发送时间，如果超过48小时，系统自动重新发送一条。
                    var mrbll = new BaseBll<wmfMailRecord>();
                    var effectiveHour = 0 - CFG.有效时间.ToAs<int>();
                    var dt = DateTime.Now.AddHours(effectiveHour);//计算48前的时间
                    var mr = mrbll.All.FirstOrDefault(p => p.MailTo == user.UserName && p.RegTime >= dt);
                    if(mr == null)
                    {
                        SendActiveMail(mrbll,user.aspnet_Membership.Email,user.wmfUserInfo.NickName,user.wmfUserInfo.UserNameString);
                        "UserName".AE("系统已经重新发送了激活邮件，请激活后再登录", ModelState);
                    }
                    else
                    {
                        "UserName".AE("账号未激活,请激活后再登录", ModelState);
                    }
                }
            }
            if (ModelState.IsValid)
            {                
                if (MembershipService.ValidateUser(model.UserName, model.Password))
                {
                    FormsService.SignIn(model.UserName, model.RememberMe);
                    if(!model.UserName.Contains("youhong@bungma"))
                        LogHelper.Write(model.UserName + "登录", LogHelper.LogMessageType.Info);
                    LoginFunction(user);        

                    //生成登录子应用链接
                    //var apps = "AppsUrl".GX().Split(',');
                    //string SSOLink = "";
                    //var dt = DateTime.Now;
                    //var dts = dt.ToShortDateString() + " " + dt.ToShortTimeString();
                    //var tok = HttpUtility.UrlEncode(SecurityHelper.Encrypt(dts + ";" + user.UserId + model.UserName));
                    //foreach (var item in apps)
                    //{

                    //    SSOLink += item + SsoConst.AppLoginPageName + "?" +
                    //             SsoConst.SsoTokenName + "=" + tok;//只传ID和用户名到子站吧，其他的子站ajax从主站获取
                    //    SSOLink += ",";
                    //}
                    ////封装返回的数据
                    //fillOperationResult(returnUrl, SSOLink, oper, "登录成功");
                    fillOperationResult(returnUrl, oper, "登录成功");  
                    return Json(oper, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    LogHelper.Write(model.UserName + "被" + Request.UserHostAddress + "恶意登录", LogHelper.LogMessageType.Info);
                    "UserName".AE("提供的用户名或密码不正确", ModelState); 
                }
            }
            oper.AppendData = ModelState.GE();
            return Json(oper, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 发送激活邮件
        /// </summary>
        /// <param name="user"></param>
        /// <param name="mrbll"></param>
        private void SendActiveMail( BaseBll<wmfMailRecord> mrbll,string email,string nickName,string userNameString)
        {
            LogHelper.Write(email + "发送邮件", LogHelper.LogMessageType.Debug);
            string fromEmail = CFG.应用邮箱;
            string fromEmailPassword = CFG.邮箱密码.DP();
            int emailPort = String.IsNullOrEmpty(CFG.邮箱端口) ? 587 : CFG.邮箱端口.ToAs<int>();
            var code = GenerateEncryptCode(userNameString, CFG.账号激活路径, false);
            string body = new WebClient().GetHtml("ServiceDomain".GHU() + "/H/ActiveAccountEmail").Replace("[==NickName==]", nickName).Replace("[==UserCode==]", code);
            //创建邮件对象并发送
            var mail = new SendMail(email, fromEmail, body, "激活账号", fromEmailPassword, "ServiceMailName".GX(), nickName);
            var mailRecord = new wmfMailRecord().wmfMailRecord2(email, body, "激活账号", "ServiceMailName".GX(), nickName, Guid.Parse(Reference.电子邮件类别_账号注册));
            mrbll.Insert(mailRecord);
            mail.Send("smtp.", emailPort, email + "激活账号邮件发送失败！");
        }

        /// <summary>
        /// 登录后的通用设置方法
        /// </summary>
        /// <param name="user"></param>
        private void LoginFunction(aspnet_Users user)
        {
            //用户登录都更换推广码,否则用之前的推广码。
            HttpCookie Cookie_login = Request.Cookies["HIC"];
            Cookie_login = new HttpCookie("HIC");
            if (user != null && user.wmfUserInfo != null && !String.IsNullOrEmpty(user.wmfUserInfo.HamInviteCode))
            {                
                Cookie_login["HIC"] = user.wmfUserInfo.HamInviteCode;                
            }
            else
            {
                Cookie_login["HIC"] = CFG.默认推广代码;
            }
            //对修改 及 新创建的cookie进行重新管理
            Cookie_login.Path = "/";
            Cookie_login.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Add(Cookie_login);
        } 

        // POST: /Account/LogOff        
        public ActionResult LogOff()
        {
            FormsService.SignOut();            
            System.Web.HttpContext.Current.Session.Abandon();
            return RedirectToAction("I", "H");
        }
         ///<summary>
         ///通行证登录       先不开启使用，有子系统再说
         ///</summary>
         ///<returns></returns>
        //[AllowAnonymous]
        //public String AppLogin()
        //{
        //    string userCode = Request.QueryString[SsoConst.SsoTokenName];
        //    string userName = "";
        //    if (!string.IsNullOrEmpty(userCode))
        //    {
        //        try
        //        {
        //            userCode = SecurityHelper.Decrypt(userCode);
        //            //修改了内容，取用户名要区分开来
        //            //取时间戳
        //            var ind = userCode.IndexOf(';');
        //            DateTime dt = DateTime.Parse(userCode.Substring(0, ind));
        //            var uid = Guid.Parse(userCode.Substring(ind + 1, 36));
        //            userName = userCode.Substring(ind + 1 + 36, userCode.Length - ind - 36 - 1);
        //            //在这个位置增加用户。
        //            var user = new BaseBll<aspnet_Users>().All.Where(p => p.UserName == userName).FirstOrDefault();
        //            if (user != null)
        //                FormsService.SignIn(userName, true);
        //        }
        //        catch (Exception ex)
        //        {
        //            return "";
        //        }
        //    }
        //    return ";";
        //}

        /// <summary>
        /// 通行证退出
        /// </summary>
        /// <returns></returns>
        //[AllowAnonymous]
        //public String AppLogOff()
        //{
        //    FormsService.SignOut();
        //    System.Web.HttpContext.Current.Session.Abandon();
        //    return ";";
        //}
        #endregion
        
        #region 帮助程序
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("I", "H");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // 请参见 http://go.microsoft.com/fwlink/?LinkID=177550 以查看
            // 状态代码的完整列表。
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "用户名已存在。请输入其他用户名。";

                case MembershipCreateStatus.DuplicateEmail:
                    return "该电子邮件地址的用户名已存在。请输入其他电子邮件地址。";

                case MembershipCreateStatus.InvalidPassword:
                    return "提供的密码无效。请输入有效的密码值。";

                case MembershipCreateStatus.InvalidEmail:
                    return "提供的电子邮件地址无效。请检查该值并重试。";

                case MembershipCreateStatus.InvalidAnswer:
                    return "提供的密码取回答案无效。请检查该值并重试。";

                case MembershipCreateStatus.InvalidQuestion:
                    return "提供的密码取回问题无效。请检查该值并重试。";

                case MembershipCreateStatus.InvalidUserName:
                    return "提供的用户名无效。请检查该值并重试。";

                case MembershipCreateStatus.ProviderError:
                    return "身份验证提供程序返回了错误。请验证您的输入并重试。如果问题仍然存在，请与系统管理员联系。";

                case MembershipCreateStatus.UserRejected:
                    return "已取消用户创建请求。请验证您的输入并重试。如果问题仍然存在，请与系统管理员联系。";

                default:
                    return "发生未知错误。请验证您的输入并重试。如果问题仍然存在，请与系统管理员联系。";
            }
        }
        #endregion
    }
}
