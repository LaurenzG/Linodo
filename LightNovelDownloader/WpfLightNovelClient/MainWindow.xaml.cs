using DataLightNovelDownloader;
using Dto;
using HtmlAgilityPack;
using SharpEpub;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfLightNovelClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<BookDto> listOfBooks = new List<BookDto>();
        List<BookDto> displayedBookList = new List<BookDto>();
        List<ChapterDto> currentChapterList = new List<ChapterDto>();
        ObservableCollection<LabeledProgressBar> downloads = new ObservableCollection<LabeledProgressBar>();
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs eh)
        {
            chapterList.ItemsSource = currentChapterList;
            bookList.ItemsSource = displayedBookList;
            downloadList.ItemsSource = downloads;
            asyncLoadBooks();
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.ToolTipOpeningEvent, new ToolTipEventHandler(ToolTipHandler));

        }
        #region Load Books
        private void asyncLoadBooks()
        {
            txtNotificator.Text = "Loading Books";
            BackgroundWorker work = new BackgroundWorker();
            work.DoWork += loadBooks;
            work.RunWorkerAsync();
            work.RunWorkerCompleted += Work_RunWorkerCompleted;
        }
        private void Work_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bookList.Items.Refresh();
            txtNotificator.Text = "";
        }
        private void loadBooks(object sender, DoWorkEventArgs eh)
        {
            var categoryList = new List<BookDto>();
            List<HtmlNode> p = new List<HtmlNode>();
            HtmlNode root;
            
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                root = switchSite("http://www.wuxiaworld.com");
                p.AddRange(getTranslations(root, "menu-item-12207"));
                p.AddRange(getTranslations(root, "menu-item-2165"));
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message);
                txtNotificator.Text = e.Message;
            }
            catch (Exception)
            {
                MessageBox.Show("WuxiaWorld failed to load!");
            }

            try
            {
                root = switchSite("http://www.translationnations.com/");
                p.AddRange(getTranslations(root, "menu-item-524"));
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message);
                txtNotificator.Text = e.Message;
            }
            catch (Exception)
            {
                MessageBox.Show("TranslationNations failed to load!");
            }

            //try
            //{
            //    root = switchSite("http://gravitytales.com/");
            //    p.AddRange(getTranslations(root, "menu-item-11067"));
            //    p.AddRange(getTranslations(root, "menu-item-11070"));
            //}
            //catch (WebException e)
            //{
            //    MessageBox.Show(e.Message);
            //    txtNotificator.Text = e.Message;
            //}
            //catch (Exception)
            //{
            //    MessageBox.Show("GravityTales failed to load!");
            //}
            displayedBookList.Clear();
            listOfBooks.Clear();
            for (int i = 0; i < p.Count(); i++)
            {
                listOfBooks.Add(new BookDto
                {
                    Name = HttpUtility.HtmlDecode(p.ElementAt(i).InnerHtml.Split('(')[0]),
                    IndexUrl = p.ElementAt(i).GetAttributeValue("href", "")
                });
            }
            listOfBooks.Sort((x, y) => x.Name.CompareTo(y.Name));
            displayedBookList.AddRange(listOfBooks);
        }
        private List<HtmlNode> getTranslations(HtmlNode root,string id)
        {
            return root.Descendants()
                .Where(n => n.GetAttributeValue("id", "").Equals(id))
                .Single()
                .Element("ul")
                .Descendants()
                .Where(n => n.Name.Equals("a") && n.ParentNode.ParentNode.ParentNode.GetAttributeValue("id", "").Equals(id))
                .ToList();
        }
        private void Work_RunWorkerSelectedItemComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            bookList.IsEnabled = true;
            txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator), new object[] { "" });
        }
        #endregion

        #region Load Chapter
        private void loadChapters(object sender, DoWorkEventArgs eh)
        {
            var selectedBook = (BookDto)eh.Argument;
            if (selectedBook == null) return;
            BookDto book = new BookDto
            {
                Name = selectedBook.Name,
                IndexUrl = selectedBook.IndexUrl
            };
            string baseUrl = book.IndexUrl.Replace("http://", "").Split('/')[0];
            var chapters = new List<ChapterDto>();
            try
            {
                var root = switchSite(book.IndexUrl);
                if (book.IndexUrl.ToLower().Contains("wuxiaworld"))
                    try
                    {
                        root.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]").Remove();
                    }
                    catch (Exception)
                    {

                    }
                
                //remove sidebars
                try
                {
                    root.Descendants()
                        .Where(n => (n.GetAttributeValue("role", "") == "complementary" || n.GetAttributeValue("class","")=="sidebar") && n.Name == "div")
                        .First().RemoveAll();
                }
                catch { }
                List<HtmlNode> p = new List<HtmlNode>();
                if (book.IndexUrl.ToLower().Contains("gravitytales"))
                {
                    try
                    {
                        string novelId = root.Descendants()
                               .Where(n => n.GetAttributeValue("id", "") == "contentElement")
                               .First().GetAttributeValue("ng-init", "").Split(';')[0].Split('=')[1].Trim(' ');

                        #region API

                        HttpClient client = new HttpClient();
                        client.BaseAddress = new Uri("http://gravitytales.com/Novels/GetChapterGroups/"+novelId);

                        // Add an Accept header for JSON format.
                        client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                        // List data response.
                        HttpResponseMessage response = client.GetAsync("").Result;  // Blocking call!
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonString = response.Content.ReadAsStringAsync().Result;
                            IEnumerable<ChapterGroup> groups = new JavaScriptSerializer().Deserialize<IEnumerable<ChapterGroup>>(jsonString);
                            book.IndexUrl = book.IndexUrl.EndsWith("/") ? book.IndexUrl : book.IndexUrl + "/";
                            foreach (var item in groups)
                            {
                                client = new HttpClient();
                                client.BaseAddress = new Uri("http://gravitytales.com/Novels/GetNovelChapters/"+ novelId);
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                response = client.GetAsync("?groupId="+item.ChapterGroupId+"&page=0&count=25").Result;
                                if (response.IsSuccessStatusCode)
                                {
                                    jsonString = response.Content.ReadAsStringAsync().Result;
                                    
                                    IEnumerable<GravityChapters> gravChapters = new JavaScriptSerializer()
                                        .Deserialize<IEnumerable<GravityChapters>>(jsonString
                                            .Replace(jsonString.Split('[')[0], "")
                                            .Replace(jsonString.Split(']').Last(), ""));
                                    foreach (var chap in gravChapters)
                                    {
                                        
                                        chapters.Add(new ChapterDto
                                        {
                                            
                                            ChapterUrl = book.IndexUrl + chap.Slug,
                                            DisplayName = chap.Name
                                        });
                                    }
                                }
                            }
                            currentChapterList.Clear();
                            currentChapterList.AddRange(chapters);
                            //chapterList.Items.Refresh();
                            txtNotificator.Dispatcher.Invoke(new RefreshListCallback(RefreshList));
                        }

                        #endregion
                    }
                    catch(Exception e) { }

                }
                try
                {
                    p = root.Descendants()
                        .Where(n => n.GetAttributeValue("id", "") == "primary" && n.Name == "div")
                        .Single()
                        .Descendants()
                        .Where(n => n.Name.Equals("a") &&
                            (n.GetAttributeValue("href", "").ToLower().Contains("-chapter-") || n.InnerText.ToLower().Contains("chapter")||
                             n.GetAttributeValue("href", "").ToLower().Contains("prologue-") || n.InnerText.ToLower().Contains("prologue")))
                        .ToList();
                    if (p.Count < 2)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    root = removeComments(root);
                    try
                    {
                        p = root.Descendants()
                            .Where(n => n.GetAttributeValue("role", "") == "main" || n.GetAttributeValue("id", "") == "main"
                                    || n.GetAttributeValue("class", "") == "main")
                            .First()
                            .Descendants()
                            .Where(n => n.Name.Equals("a") && !n.InnerHtml.Contains(">") && !n.InnerHtml.Contains("<"))
                            .ToList();
                        if (p.Count < 2)
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        List<string> classTextEntries = new List<string>();
                        classTextEntries.Add("collapseomatic_content");
                        classTextEntries.Add("chapters");
                        classTextEntries.Add("page");
                        classTextEntries.Add("contents");
                        classTextEntries.Add("tab-content");
                        bool found = false;
                        foreach (var textEntry in classTextEntries)
                        {
                            try
                            {
                                p = root.Descendants()
                                   .Where(n => n.GetAttributeValue("class", "").Contains(textEntry)|| n.GetAttributeValue("id", "").Contains(textEntry))
                                   .First()
                                   .Descendants()
                                   .Where(n => n.Name.Equals("a"))
                                   .ToList();
                                
                                if (p.Count > 0)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            catch(Exception e) {  }
                        }
                        if (!found) txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                    new object[] { "No chapters found" });
                    }
                }
                //Undefeated God of War
                if (p.Count < 4)
                {
                    var i = SearchIndex(root);
                    p = (i.Count>p.Count)
                        ? i 
                        : p;
                }

                List<string> urlList = new List<string>();
                string link = "";
                
                for (int i = 0; i < p.Count(); i++)
                {
                    link = p.ElementAt(i).GetAttributeValue("href", "");
                    link = link.StartsWith("/") ? baseUrl + link : link;
                    if (link != "" && p.ElementAt(i).InnerText != "" && p.ElementAt(i).InnerText!="Home")
                    {
                        if (!urlList.Contains(link))
                        {
                            urlList.Add(link);
                            chapters.Add(new ChapterDto
                            {
                                ChapterId = i + 1,
                                ChapterUrl = link,
                                DisplayName = HttpUtility.HtmlDecode(p.ElementAt(i).InnerText).Trim('\n')
                            });
                        }
                        else
                        {
                            p.RemoveAt(i);
                            i--;
                        }
                    }
                    else
                    {
                        p.RemoveAt(i);
                        i--;
                    }
                }
                currentChapterList.Clear();
                currentChapterList.AddRange(chapters);
                //chapterList.Items.Refresh();
                txtNotificator.Dispatcher.Invoke(new RefreshListCallback(RefreshList));

            }
            catch (WebException w)
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                    new object[] { w.Message });
            }
        }

        private HtmlNode removeComments(HtmlNode root)
        {
            try
            {
                root.Descendants().Where(n => n.GetAttributeValue("id", "").ToLower().Contains("comments") || n.GetAttributeValue("class", "").ToLower().Contains("comment")).First().Remove();
                root.Descendants().Where(n => n.GetAttributeValue("class", "").ToLower().Contains("respond")).First().Remove();
            }
            catch
            {
                
            }
            return root;
        }
        private List<HtmlNode> SearchIndex(HtmlNode root)
        {
            try
            {
                var c = root.Descendants()
                    .Where(n => n.GetAttributeValue("id", "") == "primary" && n.Name == "div")
                    .Single()
                    .Descendants()
                    .Where(n => n.Name.Equals("a") && n.GetAttributeValue("href", "").ToLower().Contains("index"))
                    .Single();
                root = switchSite(c.GetAttributeValue("href", ""));
                //root = webget.Load(c.GetAttributeValue("href", "")).DocumentNode;
                return root.Descendants()
                    .Where(n => n.GetAttributeValue("id", "") == "primary" && n.Name == "div")
                    .Single()
                    .Descendants()
                    .Where(n => n.Name.Equals("a") && n.GetAttributeValue("href", "").ToLower().Contains("-chapter-"))
                    .ToList();
            }
            catch (Exception) {
                return new List<HtmlNode>();
            }
        }
        #endregion

        #region Download Chapter
        private void downloadChapter(object sender, DoWorkEventArgs e)
        {
            List<string> content = new List<string>();
            List<string> css = new List<string>();
            try
            {
                //Add the css style-sheet inline
                css.AddRange(System.IO.File.ReadAllLines(Environment.CurrentDirectory + 
                    ((bool)Properties.Settings.Default["AsEpub"] ? "\\ePupStyles" : "\\webStyles")));
            }
            catch (System.IO.IOException)
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                        new object[] { "The style template was not found!" });
            }
            var arguments = e.Argument as List<object>;
            var chapters = (List<ChapterDto>)arguments[0];
            double avgTime = 0;
            Stopwatch watch = Stopwatch.StartNew();
            BookDto book = (BookDto)arguments[1];
            chapters.ForEach(c=>
            {
                if (!c.ChapterUrl.Contains(book.IndexUrl))
                {
                    c.ChapterUrl = book.IndexUrl + c.ChapterUrl;
                }
            });
            var barTag = arguments[2];

            try
            {
                int i = 0;
                string s;
                foreach (ChapterDto item in chapters)
                {
                    s = addSite(item);
                    if (!s.Equals(""))
                    {
                        content.Add(s);
                    }
                    i++;
                    avgTime = (avgTime * (i - 1) + watch.Elapsed.TotalMilliseconds / 1000) / i;
                    (sender as BackgroundWorker).ReportProgress(i * 100 / chapters.Count, new { RemainingTime = avgTime * (chapters.Count - i), DownloadBar = barTag });
                    watch.Restart();
                }
            }
            catch (InvalidCastException)
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                        new object[] { "Some chapters might not be displayed properly" });
            }
            watch.Stop();
            if (content.Count > 0)
            {
                //Get the default/saved path for the file to be saved at
                string path = (Properties.Settings.Default["Path"].Equals(""))
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : (string)Properties.Settings.Default["Path"];

                book.Name = book.Name.Trim();
                string ending = ((bool)Properties.Settings.Default["AsEpub"]
                    ? ".epub"
                    : ".html");
                int start = currentChapterList.FindIndex(c => c.DisplayName == chapters[0].DisplayName)+1;
                if (chapters.Count > 1)
                    path = path + "\\" + book.Name + "-Chapters-" + start + "-" + (int)(currentChapterList.FindIndex(c => c.DisplayName == chapters.Last().DisplayName)+1) + ending;
                else
                    path = path + "\\" + book.Name + "-Chapter-" + start + ending;


                if ((bool)Properties.Settings.Default["AsEpub"])
                {
                    EpubOnFly epub = new EpubOnFly();
                    epub.Metadata.Creator = "Tangrooner";
                    epub.Metadata.Title = book.Name;
                    for (int i = 0; i < content.Count; i++)
                    {
                        epub.AddContent(book.Name + "-" + (start + i) + ".html", string.Join("", css) + content[i]);
                    }
                    epub.BuildToFile(path);
                }
                else
                {
                    css.AddRange(content);
                    System.IO.File.WriteAllLines(path, css);
                }

                downloadList.Dispatcher.Invoke(new HideProgressBarCallback(HideProgressBar), new object[] { barTag, path });
            }
            else
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                        new object[] { "Failed to download the chapter!" });
                downloadList.Dispatcher.Invoke(new HideProgressBarCallback(HideProgressBar), new object[] { barTag, "" });
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string barTag = (string)e.UserState.GetType().GetProperty("DownloadBar").GetValue(e.UserState);
            foreach (var item in downloads)
            {
                if (item.Tag.Equals(barTag))
                {
                    item.ContentLabel.Content = e.ProgressPercentage + "% ~"
                        + string.Format("{0:N1}", (double)e.UserState.GetType().GetProperty("RemainingTime").GetValue(e.UserState))
                        + "s remaining";
                    item.ProgressBar.Value = e.ProgressPercentage;
                }
            }
        }

        private string addSite(ChapterDto item)
        {
            var root = switchSite(item.ChapterUrl);
            if (root == null)
            {
                return "";
            }
            HtmlNode p;
            //Tries to find the main part of the chapter
            try
            {
                p = root.Descendants()
                .Where(n => n.GetAttributeValue("class", "").Contains("entry-content"))
                .First();
                if (p.InnerText.Count() < 100)
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                try
                {
                    p = root.Descendants()
                    .Where(n => (n.GetAttributeValue("class", "").Contains("main") || n.GetAttributeValue("id", "").Contains("main"))
                     && !n.GetAttributeValue("class", "").Contains("header") && !n.GetAttributeValue("class", "").Contains("menu")
                     && !n.GetAttributeValue("class","").Contains("navigation"))
                    .First();
                }
                catch (Exception)
                {
                    try
                    {
                        p = root.Descendants()
                        .Where(n => n.GetAttributeValue("class", "").Contains("post_body"))
                        .First();
                    }
                    catch (Exception)
                    {
                        try
                        {
                          p = root.Descendants()
                           .Where(n => n.GetAttributeValue("class", "").Contains("content"))
                           .First();

                        }
                        catch (Exception)
                        {
                            p = new HtmlDocument().DocumentNode;
                        }
                       
                    }
                }
            }
            root = removeComments(root);
            //Add title
            try
            {
                p.InnerHtml = root.Descendants().Where(n => n.GetAttributeValue("class", "") == "entry-title").First().OuterHtml
                    + p.InnerHtml;
            }
            catch { }
            //Remove Share-part of the site
            try
            {
                while (true)
                {
                    p.Descendants().Where(n => n.GetAttributeValue("class", "").ToLower().Contains("sharedaddy")).First().Remove();
                }
            }
            catch (Exception){}
            //Remove ads on WuxiaWorld
            if (item.ChapterUrl.ToLower().Contains("wuxiaworld"))
            {
                try
                {
                    p.Descendants().Where(n => n.GetAttributeValue("class", "") == "code-block code-block-4 ai-viewport-3").First().RemoveAll();
                }
                catch   {   }
            }
            //Adds the Html to the list if the content is not empty
            if (p.LastChild != null)
            {
                return encodeString(p.InnerHtml);
            }
            return "";
        }

        private string encodeString(string s)
        {
            //Changes the special characters that are used in the html so that they don't get encoded
            s = s.Replace(">", "*Bly*at*");
            s = s.Replace("<", "*Bl*yat*");
            s = s.Replace(":", "*Blya*t*");
            s = s.Replace("=", "*B*lyat*");
            s = s.Replace("\"", "*B*lya*t");
            s = s.Replace("'", "*B*ly*at");

            //In some cases the text is already encoded, which results in display-errors
            s = HttpUtility.HtmlDecode(s);

            //The AntiXSSLibrary encodes even chinese chars and other unorthodox characters
            s = Microsoft.Security.Application.Encoder.HtmlEncode(s, true);

            //Change the html tags and co back to their original form
            s = s.Replace("*Bly*at*", ">");
            s = s.Replace("*Bl*yat*", "<");
            s = s.Replace("*Blya*t*", ":");
            s = s.Replace("*B*lyat*", "=");
            s = s.Replace("*B*lya*t", "\"");
            s = s.Replace("*B*ly*at", "'");
            return s;
        }

        #endregion

        #region Get Latest Chapters
        private void getLatestChapters(List<ChapterDto> chapters)
        {
            bool success=false;
            var root = new HtmlDocument().DocumentNode;
            HtmlNode next= new HtmlDocument().DocumentNode;
            string baseUrl = chapters.Last().ChapterUrl.Replace("http://", "").Split('/')[0];
            //Remove bad links to get to the latest one that is working
            while (!success && chapters.Count>0)
            {
                try
                {
                    root = switchSite(chapters.Last().ChapterUrl);
                    next = nextChapter(root);
                    root = changeSite(root,baseUrl);
                    success = true;
                }
                catch (Exception)
                {
                    chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
                                        new object[] { chapters.Last() });
                    chapters.Remove(chapters.Last());
                }
            }            
            bool stop = false;
            
            //Continues to read the next chapter links and adds those chapters to the chapter list
            while (!stop)
            {
                try
                {
                    if (chapters.Last().ChapterUrl != next.GetAttributeValue("href", ""))
                    {
                        ChapterDto c = new ChapterDto();
                        c.ChapterUrl = next.GetAttributeValue("href", "");
                        
                        if (c.ChapterUrl.First().Equals('/'))
                        {
                            if (chapters.Last().ChapterUrl.StartsWith("http://"))
                                chapters.Last().ChapterUrl = chapters.Last().ChapterUrl.Remove(0, 7);
                            c.ChapterUrl = baseUrl + c.ChapterUrl;
                        }
                        c.ChapterId = chapters.Count+1;
                        try
                        {
                            c.DisplayName = GetDisplayName(root);
                        }
                        catch (Exception e)
                        {
                            if (c.ChapterUrl.ToLower().Contains("wuxiaworld") || c.ChapterUrl.ToLower().Contains("translationnations"))
                                throw new InvalidOperationException();
                            c.DisplayName = "Chapter " + c.ChapterId;
                        }
                        chapters.Add(c);
                        chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
                                        new object[] { c });
                        next = nextChapter(root);
                    }
                    else throw new HttpException();
                    root = changeSite(root,baseUrl);
                }
                catch (Exception)
                {
                    stop = true;
                    txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator), new object[] { "" });
                }
            }
        }
        private string GetDisplayName(HtmlNode root)
        {
            return HttpUtility.HtmlDecode(root.Descendants().Where(n => (n.Name == "h1" && n.InnerText.ToLower().Contains("chapter")) ||
                                                                                                  n.GetAttributeValue("class", "") == "entry-title").FirstOrDefault().InnerText).Trim('\r').Trim('\n').Trim(' ');
        }
        #endregion

        #region Change Site
        private HtmlNode changeSite(HtmlNode root,string baseUrl="")
        {
            var str = nextChapter(root).GetAttributeValue("href", "");
            str = str.StartsWith("/") ? baseUrl + str : str;
            return switchSite(str);
        }
        private HtmlNode nextChapter(HtmlNode root)
        {
            return root.Descendants()
                    .Where(n => n.Name == "a" && n.InnerText.ToLower().Contains("next"))
                    .First();
        }
        private HtmlNode switchSite(string url)
        {
            var page = new HtmlDocument();
            try
            {
                
                if (!url.Substring(0, 6).Contains("http"))
                {
                    url = "http://" + url;
                }
                page.LoadHtml(new WebClient
                {
                    Encoding = Encoding.UTF8
                }.DownloadString(url));
            }
            catch (Exception e)
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                    new object[] { e.Message });
                return null;
            }
            return page.DocumentNode;
        }
        #endregion

        #region Callback Methods
        private void UpdateItems(ChapterDto c)
        {
            if (currentChapterList.Contains(c)) currentChapterList.Remove(c);
            else currentChapterList.Add(c);
            chapterList.Items.Refresh();
        }
        private void RemoveDownload(LabeledProgressBar lProgress)
        {
            downloads.Remove(lProgress);
        }
        private void RefreshList()
        {
            chapterList.Items.Refresh();
        }

        private void UpdateTxtNotificator(string msg)
        {
            txtNotificator.Text = msg;
        }
        private void HideProgressBar(string barTag, string path)
        {
            foreach (var item in downloads)
            {
                if (barTag.Equals(item.Tag))
                {
                    if (path == "")
                    {
                        downloads.Remove(item);
                        return;
                    }
                    item.ContentLabel.Content = "File created successfully, click to open!";
                    item.ContentLabel.Tag = path;
                    item.ContentLabel.AddHandler(MouseDownEvent, new RoutedEventHandler(ContentLabel_MouseDown));
                    System.Timers.Timer timer = new System.Timers.Timer(5000) { Enabled = true };
                    timer.Elapsed += (sender, args) =>
                    {
                        downloadList.Dispatcher.Invoke(new RemoveDownloadCallback(RemoveDownload), new object[] { item });
                        timer.Dispose();
                    };
                    return;
                }
            }
        }
        

        #endregion

        #region Eventhandler
        private void ContentLabel_MouseDown(object sender, RoutedEventArgs e)
        {
            
            Process.Start((string)sender.GetType().GetProperty("Tag").GetValue(sender));
        }

        private void refreshChapterBtn_Click(object sender, RoutedEventArgs e)
        {
            List<ChapterDto> chapters = chapterList.Items.OfType<ChapterDto>().ToList();
            if (chapters.Count > 0)
            {
                txtNotificator.Text = "Searching...";
                Thread t = new Thread(() => getLatestChapters(chapters));
                t.Start();
            }
        }

        private void bookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bookList.IsEnabled = false;
            txtNotificator.Text = "Getting Chapters";
            BackgroundWorker work = new BackgroundWorker();
            work.DoWork += loadChapters;
            work.RunWorkerAsync(bookList.SelectedItem);
            work.RunWorkerCompleted += Work_RunWorkerSelectedItemComplete;
        }

        private void downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            List<ChapterDto> selectedChapters = chapterList.SelectedItems.OfType<ChapterDto>().ToList();
            BookDto selectedBook = (BookDto)bookList.SelectedItem;
            string dt = selectedBook?.Name+DateTime.Now.Millisecond;

            if (selectedBook == null) selectedBook = new BookDto { IndexUrl = findBook.Text, Name = "Custom" };
            if (selectedChapters.Count > 0)
            {
                BackgroundWorker worker = new BackgroundWorker();
                downloads.Add(new LabeledProgressBar()
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Tag = dt,
                    ToolTip = new ToolTip()
                    {
                        Content = selectedBook.Name + ", "
                        + selectedChapters.First().DisplayName + "-" + selectedChapters.Last().DisplayName + " -> Count: " + selectedChapters.Count
                    }
                });
                downloadList.Items.Refresh();
                if (downloadList.Visibility == Visibility.Hidden) downloadListBtn_Click(new { }, new RoutedEventArgs());
                worker.WorkerReportsProgress = true;
                try
                {
                    worker.DoWork += downloadChapter;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerAsync(new List<object> { selectedChapters, selectedBook, dt });
                }
                catch (WebException we)
                {
                    txtNotificator.Text = we.Message;
                }
            }
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings home = new Settings();
            home.Owner = this;
            home.ShowDialog();
        }
        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            displayedBookList.Clear();
            foreach (var p in listOfBooks.Where(p => (p.Name.ToLower().Contains(searchBox.Text.ToLower()))))
            {
                displayedBookList.Add(p);
            }
            bookList.Items.Refresh();
        }
        private void CustomSiteBtn_Click(object sender, RoutedEventArgs e)
        {
            HtmlNode root;
            try
            {
                root = switchSite(findBook.Text);
            }
            catch (WebException exc)
            {
                txtNotificator.Text = exc.Message;
                return;
            }
            BookDto b = new BookDto
            {
                Name = (CustomBookTxt.Text.Count() > 0)
                        ? CustomBookTxt.Text
                        : "Custom",
                IndexUrl = findBook.Text
            };
            listOfBooks.Insert(0,b);
            displayedBookList.Insert(0, b);
            bookList.Items.Refresh();
            bookList.SelectedItem = bookList.Items.GetItemAt(0);
        }

        private void getCustomChapterBtn_Click(object sender, RoutedEventArgs e)
        {
            List<ChapterDto> chapters = new List<ChapterDto>();
            var w = new AddChapterDialog();
            w.Owner = this;
            w.ShowDialog();
            ChapterDto c = w.Chapter;
            if (c.DisplayName == "")
            {
                try
                {
                    c.DisplayName = GetDisplayName(switchSite(c.ChapterUrl));
                }
                catch {}
            }
            if (w.Success)
            {
                c.ChapterId = currentChapterList.Count;
                currentChapterList.Add(c);
                chapterList.Items.Refresh();
            }
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (ChapterDto item in chapterList.SelectedItems)
            {
                currentChapterList.Remove(item);
            }
            chapterList.Items.Refresh();
        }

        private void downloadListBtn_Click(object sender, RoutedEventArgs e)
        {
            if (bookList.Visibility == Visibility.Visible)
            {
                downloadListBtn.Content = "Show Books";
                bookList.Visibility = Visibility.Hidden;
                searchBox.Visibility = Visibility.Hidden;
                downloadLbl.Visibility = Visibility.Visible;
                downloadList.Visibility = Visibility.Visible;
            }
            else
            {
                downloadListBtn.Content = "Show Downloads";
                bookList.Visibility = Visibility.Visible;
                searchBox.Visibility = Visibility.Visible;
                downloadLbl.Visibility = Visibility.Hidden;
                downloadList.Visibility = Visibility.Hidden;
            }
        }

        private void ToolTipHandler(object sender, ToolTipEventArgs e)
        {
            // To stop the tooltip from appearing, mark the event as handled
            if (!(bool)Properties.Settings.Default["ShowToolTips"] && !e.Source.ToString().Contains("LabeledProgressBar"))
                e.Handled = true;
        }
        #endregion

        #region delegates
        private delegate void HideProgressBarCallback(string barTag, string path);
        private delegate void UpdateTxtNotificatorCallback(string msg);
        private delegate void RefreshListCallback();
        private delegate void UpdateItemsCallback(ChapterDto c);
        private delegate void RemoveDownloadCallback(LabeledProgressBar lProgress);
        #endregion
    }
}
