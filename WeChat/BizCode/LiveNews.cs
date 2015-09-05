using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeChat
{
    class LiveNews
    {
        private static DateTime mLastPostTime = DateTime.Now.AddMinutes(-10);

        public static List<LiveNewsInfo> GetNews()
        {            
            List<LiveNewsInfo> listNews = RetriveAndPhaseNews("http://live.sina.com.cn/zt/f/v/finance/globalnews1",
                "<p class=\"bd_i_time_c\">(?<TIME>.*?)</p>.*?<p class=\"bd_i_txt_c\">(?<CONTENT>.*?)</p>").OrderBy(n=>n.NewsDate).ToList();
            
            List<LiveNewsInfo> listNews2 = RetriveAndPhaseNews("http://live.wallstreetcn.com/",
                "<li id=\"livenews-.*?<span class=\"time\">(?<TIME>.*?)</span></a>.*?<div class=\"content\"><p>(?<CONTENT>.*?)</p></div>").OrderBy(n => n.NewsDate).ToList();
            listNews.AddRange(listNews2);
  //List<LiveNewsInfo> listNews2 = RetriveAndPhaseNews("http://live.wallstreetcn.com/",
  //              "<a href=\"/livenews/.*?\"><span class=\"time\">(?<TIME>.*?)</span></a>.*?<div class=\"content\"><p>(?<CONTENT>.*?)</p></div>").OrderBy(n => n.NewsDate).ToList();
            
            var result = from a in listNews where a.NewsDate > mLastPostTime orderby a.NewsDate select a;
            //mLastPostTime = result.Max(n => n.NewsDate);
            List<LiveNewsInfo> ret = result.ToList();
            if(ret.Count > 0)
                mLastPostTime = ret[ret.Count - 1].NewsDate;
            
            return  ret;         
        }

        private static List<LiveNewsInfo> RetriveAndPhaseNews(string strUrl, string strPattern)
        {
            bool bIsCrossDay = false;
            DateTime dtNow = DateTime.Now;
            if (dtNow.Hour < 9)
            {
                bIsCrossDay = true;
            }
            List<LiveNewsInfo> listNews = new List<LiveNewsInfo>();
            Phaeton.WebHelper web = new Phaeton.WebHelper();
            string strHtml = web.GetOneHtml(strUrl, System.Text.Encoding.UTF8);
            var list = Phaeton.RegexHelper.MultiGroupPatternMatch(strHtml, strPattern);
            foreach (var item in list)
            {
                LiveNewsInfo news = new LiveNewsInfo()
                {
                    NewsDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd ") + item["TIME"].Value),
                    NewsTitle = item["CONTENT"].Value.Replace(" ", "").Replace("</p><p>", "")
                };

                if (bIsCrossDay)
                {
                    //如果早上9点之前获取到大于12点的新闻，系统将认为是前一天的
                    if (news.NewsDate.Hour >= 12)
                        news.NewsDate = news.NewsDate.AddDays(-1);
                }
                listNews.Add(news);
                /*
                listNews.Add(new LiveNewsInfo()
                {                
                    NewsDate = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd ") + item["TIME"].Value),
                    NewsTitle = item["CONTENT"].Value.Replace(" ", "").Replace("</p><p>", "")
                });
                 * */
            }
            return listNews;
        }
    }
}
