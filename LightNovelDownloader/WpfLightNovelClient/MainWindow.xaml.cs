using DataLightNovelDownloader;
using Dto;
using HtmlAgilityPack;
using SharpEpub;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs eh)
        {
            chapterList.ItemsSource = currentChapterList;
            bookList.ItemsSource = displayedBookList;
            asyncLoadBooks();
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.ToolTipOpeningEvent, new ToolTipEventHandler(ToolTipHandler));

        }

        private void ToolTipHandler(object sender, ToolTipEventArgs e)
        {
            // To stop the tooltip from appearing, mark the event as handled
            if(!(bool)Properties.Settings.Default["ShowToolTips"])
                e.Handled = true;
        }

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

            #region Fill p
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

            try
            {
                root = switchSite("http://gravitytales.com/");
                p.AddRange(getTranslations(root, "menu-item-11067"));
                p.AddRange(getTranslations(root, "menu-item-11070"));
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message);
                txtNotificator.Text = e.Message;
            }
            catch (Exception)
            {
                MessageBox.Show("GravityTales failed to load!");
            }
            #endregion
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

        private void bookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bookList.IsEnabled = false;
            txtNotificator.Text = "Getting Chapters";
            BackgroundWorker work = new BackgroundWorker();
            work.DoWork += loadChapters;
            work.RunWorkerAsync(bookList.SelectedItem);
            work.RunWorkerCompleted += Work_RunWorkerSelectedItemComplete;
        }

        private void Work_RunWorkerSelectedItemComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            bookList.IsEnabled = true;
            txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator), new object[] { "" });
        }

        private void loadChapters(object sender, DoWorkEventArgs eh)
        {
            var selectedBook = (BookDto)eh.Argument;
            
            
            if (selectedBook == null) return;
            BookDto book = new BookDto
            {
                Name = selectedBook.Name,
                IndexUrl = selectedBook.IndexUrl
            };

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
                        bool found = false;
                        foreach (var textEntry in classTextEntries)
                        {
                            try
                            {
                                p = root.Descendants()
                                   .Where(n => n.GetAttributeValue("class", "").Contains(textEntry))
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
                            catch { }
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
                for (int i = 0; i < p.Count(); i++)
                {
                    if (p.ElementAt(i).GetAttributeValue("href", "") != "" && p.ElementAt(i).InnerText != "" && p.ElementAt(i).InnerText!="Home")
                    {
                        if (!urlList.Contains(p.ElementAt(i).GetAttributeValue("href", "")))
                        {
                            urlList.Add(p.ElementAt(i).GetAttributeValue("href", ""));
                            chapters.Add(new ChapterDto
                            {
                                ChapterId = i + 1,
                                ChapterUrl = p.ElementAt(i).GetAttributeValue("href", ""),
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

        private void RefreshList()
        {
            chapterList.Items.Refresh();
        }

        private void UpdateTxtNotificator(string msg)
        {
            txtNotificator.Text = msg;
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

        private void downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            List<ChapterDto> selectedChapters = chapterList.SelectedItems.OfType<ChapterDto>().ToList();
            BookDto selectedBook = (BookDto)bookList.SelectedItem;
            
            if (selectedBook == null) selectedBook = new BookDto { IndexUrl = findBook.Text, Name = "Custom" };
            if (selectedChapters.Count > 0)
            {
                BackgroundWorker worker = new BackgroundWorker();
                downloadProgress.Value = 0;
                downloadProgress.Visibility = Visibility.Visible;
                txtNotificator.Text = "Downloading...";
                worker.WorkerReportsProgress = true;
                try
                {
                    worker.DoWork += downloadChapter;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerAsync(new List<object> { selectedChapters, selectedBook });
                }
                catch (WebException we)
                {
                    txtNotificator.Text = we.Message;
                }
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            downloadProgress.Value = e.ProgressPercentage;
        }

        private void downloadChapter(object sender, DoWorkEventArgs e)
        {
            List<string> content = new List<string>();
            List<string> css = new List<string>();
            try
            {
                //Add the css style-sheet inline
                css.AddRange(System.IO.File.ReadAllLines(Environment.CurrentDirectory + "\\styles"));
            }
            catch (System.IO.IOException)
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                        new object[] { "The style template was not found!" });
            }
            
            var arguments = e.Argument as List<object>;
            var chapters = (List<ChapterDto>)arguments[0];
            var firstChapter = chapters[0];
            
            ChapterDto lastChapter = new ChapterDto();
            try
            {
                int i = 0;
                string s;
                foreach (ChapterDto item in chapters)
                {
                    s = addSite(item);
                    if (!s.Equals(""))
                    {
                        lastChapter = item;
                        content.Add(s);
                    }
                    i++;
                    (sender as BackgroundWorker).ReportProgress(i*100/chapters.Count);
                }
            }
            catch (InvalidCastException)
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                        new object[] { "Some chapters might not be displayed properly" });
            }
            if (content.Count > 0)
            {
                //Get the default/saved path for the file to be saved at
                string path = (Properties.Settings.Default["Path"].Equals(""))
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : (string)Properties.Settings.Default["Path"];

                //Get the book, to name the site accordingly
                BookDto book = (BookDto)arguments[1];
                string ending = ((bool)Properties.Settings.Default["AsEpub"]
                    ? ".epub"
                    : ".html");

                if (chapters.Count > 1)
                    path = path + "\\" + book.Name + "-Chapters-" + firstChapter.ChapterId + "-" + lastChapter.ChapterId + ending;
                else
                    path = path + "\\" + book.Name + "-Chapter-" + firstChapter.ChapterId + ending;


                if ((bool)Properties.Settings.Default["AsEpub"])
                {
                    EpubOnFly epub = new EpubOnFly();
                    epub.Metadata.Creator = "Tangrooner";
                    epub.Metadata.Title = book.Name;
                    for (int i = 0; i < content.Count; i++)
                    {
                        epub.AddContent(book.Name + "-" + (firstChapter.ChapterId + i) + ".html", string.Join("", css) + content[i]);
                    }
                    epub.BuildToFile(path);
                }
                else
                {
                    css.AddRange(content);
                    System.IO.File.WriteAllLines(path, css);
                }

                txtNotificator.Dispatcher.Invoke(new UpdateProgressBarCallback(UpdateProgress),
                        new object[] { path });
                downloadProgress.Dispatcher.Invoke(new HideProgressBarCallback(HideProgressBar));
            } else
            {
                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                        new object[] { "Failed to download the chapter!" });
                downloadProgress.Dispatcher.Invoke(new HideProgressBarCallback(HideProgressBar));
            }
        }

        private void HideProgressBar()
        {
            downloadProgress.Visibility = Visibility.Collapsed;
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
        private delegate void UpdateProgressBarCallback(string path);
        private delegate void HideProgressBarCallback();
        private delegate void UpdateTxtNotificatorCallback(string msg);
        private delegate void RefreshListCallback();
        private delegate void UpdateItemsCallback(ChapterDto c);
        private void getLatestChapters(List<ChapterDto> chapters)
        {
            bool success=false;
            var root = new HtmlDocument().DocumentNode;
            HtmlNode next= new HtmlDocument().DocumentNode;
            //Remove bad links to get to the latest one that is working
            while (!success && chapters.Count>0)
            {
                try
                {
                    root = switchSite(chapters.Last().ChapterUrl);
                    next = nextChapter(root);
                    root = changeSite(root);
                    success = true;
                }
                catch (Exception)
                {
                    chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
                                        new object[] { chapters.Last() });
                    chapters.Remove(chapters.Last());
                }
            }
            //catch (Exception)
            //{
            //    if(chapters.ElementAt(chapters.Count-1).DisplayName=="")
            //        chapters.RemoveAt(chapters.Count - 1);
            //    chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
            //    new object[] { chapters });
            //    return;
            //}
            
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
                        c.ChapterId = chapters.Count+1;
                        try
                        {
                            c.DisplayName = HttpUtility.HtmlDecode(root.Descendants().Where(n => (n.Name == "h1" && n.InnerText.ToLower().Contains("chapter")) ||
                                                                                                  n.GetAttributeValue("class","")=="entry-title").FirstOrDefault().InnerText);
                        }
                        catch (Exception)
                        {
                            if (c.ChapterUrl.ToLower().Contains("wuxiaworld") || c.ChapterUrl.ToLower().Contains("gravitytales") || c.ChapterUrl.ToLower().Contains("translationnations"))
                                throw new InvalidOperationException();
                            c.DisplayName = "Chapter " + c.ChapterId;
                        }
                        chapters.Add(c);
                        chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
                                        new object[] { c });
                        next = nextChapter(root);
                    }
                    else throw new HttpException();
                    root = changeSite(root);
                }
                catch (Exception)
                {
                    stop = true;
                    txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator), new object[] { "" });
                }
            }
        }

        private HtmlNode changeSite(HtmlNode root)
        {
            var p = nextChapter(root);
            return switchSite(p.GetAttributeValue("href", ""));
        }
        private HtmlNode nextChapter(HtmlNode root)
        {
            return root.Descendants()
                    .Where(n => n.Name == "a" && n.InnerText.ToLower().Contains("next"))
                    .First();
        }
        private void UpdateItems(ChapterDto c)
        {
            if (currentChapterList.Contains(c)) currentChapterList.Remove(c);
            else currentChapterList.Add(c);
            chapterList.Items.Refresh();
        }
        private void UpdateProgress(string path)
        {
            txtNotificator.Text = "File created successfully,click to open!";
            txtNotificator.Tag = path;
            txtNotificator.AddHandler(MouseDownEvent, new RoutedEventHandler(txtNotificator_MouseDown));           
            System.Timers.Timer timer = new System.Timers.Timer(5000) { Enabled = true };
            timer.Elapsed += (sender, args) =>
            {
                txtNotificator.RemoveHandler(MouseDownEvent, new RoutedEventHandler(txtNotificator_MouseDown));

                txtNotificator.Dispatcher.Invoke(new UpdateTxtNotificatorCallback(UpdateTxtNotificator),
                                        new object[] { "" });

                timer.Dispose();
            };
        }

        private void txtNotificator_MouseDown(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(txtNotificator.Tag.ToString());
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
    }
}
