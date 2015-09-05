using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WeChat
{
    public class WeGlobal
    {
        public static string WechatHost
        {
            get
            {
                return ConfigurationManager.AppSettings["WechatUrlHost"];
            }
        }

        public static int NewsReflashTimer
        {
            get
            {
                return string.IsNullOrEmpty(ConfigurationManager.AppSettings["NewsReflashTimer"]) 
                    ? 60 : int.Parse(ConfigurationManager.AppSettings["NewsReflashTimer"]);
            } 
        }
    }
}
