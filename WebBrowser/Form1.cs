using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace WebBrowserT
{
    public partial class Form1 : Form
    {
        const string format = "总共需要查询{0}个考生，现在正在查询第{1}个考生.";
        string sPath = System.IO.Directory.GetCurrentDirectory() + "/考生考号.txt";
        bool bQuerying = false;      //查询状态
        WebBrowser browser = new WebBrowser();
        int nTotalNum = 0;
        int nCurNum = 0;

        public Form1()
        {
            InitializeComponent();
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            buttonQuery.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            browser.Navigate("http://www.cczsb.com/zklq_2010.asp");           
            browser.DocumentCompleted += WebBrowserDocumentCompleted;
        }

        private void WebBrowserDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            buttonQuery.Enabled = true;
            label1.Visible = false;

            if (bQuerying)
            {
                string info = string.Empty;
                HtmlElement element = this.browser.Document.GetElementById("table33");
                foreach (HtmlElement el in element.All)
                {
                    if (el.TagName.ToUpper() == "TD")
                    {
                        info += el.InnerText.Trim() + "\t";
                    }
                }

                WriteToFile(info + "\r\n");

                nCurNum++;
                if (nCurNum == nTotalNum)
                {
                    MessageBox.Show("查询完成！");
                }
            }
            bQuerying = false;
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
            if (sKH1.Substring(0,5)!="17701" || sKH2.Substring(0,5)!="17701")
            {
                lblMsg.Text = "考号的前5位必须为17701!";
                return;
            }
            if ( !(Convert.ToInt32(sKH1.Substring(5,2))>=60 && Convert.ToInt32(sKH1.Substring(5,2))<=90)
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
            
            if (!System.IO.Directory.Exists(sPath))
            {
                System.IO.File.Delete(sPath);
            }
            System.IO.File.Create(sPath);

            GetHtml(urls.ToArray());
        }

        private void GetHtml(string[] urls)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object obj)
                {
                    foreach (string url in urls)
                    {
                        this.Invoke(new Action(delegate() { lblMsg.Text = string.Format(format, nTotalNum, nCurNum + 1); }));
                        bQuerying = true;
                        BeginSearch(url);
                        while (bQuerying)
                        {
                            Application.DoEvents();
                        }
                    }
                }));
        }

        private void BeginSearch(string sKH)
        {
            Action a = delegate()
            {
                HtmlElement ksElement = browser.Document.All["kskh"];
                ksElement.SetAttribute("value", sKH);
                foreach (HtmlElement element in browser.Document.All)
                {
                    if (element.OuterHtml == "<INPUT value=提交查询内容 type=submit>")
                    {
                        element.InvokeMember("click");
                    }
                }
            };
            this.Invoke(a);
        }

        private void WriteToFile(string text)
        {
            System.IO.File.AppendAllText(sPath, text, Encoding.Default);
        }
    }
}
