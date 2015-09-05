using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Web;

namespace Phaeton
{
    public class WebHelper
    {
        public WebHelper() { }
        
        private CookieContainer myCookieContainer = new CookieContainer();

        private HttpWebRequest httpWebRequest;
        private HttpWebResponse myHttpWebResponse;

        private Stream myResponseStream;
        private StreamWriter myStreamWriter;

        private Stream myRequestStream;
        private StreamReader myStreamReader;

        public CookieContainer cookieContainer = null;
        
        public string RequestAndResponse(HttpWebRequest myHttpWebRequest, string postData, Encoding WriteEncoding,
                Encoding ReadEncoding, string contentType, bool Response,bool gzip)
        {
            string outdata = string.Empty;

            try
            {
                //新建一个HttpWebRequest
                httpWebRequest = myHttpWebRequest;
                //Encoding.Default.GetByteCount(indata);
                httpWebRequest.ContentType = contentType;
                if (!Response && gzip)
                {
                    httpWebRequest.Headers.Add("Accept-Encoding", "gzip");
                }
                //myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1) Gecko/20070309 Firefox/2.0.0.3";
                httpWebRequest.ContentLength = WriteEncoding.GetByteCount(postData);// indata.Length;
                httpWebRequest.Method = "POST";

                //ProxySetting(myHttpWebRequest);
                httpWebRequest.CookieContainer = myCookieContainer;

                //设置HttpWebRequest的CookieContainer为刚才建立的那个myCookieContainer 
                myRequestStream = httpWebRequest.GetRequestStream();
                myStreamWriter =  new StreamWriter(myRequestStream);
                //myStreamWriter = new StreamWriter(myRequestStream);//,WriteEncoding);
                myStreamWriter.Write(postData);

                //把数据写入HttpWebRequest的Request流 
                //myRequestStream.Close();
                myStreamWriter.Close();
                myRequestStream.Close();


                //关闭打开对象
                myHttpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                //新建一个HttpWebResponse 
                myHttpWebResponse.Cookies = myCookieContainer.GetCookies(httpWebRequest.RequestUri);
                if (gzip) myHttpWebResponse.Headers.Add("Accept-Encoding", "gzip");
                //myHttpWebResponse.Headers.Add("User-Agent","Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 1.1.4322)");
                //获取一个包含url的Cookie集合的CookieCollection 
                myResponseStream = myHttpWebResponse.GetResponseStream();

                if (Response)
                {
                    myStreamReader = new StreamReader(myResponseStream, ReadEncoding);
                    outdata = myStreamReader.ReadToEnd();
                }

                //把数据从HttpWebResponse的Response流中读出 
                myStreamReader.Close();
                myResponseStream.Close();
                //显示"登录" 

            }
            catch (Exception ex)
            {
                if (myStreamWriter != null) myStreamWriter.Close();
                if (myRequestStream != null) myRequestStream.Close();
                if (myStreamReader != null) myStreamReader.Close();
                if (myResponseStream != null) myResponseStream.Close();

                return ex.Message;
            }

            return StringTrim(outdata);
        }

        //去除模式匹配的干扰字符，不包括空格
        public string StringTrim(string source)
        {
            source = source.Replace("\n", "");
            source = source.Replace("\t", "");
            source = source.Replace("\r", "");
            return source;
        }

        public string GetOneHtml(string url, Encoding encoding)
        {
            url = url.Replace("&amp;", "&");

            string outdata = "";

            //拿到了Cookie，再进行请求就能直接读取到登录后的内容了 
            httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            httpWebRequest.CookieContainer = myCookieContainer;//* 

            //刚才那个CookieContainer已经存有了Cookie,把它附加到HttpWebRequest中则能直接通过验证 
            myHttpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            myHttpWebResponse.Cookies = myCookieContainer.GetCookies(httpWebRequest.RequestUri);
            myResponseStream = myHttpWebResponse.GetResponseStream();
            myStreamReader = new StreamReader(myResponseStream, encoding);
            outdata = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            //return outdata.Replace("\r\n", "");
            return StringTrim(outdata);
            //再次显示"登录" 
            //如果把*行注释调，就显示"没有登录" 

        }

        public string KaiXinLogin()
        {
            httpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.kaixin001.com/login/login_api.php");

            string postData = "ver=1&email=rumo.11%40qq.com&rpasswd=ff8f971c6a32109c3b2251197b0959d12ab63c74&encypt=Zmrbegi0UVDXhfK&url=%2Fhome%2F&remember=1";

            return RequestAndResponse(httpWebRequest, postData, Encoding.UTF8, Encoding.UTF8,
                "application/x-www-form-urlencoded", true, true);
        }
    }
}
