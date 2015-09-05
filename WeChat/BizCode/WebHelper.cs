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
                //�½�һ��HttpWebRequest
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

                //����HttpWebRequest��CookieContainerΪ�ղŽ������Ǹ�myCookieContainer 
                myRequestStream = httpWebRequest.GetRequestStream();
                myStreamWriter =  new StreamWriter(myRequestStream);
                //myStreamWriter = new StreamWriter(myRequestStream);//,WriteEncoding);
                myStreamWriter.Write(postData);

                //������д��HttpWebRequest��Request�� 
                //myRequestStream.Close();
                myStreamWriter.Close();
                myRequestStream.Close();


                //�رմ򿪶���
                myHttpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                //�½�һ��HttpWebResponse 
                myHttpWebResponse.Cookies = myCookieContainer.GetCookies(httpWebRequest.RequestUri);
                if (gzip) myHttpWebResponse.Headers.Add("Accept-Encoding", "gzip");
                //myHttpWebResponse.Headers.Add("User-Agent","Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 1.1.4322)");
                //��ȡһ������url��Cookie���ϵ�CookieCollection 
                myResponseStream = myHttpWebResponse.GetResponseStream();

                if (Response)
                {
                    myStreamReader = new StreamReader(myResponseStream, ReadEncoding);
                    outdata = myStreamReader.ReadToEnd();
                }

                //�����ݴ�HttpWebResponse��Response���ж��� 
                myStreamReader.Close();
                myResponseStream.Close();
                //��ʾ"��¼" 

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

        //ȥ��ģʽƥ��ĸ����ַ����������ո�
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

            //�õ���Cookie���ٽ����������ֱ�Ӷ�ȡ����¼��������� 
            httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            httpWebRequest.CookieContainer = myCookieContainer;//* 

            //�ղ��Ǹ�CookieContainer�Ѿ�������Cookie,�������ӵ�HttpWebRequest������ֱ��ͨ����֤ 
            myHttpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            myHttpWebResponse.Cookies = myCookieContainer.GetCookies(httpWebRequest.RequestUri);
            myResponseStream = myHttpWebResponse.GetResponseStream();
            myStreamReader = new StreamReader(myResponseStream, encoding);
            outdata = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            //return outdata.Replace("\r\n", "");
            return StringTrim(outdata);
            //�ٴ���ʾ"��¼" 
            //�����*��ע�͵�������ʾ"û�е�¼" 

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
