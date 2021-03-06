﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace WeChat
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        string uuid;
        int tip;
        string redirect_uri;

        public Login()
        {
            InitializeComponent();
        }

        void test()
        {
            new Main(null).Show();
            new Dialog(null).Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //test();
            //return;
            开启后台线程();

            //string ret = "{\"BaseResponse\": {\"Ret\": 1102,\"ErrMsg\": \"\"},\"MsgID\": \"\",\"LocalID\": \"\"}";
            //webwxsendmsg wxsendmsg = JsonConvert.DeserializeObject<webwxsendmsg>(ret);
        }

        BackgroundWorker backgroundWorker;
        void 开启后台线程()
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += 后台线程;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.ProgressChanged += 登录状态;
            backgroundWorker.RunWorkerCompleted += 登录完毕;
            backgroundWorker.RunWorkerAsync();
        }

        void 后台线程(object sender, DoWorkEventArgs e)
        {
            获取二维码();
            backgroundWorker.ReportProgress(0);

            //等待登录
            while (!backgroundWorker.CancellationPending)
            {
                ///cgi-bin/mmwebwx-bin/login?loginicon=true&uuid=YYQaWByPSA==&tip=1&r=-1696543151 
                WebRequest request = WebRequest.Create(WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/login" +
                    "?loginicon=true&tip=" + tip +
                    "&uuid=" + uuid +
                    string.Format("&r={0}",0-Time.Now()));
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string ret = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                Trace.WriteLine("等待登录");
                Trace.WriteLine(ret);

                string[] rets = ret.Split(new char[] { '=', ';' });
                string code = rets[1];
                switch (rets[1])
                {
                    case "408"://超时
                        break;
                    case "201"://已扫描
                        tip = 0;
                        状态报告(1);
                        状态报告(2);
                        backgroundWorker.ReportProgress(201);
                        break;
                    case "200"://已登录
                        状态报告(3);
                        backgroundWorker.ReportProgress(201);
                        redirect_uri = ret.Split('"')[1];
                        backgroundWorker.CancelAsync();
                        break;
                    default://400,500
                        获取二维码();
                        backgroundWorker.ReportProgress(0);
                        break;
                }
            }

        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        void 获取二维码uuid()
        {
            // /jslogin?appid=wx782c26e4c19acffb&redirect_uri=https%3A%2F%2Fwx2.qq.com%2Fcgi-bin%2Fmmwebwx-bin%2Fwebwxnewloginpage&fun=new&lang=zh_CN 
            string uri = WeGlobal.WechatHost + "jslogin?appid=wx782c26e4c19acffb&redirect_uri=https%3A%2F%2Fwx2.qq.com%2Fcgi-bin%2Fmmwebwx-bin%2Fwebwxnewloginpage&fun=new&lang=zh_CN&_=" + Time.Now();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //X509Certificate2 certificate = new X509Certificate2(PATH_TO_CERTIFICATE, PASSWORD, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
            //request.ClientCertificates.Add(new X509Certificate(@"D:\abc.cer"));
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.63 Safari/537.36";
            request.ProtocolVersion = HttpVersion.Version10;
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string ret = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();           

            uuid = ret.Split('"')[1];
            tip = 1;
        }

        void 获取二维码图片()
        {
            ///qrcode/YYQaWByPSA==
            Uri uri = new Uri(WeGlobal.WechatHost + "qrcode/" + uuid + "?t=webwx&_=" + Time.Now(), UriKind.Absolute);
            qrcode.Source = new BitmapImage(uri);
        }

        void 获取二维码()
        {
            获取二维码uuid();
            Dispatcher.BeginInvoke(new Action(获取二维码图片));
        }

        
        DateTime login_time;
        void 状态报告(int state)
        {
            Trace.WriteLine("状态报告" + state);
            string postData = "";
            switch (state)
            {
                case 1:
                    login_time = DateTime.Now;
                    postData = "{\"BaseRequest\":{\"Uin\":0,\"Sid\":0},\"Count\":1,\"List\":[{\"Type\":1,\"Text\":\"/cgi-bin/mmwebwx-bin/login, First Request Success, uuid: " + uuid + "\"}]}";
                    break;
                case 2:
                    postData = "{\"BaseRequest\":{\"Uin\":0,\"Sid\":0},\"Count\":1,\"List\":[{\"Type\":1,\"Text\":\"/cgi-bin/mmwebwx-bin/login, Second Request Start, uuid: " + uuid + "\"}]}";
                    break;
                case 3:
                    postData = "{\"BaseRequest\":{\"Uin\":0,\"Sid\":0},\"Count\":1,\"List\":[{\"Type\":1,\"Text\":\"/cgi-bin/mmwebwx-bin/login, Second Request Success, uuid: " + uuid + ", time: " + (DateTime.Now - login_time).Milliseconds + "ms\"}]}";
                    break;
            }

            string url = WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/webwxstatreport?type=1&skey=&pass_ticket=undefined&r=" + Time.Now();
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            response.Close();
        }
        

        void 登录状态(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 0:
                    login_info.Content = "请使用微信扫描二维码以登录";
                    break;
                case 201:
                    login_info.Content = "成功扫描,请在手机上点击确认以登录";
                    break;
                case 200:
                    login_info.Content = "正在登录...";
                    break;
            }
        }

        void 登录完毕(object sender, RunWorkerCompletedEventArgs e)
        {
            new Main(redirect_uri).Show();
            Close();
        }

    }
}
