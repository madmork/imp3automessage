using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Imp3AutoMessage.DB_StorageDataSetTableAdapters;

namespace Imp3AutoMessage
{
    public partial class Form1 : Form
    {
        //  需要页面URL
        private string[] pageUrls = new string[50];
        //  在程序开始后，需要把URL载入到webbrowser中
        //  然后等完全载入后，查询所有帖子的URL并保存到list中去
        private List<String> threadUrls = new List<string>();
        /// <summary>
        /// 当前页面
        /// </summary>
        private int currentPageIndex = 1;
        //  测试自动留言的帖子url
        string testAutoMessageUrl = "http://bbs.imp3.net/thread-11007854-1-1.html";
        private int currentPageUrlIndex = 1;
        private MessageURLTableAdapter adapter = new MessageURLTableAdapter();
        private string[] messages = new string[]
            {
                "不错不错，帮顶了",
                "我来顶一个",
                "帮你顶一下",
            };
        private Random random = new Random();
        private int pageCount = 50;

        public Form1()
        {
            InitializeComponent();
            InitUrl();
            InitEnvironment();
        }

        /// <summary>
        /// 对程序的初始化
        /// </summary>
        private void InitEnvironment()
        {
            //  5秒换一页
            timer1.Interval = 2000;
            //  自动回复的时间间隔
            timer2.Interval = 3 * 60 * 1000;
            button2.Enabled = false;
            label1.Text = "";
            webBrowser1.ScriptErrorsSuppressed = false;
        }

        private void InitUrl()
        {
            for (int i = 0; i < pageCount; i++)
            {
                pageUrls[i] = String.Format("http://bbs.imp3.net/forum-63-{0}.html", i + 1);
            }
        }

        //  在点击开始留言后，开始干活，否则就直接到首页，不干活
        private void button1_Click(object sender, EventArgs e)
        {
            //  需要对10个页面进行浏览并得到所有的帖子URL
            //  这里就需要个timer来控制了，控制每次webbrowser的访问的时间
            //  每次访问给10秒时间
            timer1.Enabled = true;
            timer1.Start();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private Boolean hrefIsOk(string original, string another)
        {
            string[] array1 = original.Split('-');
            string[] array2 = another.Split('-');
            if (array1[1].Equals(array2[1]))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 在页面加载好后，需要对帖子URL进行提取并保存到threadUrls中
        /// </summary>
        /// <param name="sender">webbrowser control</param>
        /// <param name="e">The <see cref="WebBrowserDocumentCompletedEventArgs"/> instance containing the event data.</param>
        private void Page_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
           
            HtmlElementCollection elements = webBrowser1.Document.GetElementsByTagName("a");
            for (int i = 0; i < elements.Count; i++)
            {
                HtmlElement element = elements[i];
                string href = element.GetAttribute("href");
                Boolean canAdd = true;
                //  对href进行判断，需要是http://bbs.imp3.net/thread-10997339-1-50.html这种格式的
                //  就不适用regular expression了
                if (href.StartsWith("http://bbs.imp3.net/thread-"))
                {
                    for (int j = 0; j < threadUrls.Count; j++)
                    {
                        if (hrefIsOk(threadUrls[j], href))
                        {
                            canAdd = false;
                            break;
                        }
                    }
                    if (canAdd)
                    {
                        threadUrls.Add(href);
                        listBox1.Items.Add(href);
                    }
                }
            }
            webBrowser1.DocumentCompleted -= Page_DocumentCompleted;
        }

        private void Thread_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlElement editor = webBrowser1.Document.GetElementById("fastpostmessage");
            //editor.ScrollIntoView();
            webBrowser1.Select();
            editor.Focus();
            int randomIndex = random.Next(messages.Length);
            editor.InnerText = messages[randomIndex];
            HtmlElement submitButton = webBrowser1.Document.GetElementById("fastpostsubmit");
            submitButton.InvokeMember("click");
            adapter.Save(threadUrls[currentPageUrlIndex], DateTime.Now);
            webBrowser1.DocumentCompleted -= Thread_DocumentCompleted;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (currentPageIndex <= pageUrls.Length)
            {
                string visitUrl = pageUrls[currentPageIndex - 1];
                webBrowser1.Navigate(visitUrl);
                webBrowser1.DocumentCompleted += Page_DocumentCompleted;
                currentPageIndex++;
            }
            else
            {
                timer1.Stop();
                timer1.Enabled = false;
                MessageBox.Show("获取完毕，本次获取到" + threadUrls.Count + "条记录。可以开始自动留言了");
                button2.Enabled = true;
            }
        }

        /// <summary>
        /// 双击List中的URL，webbrowser载入此URL
        /// </summary>
        /// <param name="sender">listBox1</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string url = listBox1.SelectedItem.ToString();
                webBrowser1.Navigate(url);
            }
        }

        /// <summary>
        /// 对帖子进行自动留言
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void button2_Click(object sender, EventArgs e)
        {
            //  对帖子的所有URL进行遍历，然后自动提交
            //  对帖子URL进行过滤
            string[] urls = threadUrls.ToArray();
            foreach (string url in urls)
            {
                string searchUrl = url.Substring(0, url.LastIndexOf('-'));
                var result = adapter.GetDataByThreadId(searchUrl);
                if (result != null && result.Count > 0)
                {
                    threadUrls.Remove(url);
                }
            }
            MessageBox.Show("本次可以对" + threadUrls.Count + "个帖子进行自动留言，点击确定开始");
            timer2.Enabled = true;
            timer2.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            threadUrls.Add(testAutoMessageUrl);
            MessageBox.Show("添加成功!");
            button3.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer2.Enabled = true;
            timer2.Start();
            button4.Enabled = false;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //  专门用来进行留言
            if (currentPageUrlIndex <= threadUrls.Count)
            {
                string url = threadUrls[currentPageUrlIndex - 1];
                webBrowser1.Navigate(url);
                webBrowser1.DocumentCompleted += Thread_DocumentCompleted;
                currentPageUrlIndex++;
                textBox1.Text = url;
                label1.Text = currentPageUrlIndex + " / " + threadUrls.Count;
            }
            else
            {
                timer2.Stop();
                timer2.Enabled = false;
            }
        }

        private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            textBox1.SelectAll();
            Clipboard.SetText(textBox1.Text);
        }
    }
}
