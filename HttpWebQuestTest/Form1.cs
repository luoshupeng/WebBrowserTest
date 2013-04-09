using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using mshtml;

namespace HttpWebQuestTest
{
    public partial class Form1 : Form
    {
        const string format = "总共需要查询{0}个考生，现在正在查询第{1}个考生.";
        //bool bQuerying = false;      //查询状态
        string sPath = System.IO.Directory.GetCurrentDirectory() + "/考生考号.txt";
        int nTotalNum = 0;
        int nCurNum = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonQuery_Click(object sender, EventArgs e)
        {
            string sKH1 = textBox1.Text;
            string sKH2 = textBox2.Text;

            if (sKH1.Length != 9 || sKH2.Length != 9)
            {
                lblMsg.Text = "考号的长度必须为9！";
                return;
            }
            if (sKH1.Substring(0, 5) != "17701" || sKH2.Substring(0, 5) != "17701")
            {
                lblMsg.Text = "考号的前5位必须为17701!";
                return;
            }
            if (!(Convert.ToInt32(sKH1.Substring(5, 2)) >= 60 && Convert.ToInt32(sKH1.Substring(5, 2)) <= 90)
                || !(Convert.ToInt32(sKH2.Substring(5, 2)) >= 60 && Convert.ToInt32(sKH2.Substring(5, 2)) <= 90)
                )
            {
                lblMsg.Text = "考号的第六位和第七位必须为是60和90之间的数!";
                return;
            }
            if (!(Convert.ToInt32(sKH1.Substring(7, 2)) > 0 && Convert.ToInt32(sKH1.Substring(7, 2)) <= 30)
                || !(Convert.ToInt32(sKH2.Substring(7, 2)) > 0 && Convert.ToInt32(sKH2.Substring(7, 2)) <= 30)
                )
            {
                lblMsg.Text = "考号的第八位和第九位必须为是1和30之间的数!";
                return;
            }
            if (Convert.ToInt32(sKH1) > Convert.ToInt32(sKH2))
            {
                lblMsg.Text = "考号的'始'必须大于或等于'止'!";
                return;
            }

            List<string> urls = new List<string>();
            string sTemp = string.Empty;
            int iKH1_67 = Convert.ToInt32(sKH1.Substring(5, 2));
            int iKH2_67 = Convert.ToInt32(sKH2.Substring(5, 2));
            int iKH1_89 = Convert.ToInt32(sKH1.Substring(7, 2));
            int iKH2_89 = Convert.ToInt32(sKH2.Substring(7, 2));
            for (int i = iKH1_67; i <= iKH2_67; i++)
            {
                if (i == iKH1_67)
                {
                    if (i == iKH2_67)
                    {
                        for (int j = iKH1_89; j <= iKH2_89; j++)
                        {
                            sTemp = "17701" + i.ToString("D2") + j.ToString("D2");
                            urls.Add(sTemp);
                        }
                    }
                    else
                    {
                        for (int j = iKH1_89; j <= 30; j++)
                        {
                            sTemp = "17701" + i.ToString("D2") + j.ToString("D2");
                            urls.Add(sTemp);
                        }
                    }
                }
                else if (i > iKH1_67 && i < iKH2_67)
                {
                    for (int j = 1; j <= 30; j++)
                    {
                        sTemp = "17701" + i.ToString("D2") + j.ToString("D2");
                        urls.Add(sTemp);
                    }
                }
                else  // i == iKH2_67
                {
                    for (int j = 1; j <= iKH2_89; j++)
                    {
                        sTemp = "17701" + i.ToString("D2") + j.ToString("D2");
                        urls.Add(sTemp);
                    }
                }
            }

            nTotalNum = urls.Count;
            nCurNum = 0;

            if (System.IO.File.Exists(sPath))
            {
                System.IO.File.Delete(sPath);
            }
            //System.IO.File.Create(sPath);

            GetHtml(urls.ToArray());
        }

        private void GetHtml(string[] urls)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object obj)
            {
                foreach (string url in urls)
                {
                    this.Invoke(new Action(delegate() { lblMsg.Text = string.Format(format, nTotalNum, nCurNum + 1); }));

                    BeginSearch(url);
                }
            }));
        }

        private void BeginSearch(string sKH)
        {
            //这样是可以的，当然也可以用Post方式
            //string urlAddr = string.Format("http://www.cczsb.com/zklq_2010.asp?kskh={0}",sKH);
            //HttpWebRequest request = WebRequest.Create(urlAddr) as HttpWebRequest;
            //这是POST方式,好像使用这种方式查询有点慢
            string urlAddr = "http://www.cczsb.com/zklq_2010.asp";
            string postData = "kskh=" + sKH;
            byte[] post = ASCIIEncoding.ASCII.GetBytes(postData);
            HttpWebRequest request = WebRequest.Create(urlAddr) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = post.Length;
            using (Stream streamWrite = request.GetRequestStream())
            {
                streamWrite.Write(post, 0, post.Length);
                streamWrite.Close();
            }

            HttpWebResponse webResponse = request.GetResponse() as HttpWebResponse;
            Stream stream = webResponse.GetResponseStream();
            using (StreamReader sr = new StreamReader(stream, Encoding.Default))
            {
                string html = sr.ReadToEnd();
                stream.Close();

                ProcessHtml(html);
            }
        }

        private void ProcessHtml(string html)
        {
            IHTMLDocument2 doc = new HTMLDocumentClass();
            doc.write(new object[] { html });
            string title = doc.title;

            IHTMLElementCollection elc = (IHTMLElementCollection)doc.all.tags("table");
            int leng = elc.length;
            foreach (IHTMLElement el in elc)
            {                
                string id = el.id;
                if (id == "table33")
                {
                    IHTMLElementCollection chc = (IHTMLElementCollection)el.all;
                    int length = chc.length;
                    string info = string.Empty;
                    foreach (IHTMLElement cel in chc)
                    {
                        string tagName = cel.tagName;
                        if (tagName.ToUpper() == "TD")
                        {
                            string innerText = cel.innerText;
                            info += innerText.Trim() + "\t";
                        }                        
                    }
                    WriteToFile(info + "\r\n"); 
                    break;
                }
            }

            nCurNum++;
            if (nCurNum == nTotalNum)
            {
                MessageBox.Show("查询完成");
            }
        }

        private void WriteToFile(string text)
        {
            System.IO.File.AppendAllText(sPath, text, Encoding.Default);
        }
    }
}
