using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;

namespace WeChat
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : Window
    {
        string redirect_uri;

        /// <summary>
        /// 刷新新闻的计时器
        /// </summary>
        System.Timers.Timer timerGetNews = new System.Timers.Timer();

        /// <summary>
        /// 直播的多个群列表
        /// </summary>
        List<string> listContact = new List<string>();

        public Main(string redirect_uri)
        {
            InitializeComponent();
            if (redirect_uri == null)   return;
            this.redirect_uri = redirect_uri;
            Data.main = this;

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ContactListForLiveNews"]))
            {
                listContact = ConfigurationManager.AppSettings["ContactListForLiveNews"].Split(';').ToList();
            }
            timerGetNews.Interval = WeGlobal.NewsReflashTimer * 1000;
            timerGetNews.Elapsed += timerGetNews_Elapsed;
        }

        void timerGetNews_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var news = LiveNews.GetNews();
            for (int i = 0; i < news.Count; i++)
            {
                foreach (var contact in listContact)
                {
                    SendMessage(contact, news[i].NewsDate.ToShortTimeString() + " " + news[i].NewsTitle);
                }
            }
        }

        public async void SendMessage(string contact, string message)
        {           
            var u = from c in Data.Contactlist where (c.Value.NickName == contact 
                        || c.Value.DisplayName == contact || c.Value.Alias == contact) select c.Value;
            if (u.Count() == 0)
            {
                Trace.WriteLine(string.Format("发送消息失败！未找到群：{0}, 消息：{1}。", contact, message));
                return;
            }
            string userKey = u.First().UserName;
            long time = Time.Now();

            string url = string.Format(WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/webwxsendmsg?r={0}&pass_ticket={1}&sid={2}&skey={3}",
                time, Data.pass_ticket, Data.wxsid, Data.skey);

            WebRequest request = WebRequest.Create(url);
            //HttpWebRequest request = HttpWebRequest.CreateHttp(url);
            request.Method = "POST";

            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(Data.baseRequest));
            SendMsg msg = new SendMsg();
            msg.FromUserName = Data.me.UserName;
            msg.ToUserName = userKey;
            msg.Type = 1;
            msg.Content = message.Replace("\r", "");
            msg.ClientMsgId = time;
            msg.LocalID = time;
            
            jsonObj.Add("Msg", JObject.FromObject(msg));

            byte[] byteArray = Encoding.UTF8.GetBytes(jsonObj.ToString().Replace("\r\n", ""));
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            Trace.WriteLine("发送消息...");
            Trace.WriteLine(jsonObj.ToString().Replace("\r\n", ""));
       
            WebResponse response = await request.GetResponseAsync();
            dataStream = response.GetResponseStream();            
            StreamReader reader = new StreamReader(dataStream);
            string ret = reader.ReadToEnd();
            webwxsendmsg wxsendmsg = JsonConvert.DeserializeObject<webwxsendmsg>(ret);
            reader.Close();
            dataStream.Close();
            response.Close();

            //如果发送失败，调用同步消息获取最新synckey， 再重发一次？               
            if (wxsendmsg.MsgID == null)
            {
                this.同步消息();
                response = request.GetResponse();
                dataStream = response.GetResponseStream();
                reader = new StreamReader(dataStream);
                ret = reader.ReadToEnd();
                wxsendmsg = JsonConvert.DeserializeObject<webwxsendmsg>(ret);
                reader.Close();
                dataStream.Close();
                response.Close();
            }

            Trace.WriteLine(string.Format("发送消息成功！发送到：{0}, 消息：{1}。", contact, message));
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            显示托盘();
            获取登录信息();
            获取会话();
            读取消息();
            更新界面(true);
            开启后台线程();
        }

        BackgroundWorker backgroundWorker;
        void 开启后台线程()
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += 后台线程;
            backgroundWorker.WorkerSupportsCancellation = true;
            //backgroundWorker.RunWorkerCompleted += 退出登录;
            backgroundWorker.RunWorkerAsync();
        }

        void 后台线程(object sender, DoWorkEventArgs e)
        {
            获取通讯录();
            状态提醒();
            while (!backgroundWorker.CancellationPending)
            {
                同步消息();
                //隔15s再同步
                System.Threading.Thread.Sleep(15000);
            }
        }

        private void OnNotifyIconClick(object sender, EventArgs e)
        {
            Show();
            Activate();
        }

        System.Windows.Forms.NotifyIcon notifyIcon;
        void 显示托盘()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = "微信";
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += OnNotifyIconClick;
        }

        void 获取登录信息()
        {
            WebRequest request = WebRequest.Create(redirect_uri + "&fun=new");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(dataStream);
            XmlNode node = xmlDoc["error"];
            if (!node["ret"].InnerText.Equals("0"))
            {
                MessageBox.Show("登录失败,webwxnewloginpage.Ret:" + node["ret"].InnerText, "错误", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            Data.skey = node["skey"].InnerText;
            Data.wxsid = node["wxsid"].InnerText;
            Data.wxuin = node["wxuin"].InnerText;
            Data.pass_ticket = node["pass_ticket"].InnerText;

            Data.baseRequest = new BaseRequest(Data.wxuin, Data.wxsid, Data.skey, Data.device_id);

            string[] temp = response.Headers[HttpResponseHeader.SetCookie].Split(new char[] { ',', ';' });
            foreach (string c in temp)
            {
                if (c.Contains("webwx_data_ticket"))
                {
                    Data.webwx_data_ticket = c.Split('=')[1];
                    break;
                }
            }

            Data.cookie = "webwx_data_ticket=" + Data.webwx_data_ticket + "; wxsid=" + Data.wxsid + "; wxuin=" + Data.wxuin;

            dataStream.Close();
            response.Close();

            Trace.WriteLine("获取登录信息");
            //Trace.WriteLine(redirect_uri);
            Trace.WriteLine(xmlDoc);
        }

        void 获取会话()
        {
            //string url = "http://wx.qq.com/cgi-bin/mmwebwx-bin/webwxinit" +
            //    "?pass_ticket=" + Data.pass_ticket +
            //    "&skey=" + Data.skey +
            //    "&r=" + Time.Now();
            string url = string.Format(WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/webwxinit?r={0}&pass_ticket={1}", 0 - Time.Now(), Data.pass_ticket);
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";

            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(Data.baseRequest));

            byte[] byteArray = Encoding.UTF8.GetBytes(jsonObj.ToString().Replace("\r\n", ""));
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string initResult = reader.ReadToEnd();
            webwxinit init = JsonConvert.DeserializeObject<webwxinit>(initResult);
            Data.me = init.User;
            foreach (User user in init.ContactList)
            {
                if (Data.Chatlist.ContainsKey(user.UserName))
                    continue;
                user.setDisplayName();
                Data.Chatlist.Add(user.UserName, user);
            }

            reader.Close();
            dataStream.Close();
            response.Close();

            Trace.WriteLine("获取会话");
            //Trace.WriteLine(url);
            Trace.WriteLine(jsonObj);
            Trace.WriteLine("BaseResponse.Ret:" + init.BaseResponse.Ret);

            if (init.BaseResponse.Ret != 0)
            {
                MessageBox.Show("初始化失败,webwxinit.BaseResponse.Ret:" + init.BaseResponse.Ret, "错误", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            Data.synckey = init.SyncKey;
            myname.Content = Data.me.NickName;
            myhead.Source = new ImagePathConverter().getHead(Data.me);
        }

        void 读取消息()
        {
            long time = Time.Now();
            //string url = "http://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsync" +
            //    "?pass_ticket=" + Data.pass_ticket +
            //    "&sid=" + Data.wxsid +
            //    "&skey=" + Data.skey +
            //    "&r=" + time;
            string url = string.Format(WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/webwxsync?r={0}&pass_ticket={1}&sid={2}&skey={3}",
                0 - Time.Now(), Data.pass_ticket, Data.wxsid, Data.skey);
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";

            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(Data.baseRequest));
            jsonObj.Add("SyncKey", JObject.FromObject(Data.synckey));
            jsonObj.Add("rr", time);

            byte[] byteArray = Encoding.UTF8.GetBytes(jsonObj.ToString().Replace("\r\n", ""));
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string rlt = reader.ReadToEnd();
            webwxsync sync = JsonConvert.DeserializeObject<webwxsync>(rlt);
            Data.synckey = sync.SyncKey;

            reader.Close();
            dataStream.Close();
            response.Close();

            Trace.WriteLine("读取消息");
            //Trace.WriteLine(url);
            Trace.WriteLine("BaseResponse.Ret:" + sync.BaseResponse.Ret);
            Trace.WriteLine("AddMsgCount:" + sync.AddMsgCount);
            Trace.WriteLine(sync.AddMsgList.ToString());
            Trace.WriteLine("ModContactCount:" + sync.ModContactCount);
            Trace.WriteLine("DelContactCount:" + sync.DelContactCount);
            Trace.WriteLine("ModChatRoomMemberCount:" + sync.ModChatRoomMemberCount);

            if (sync.BaseResponse.Ret != 0)
            {
                //MessageBox.Show("读取消息失败,webwxsync.BaseResponse.Ret:" + sync.BaseResponse.Ret, "错误", MessageBoxButton.OK, MessageBoxImage.Information);
                //Close();
                Trace.WriteLine("读取消息失败,webwxsync.BaseResponse.Ret:" + sync.BaseResponse.Ret);
            }

            foreach (Msg msg in sync.AddMsgList)
            {
                记录消息(msg);
            }
        }

        public void 记录消息(Msg msg)
        {
            Trace.WriteLine("记录消息");
            Trace.WriteLine("消息类型:" + msg.MsgType);
            Trace.WriteLine("消息内容:" + msg.Content);

            string friend = msg.FromUserName.Equals(Data.me.UserName) ? msg.ToUserName : msg.FromUserName;

            //暂时只支持文字
            if (msg.MsgType != 1)
                return;

            //记录消息
            if (!Data.messages.ContainsKey(friend))
                Data.messages.Add(friend, new List<Msg>());
            Data.messages[friend].Add(msg);

            //刷新消息
            if (Data.dialogs.ContainsKey(friend) && Data.dialogs[friend] != null)
            {
                //更新窗口
                Action<Msg> updateAction = new Action<Msg>(Data.dialogs[friend].Recv);
                Data.dialogs[friend].Dispatcher.BeginInvoke(updateAction, msg);
                //任务栏闪烁
                //System.Windows.Interop.WindowInteropHelper wndHelper = new System.Windows.Interop.WindowInteropHelper(Data.dialogs[friend]);
                //flashTaskBar(wndHelper.Handle, falshType.FLASHW_TIMERNOFG);
            }
            else
            {
                //加入会话列表
                if (!Data.Chatlist.ContainsKey(friend))
                    Data.Chatlist.Add(friend, Data.Contactlist[friend]);

                //刷新ui
                if (current_isChat)
                    Dispatcher.BeginInvoke(new Action<bool>(更新界面), true);
            }

            //播放声音
            if (!msg.FromUserName.Equals(Data.me.UserName))
                Data.player.Play();
        }

        void 获取通讯录()
        {
            //string url = "http://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact" +
            //    "?pass_ticket=" + Data.pass_ticket +
            //    "&skey=" + Data.skey +
            //    "&r=" + Time.Now();
            string url = string.Format(WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/webwxgetcontact?r={0}&pass_ticket={1}&skey={2}",
                0 - Time.Now(), Data.pass_ticket, Data.skey);
            WebRequest request = WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.Cookie, Data.cookie);
            request.ContentType = "application/json; charset=UTF-8";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            webwxgetcontact getcontact = JsonConvert.DeserializeObject<webwxgetcontact>(reader.ReadToEnd());
            foreach (User user in getcontact.MemberList)
            {
                user.setDisplayName();
                Data.Contactlist.Add(user.UserName, user);
            }

            reader.Close();
            dataStream.Close();
            response.Close();

            Trace.WriteLine("获取通讯录");
            //Trace.WriteLine(url);
            Trace.WriteLine("BaseResponse.Ret:" + getcontact.BaseResponse.Ret);
            Trace.WriteLine("MemberCount:" + getcontact.MemberCount);

            if (getcontact.BaseResponse.Ret != 0)
                MessageBox.Show("获取通讯录失败,webwxgetcontact.BaseResponse.Ret:" + getcontact.BaseResponse.Ret, "错误", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void 状态提醒()
        {
            long time = Time.Now();
            //string url = "http://wx.qq.com/cgi-bin/mmwebwx-bin/webwxstatusnotify" +
            //    "?pass_ticket=" + Data.pass_ticket +
            //    "&sid=" + Data.wxsid +
            //    "&skey=" + Data.skey +
            //    "&r=" + time;
            string url = string.Format(WeGlobal.WechatHost + "cgi-bin/mmwebwx-bin/webwxstatusnotify?r={0}&pass_ticket={1}&sid={2}&skey={3}",
                0 - Time.Now(), Data.pass_ticket, Data.wxsid, Data.skey);

            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";

            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(Data.baseRequest));
            jsonObj.Add("Code", 3);
            jsonObj.Add("FromUserName", Data.me.UserName);
            jsonObj.Add("ToUserName", Data.me.UserName);
            jsonObj.Add("ClientMsgId", time);

            byte[] byteArray = Encoding.UTF8.GetBytes(jsonObj.ToString().Replace("\r\n", ""));
            request.ContentType = "application/json; charset=UTF-8";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            webwxstatusnotify statusnotify = JsonConvert.DeserializeObject<webwxstatusnotify>(reader.ReadToEnd());

            reader.Close();
            dataStream.Close();
            response.Close();

            Trace.WriteLine("状态提醒");
            //Trace.WriteLine(url);
            Trace.WriteLine(statusnotify.ToString());
        }

        bool current_isChat = true;
        void 更新界面(bool isChat)//true:Chat,false:Contact
        {
            Trace.WriteLine("更新界面");
            current_isChat = isChat;
            listbox.Items.Clear();
            Dictionary<string, User> list = isChat ? Data.Chatlist : Data.Contactlist;
            //TODO:排序
            foreach (var item in list)
            {
                listbox.Items.Add(item.Value);
            }
            //listbox.ScrollIntoView(listbox.Items[0]);
        }

        void 同步消息()
        {
            //string url = "http://webpush.weixin.qq.com/cgi-bin/mmwebwx-bin/synccheck" +
            //    "?pass_ticket=" + Data.pass_ticket +
            //    "&skey=" + Data.skey +
            //    "&sid=" + Data.wxsid +
            //    "&uin=" + Data.wxuin +
            //    "&deviceid=" + Data.device_id +
            //    "&synckey=" + Data.synckey.get_urlstring() +
            //    "&_=" + Time.Now();

            ///cgi-bin/mmwebwx-bin/synccheck?r=1440585331636&skey=%40crypt_67c13b2d_631cde5822b6fd866704b9045a13d17c&sid=jmTznox8qavfBu7%2F&uin=1547229180&deviceid=e824539813213050&synckey=1_637639781%7C2_637649490%7C3_637649454%7C11_637649450%7C201_1440585302%7C1000_1440581558 
            string url = string.Format("https://webpush2.weixin.qq.com/cgi-bin/mmwebwx-bin/synccheck?r={0}&pass_ticket={1}&sid={2}&skey={3}&uin={4}&deviceid={5}&synckey={6}&lang=zh_CN",
                Time.Now(), Data.pass_ticket, Data.wxsid, Data.skey, Data.wxuin, Data.device_id, Data.synckey.get_urlstring());

            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string ret_str = reader.ReadToEnd().Split('=')[1];
            synccheck ret = JsonConvert.DeserializeObject<synccheck>(ret_str);

            reader.Close();
            dataStream.Close();
            response.Close();

            Trace.WriteLine("同步消息");
            //Trace.WriteLine(url);
            Trace.WriteLine(ret_str);

            if (!ret.retcode.Equals("0"))
            {
                //MessageBox.Show("同步失败,synccheck.retcode:" + ret.retcode, "错误", MessageBoxButton.OK, MessageBoxImage.Information);
                //Close();
                Trace.WriteLine("同步失败,synccheck.retcode:" + ret.retcode);
            }

            if (!ret.selector.Equals("0"))
                读取消息();
        }

        private void OpenDialog(object sender, MouseButtonEventArgs e)
        {
            User user = (User)listbox.SelectedValue;
            if (Data.dialogs.ContainsKey(user.UserName))
                Data.dialogs[user.UserName].Activate();
            else
            {
                Dialog dialog = new Dialog(user);
                Data.dialogs.Add(user.UserName, dialog);
                dialog.Show();
            }
        }

        private void OnClose(object sender, MouseButtonEventArgs e)
        {
            if (Data.dialogs.Count == 0 ||
                MessageBox.Show("您还有会话窗口未关闭,确认要退出微信?", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                Application.Current.Shutdown();
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void ChatBtn_Click(object sender, MouseButtonEventArgs e)
        {
            更新界面(true);
        }

        private void ContactBtn_Click(object sender, MouseButtonEventArgs e)
        {
            更新界面(false);
        }

        private void OnMin(object sender, MouseButtonEventArgs e)
        {
            Hide();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            timerGetNews.Start();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            timerGetNews.Stop();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

    }

}
