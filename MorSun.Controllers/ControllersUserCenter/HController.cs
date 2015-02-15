using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MorSun.Bll;
using MorSun.Model;
using System.Web.Routing;
using MorSun.Common.类别;
using MorSun.Common.配置;
using HOHO18.Common.SSO;
using HOHO18.Common.WEB;
using HOHO18.Common;
using System.Web.Security;


namespace MorSun.Controllers
{
    public class HController : Controller
    {
        public ActionResult I(string id, string returnUrl)
        {            
            var refuseUrl = "RefuseUrl".GX().Split(',');
            LogHelper.Write("ServiceDomain".GHU(), LogHelper.LogMessageType.Info);
            if (refuseUrl.Contains("ServiceDomain".GHU()))
                return Redirect("http://" + "ServiceDomain".GX());

            var UInfo = new UInfo();            
            var uw = GetUserBound();
            if (uw == null || String.IsNullOrEmpty(uw.WeiXinId))
            {
                UInfo.IsBoundZYB = false;
                //缓存存储
                UInfo.BoundCode = CFG.微信绑定前缀 + " " + GetUserBoundCache().BoundCode.ToString();
            }
            else
            {
                UInfo.IsBoundZYB = true;
            }
            return View(UInfo);
        }

        #region 获取用户的绑定信息
        protected static Guid UserID
        {
            get
            {
                string name = System.Web.HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(name))
                    System.Web.HttpContext.Current.Response.Redirect(FormsAuthentication.LoginUrl);
                MembershipUser user = Membership.GetUser();
                if (user == null)
                    System.Web.HttpContext.Current.Response.Redirect(FormsAuthentication.LoginUrl);
                return new Guid(user.ProviderUserKey.ToString());
            }
        }     

        /// <summary>
        /// 获取用户的微信绑定作业邦信息
        /// </summary>
        /// <returns></returns>
        protected bmUserWeixin GetUserBound()
        {
            var wxyy = Guid.Parse(CFG.邦马网_当前微信应用);
            return new BaseBll<bmUserWeixin>().All.FirstOrDefault(p => p.UserId == UserID && p.WeiXinAPP == wxyy);
        }

        /// <summary>
        /// 获取用户绑定信息
        /// </summary>
        /// <returns></returns>
        protected UserBoundCache GetUserBoundCache()
        {
            var key = CFG.微信绑定前缀 + UserID.ToString();
            var ubc = CacheAccess.GetFromCache(key) as UserBoundCache;
            //如果缓存为空，重新设置值
            if (ubc == null)
            {
                ubc = new UserBoundCache();
                ubc.UserId = UserID;
                Random Rdm = new Random();
                int iRdm = 0;
                do
                {
                    iRdm = Rdm.Next(1, 999999);

                } while (GetUserBoundCodeCache(iRdm) != null);//不为空才会再生成
                //马上设置生成码缓存
                var codeKey = CFG.微信绑定前缀 + iRdm.ToString();
                var ubcc = new UserBoundCodeCache();
                ubcc.UserId = UserID;
                ubcc.BoundCode = iRdm;
                CacheAccess.InsertToCacheByTime(codeKey, ubcc, 120);//两分钟内过期
                ubc.BoundCode = iRdm;
                CacheAccess.InsertToCacheByTime(key, ubc, 120);
            }
            return ubc;
        }

        /// <summary>
        /// 根据绑定代码取要绑定的用户
        /// </summary>
        /// <param name="boundCode"></param>
        /// <returns></returns>
        protected UserBoundCodeCache GetUserBoundCodeCache(int boundCode)
        {
            var key = CFG.微信绑定前缀 + boundCode.ToString();
            return CacheAccess.GetFromCache(key) as UserBoundCodeCache;
        }
        #endregion        

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult ActiveAccountEmail()
        {
            return View();
        }

        public ActionResult AccountChangePassword()
        {
            return View();
        }

        
        public IFormsAuthenticationService FormsService { get; set; }
        protected override void Initialize(RequestContext requestContext)
        {
            if (FormsService == null) { FormsService = new FormsAuthenticationService(); }   
            base.Initialize(requestContext);
        }
 
        /// <summary>
        /// 获取绑定用户
        /// </summary>
        /// <param name="userWeiXinId"></param>
        /// <returns></returns>
        private bmUserWeixin GetUserByWeiXinId(string userWeiXinId)
        {
            if (!String.IsNullOrEmpty(userWeiXinId))
                return new BaseBll<bmUserWeixin>().All.Where(p => p.WeiXinId == userWeiXinId).FirstOrDefault();
            else
                return null;
        }        
    }
}
