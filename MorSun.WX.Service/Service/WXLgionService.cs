using System;
using System.Linq;
using System.Collections.Generic;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Helpers;
using MorSun.Bll;
using MorSun.Model;
using MorSun.Common.类别;
using MorSun.Common.配置;
using HOHO18.Common.SSO;
using HOHO18.Common.WEB;
using HOHO18.Common;
using HOHO18.Common.Web;
using System.Web;

namespace MorSun.WX.NetBung.Service
{
    public class WXLgionService
    {        

        #region 返回一键登录链接
        /// <summary>
        /// 提问返回数据处理
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        private IResponseMessageBase WXLgionResponse<T>(T requestMessage, string id)
            where T : RequestMessageBase
        {
            var responseMessage = ResponseMessageBase.CreateFromRequestMessage<ResponseMessageNews>(requestMessage); //CreateResponseMessage<ResponseMessageNews>();
            var comonservice = new CommonService();
            
            responseMessage.Articles.Add(new Article()
            {
                Title = "微信快捷登录隧道",
                Description = "请在一分钟内点击穿越，穿越后该隧道自动失效！",
                PicUrl = CFG.网站域名 + "/images/zyb/wxlogin.png",
                Url = CFG.网站域名 + CFG.邦马网_微信一键登录路径 + "/" + id //model.PicUrl
            });
            
            return responseMessage;            
        }        
        #endregion        

        #region 一键登录
        /// <summary>
        /// 一键登录
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public IResponseMessageBase WXLgionResponseMessage(RequestMessageText requestMessage)
        {
            var commonService = new CommonService();
            //处理并发而生成的操作唯一ID
            var rqid = Guid.NewGuid();
            new AnswerService().RQStart(requestMessage, rqid, commonService);

            var msgid = requestMessage.MsgId == null ? "" : requestMessage.MsgId.ToString();

            var id = Guid.Empty;
            var result = "";
            if (commonService.GetMsgIdCache(msgid) == rqid)
            {
                id = Guid.NewGuid();                
                var dts = DateTime.Now.ToString();
                var tok = SecurityHelper.Encrypt(dts + ";" + CFG.邦马网_对接统一码);
                string strUrl = CFG.网站域名 + CFG.邦马网_微信登录设置路径;
                string appendUrl = "?tok=" + HttpUtility.UrlEncode(tok);

                var sid = id.ToString();
                appendUrl += "&id=" + HttpUtility.UrlEncode(SecurityHelper.Encrypt(sid));
                appendUrl += "&weixinId=" + HttpUtility.UrlEncode(SecurityHelper.Encrypt(requestMessage.FromUserName));
                //LogHelper.Write(appendUrl, LogHelper.LogMessageType.Info);
                result = GetHtmlHelper.GetPage(strUrl + appendUrl, "");
            }
            else
            {
                int i = 0;
                LogHelper.Write("非主线程等待", LogHelper.LogMessageType.Debug);
                do
                {
                    System.Threading.Thread.Sleep(500);//其他访问等1秒
                    i++;                    
                } while ((id != Guid.Empty && !String.IsNullOrEmpty(result)) || i > 6);      
            }

            if (String.IsNullOrEmpty(result))
            {
                return new InvalidCommondService().GetInvalidCommondResponseMessage(requestMessage);
            }
            else if (result == "true")
            {
                return WXLgionResponse(requestMessage, id.ToString());
            }
            else
            {
                return new InvalidCommondService().GetInvalidCommondResponseMessage(requestMessage);
            }
            
        }
        #endregion


        
    }
}