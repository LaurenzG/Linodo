using DataLightNovelDownloader;
using Dto;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadBooks();
            this.DataContext = listOfBooks;
        }
        private void loadBooks()
        {
            var categoryList = new List<BookDto>();
            List<HtmlNode> p = new List<HtmlNode>();
            HtmlNode root;

            #region Fill p
            try
            {
                root = switchSite("http://www.wuxiaworld.com");
                p.AddRange(getTranslations(root, "menu-item-12207"));
                p.AddRange(getTranslations(root, "menu-item-2165"));
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message);
                txtNotificator.Text = e.Message;
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
            #endregion
            listOfBooks.Clear();
            for (int i = 0; i < p.Count(); i++)
            {
                listOfBooks.Add(new BookDto
                {
                    Name = HttpUtility.HtmlDecode(p.ElementAt(i).InnerHtml.Split('(')[0]),
                    IndexUrl = p.ElementAt(i).GetAttributeValue("href", "")
                });
            }
            listOfBooks = listOfBooks.OrderBy(c => c.Name).ToList();
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
            if (!url.Substring(0, 6).Contains("http"))
            {
                url = "http://" + url;
            }
            var page = new HtmlDocument();
            try
            {
                page.LoadHtml(new WebClient
                {
                    Encoding = Encoding.UTF8
                }.DownloadString(url));
            }
            catch (Exception e)
            {
                txtNotificator.Text = e.Message;
            }
            return page.DocumentNode;
        }

        private void bookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtNotificator.Text = "";
            if (((System.Windows.Controls.Primitives.Selector)sender).SelectedItem == null) return;
            Book book = new Book
            {
                Name = ((BookDto)((System.Windows.Controls.Primitives.Selector)sender).SelectedItem).Name,
                IndexUrl = ((BookDto)((System.Windows.Controls.Primitives.Selector)sender).SelectedItem).IndexUrl,
            };
            //var books = await client.GetAsync<BookDto>(((BookDto)((System.Windows.Controls.Primitives.Selector)sender).SelectedItem).Name);
            //BookDto book = books.SingleOrDefault();
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
                        txtNotificator.Text ="There was a minor error, the shown chapters might not be completely accurate.";
                    }
                var p = root.Descendants()
                    .Where(n => n.GetAttributeValue("id", "") == "primary" && n.Name == "div")
                    .Single()
                    .Descendants()
                    .Where(n => n.Name.Equals("a") && 
                        (n.GetAttributeValue("href", "").ToLower().Contains("-chapter-") || n.InnerText.ToLower().Contains("chapter")))
                    .ToList();
                //Undefeated God of War
                if (p.Count < 4)
                {
                    p = SearchIndex(root);
                }
                for (int i = 0; i < p.Count(); i++)
                {
                    chapters.Add(new ChapterDto
                    {
                        ChapterId = i + 1,
                        ChapterUrl = p.ElementAt(i).GetAttributeValue("href", ""),
                        DisplayName = HttpUtility.HtmlDecode(p.ElementAt(i).InnerText)
                    });
                }

                chapterList.ItemsSource = chapters;
            }
            catch (WebException w)
            {
                txtNotificator.Text = w.Message;
            }
            
            
            //foreach (var item in chapters)
            //{
            //    await client.PostAsync(item);
            //}

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
            try
            {
                //Add the css style-sheet inline
                content.AddRange(System.IO.File.ReadAllLines(Environment.CurrentDirectory + "\\styles"));
            }
            catch (System.IO.IOException)
            {
                txtNotificator.Dispatcher.Invoke(new FailReadingStylesCallback(FailReadingStyles));
            }
            
            var arguments = e.Argument as List<object>;
            var chapters = (List<ChapterDto>)arguments[0];
            var firstChapter = chapters[0];
            
            ChapterDto lastChapter = new ChapterDto();
            try
            {
                int i = 0;
                foreach (ChapterDto item in chapters)
                {
                    lastChapter = item;
                    content.Add(addSite(item));
                    i++;
                    (sender as BackgroundWorker).ReportProgress(i*100/chapters.Count);
                }
            }
            catch (InvalidCastException)
            {
                MessageBox.Show("Some chapters might not be displayed properly");
            }
            //Get the default/saved path for the file to be saved at
            string path = (Properties.Settings.Default["Path"].Equals("")) 
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : (string)Properties.Settings.Default["Path"];

            //Get the book, to name the site accordingly
            BookDto book = (BookDto)arguments[1];
            
            if (chapters.Count > 1)
                path = path + "\\" + book.Name + "-Chapters-" + firstChapter.ChapterId + "-" + lastChapter.ChapterId + ".html";
            else
                path = path + "\\" + book.Name + "-Chapter-" + firstChapter.ChapterId + ".html";
            System.IO.File.WriteAllLines(path, content);
            txtNotificator.Dispatcher.Invoke(new UpdateProgressBarCallback(UpdateProgress),
                    new object[] { path });
        }

        private string addSite(ChapterDto item)
        {
            string s = "";
            var root = switchSite(item.ChapterUrl);
            HtmlNode p;
            //Tries to find the main part of the chapter
            try
            {
                p = root.Descendants()
                .Where(n => n.GetAttributeValue("class", "").Contains("entry-content"))
                .First();
            }
            catch (Exception)
            {
                try
                {
                    p = root.Descendants()
                    .Where(n => n.GetAttributeValue("class", "").Contains("main") || n.GetAttributeValue("id", "").Contains("main"))
                    .First();
                }
                catch (Exception)
                {
                    p = new HtmlDocument().DocumentNode;
                }
            }
            //Remove comments
            try
            {
                root.Descendants().Where(n => n.GetAttributeValue("id", "").ToLower().Contains("comments")).First().Remove();
            }
            catch { }
            //Remove links to other chapters including the ToC
            try
            {
                var a = p.Descendants()
                .Where(n => n.Name == "a" &&
                    (n.InnerText.ToLower().Contains("next") || n.InnerText.ToLower().Contains("previous") ||
                    n.InnerText.ToLower().Contains("table of content") || n.InnerText.ToLower().Contains("index")))
                .ToList();
                for (int k = 0; k < a.Count(); k++)
                {
                    a[k].Remove();
                }
            }
            catch { }
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
                    p.RemoveChild(p.Descendants().Where(n => n.GetAttributeValue("class", "").ToLower().Contains("sharedaddy")).First());
                }
            }
            catch { }
            //Remove ads on WuxiaWorld
            if (item.ChapterUrl.ToLower().Contains("wuxiaworld"))
            {
                p.LastChild.Remove();
                p.LastChild.Remove();
            }
            //Adds the Html to the list if the content is not empty
            if (p.LastChild != null)
            {
                s = p.InnerHtml;
            }

            return encodeString(s);
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
        private delegate void FailReadingStylesCallback();

        private delegate void UpdateItemsCallback(List<ChapterDto> c);
        private void getLatestChapters(List<ChapterDto> chapters)
        {
            bool success=false;
            var root = new HtmlDocument().DocumentNode;
            //Remove bad links to get to the latest one that is working
            while (!success)
            {
                try
                {
                    root = switchSite(chapters.Last().ChapterUrl);
                    success = true;
                }
                catch (Exception)
                {
                    chapters.Remove(chapters.Last());
                }
            }
            HtmlNode next;
            try
            {
                next = nextChapter(root);
                root = changeSite(root);
            }
            catch (Exception)
            {
                if(chapters.ElementAt(chapters.Count-1).DisplayName=="")
                    chapters.RemoveAt(chapters.Count - 1);
                chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
                new object[] { chapters });
                return;
            }
            
            bool stop = false;
            
            //Continues to read the next chapter links and adds those chapters to the chapter list
            while (!stop)
            {
                try
                {
                    chapters.Add(new ChapterDto
                    {
                        ChapterUrl = next.GetAttributeValue("href",""),
                        ChapterId = chapters.Count-1,
                        DisplayName = root.Descendants().Where(n => n.Name == "h1" && n.InnerText.ToLower().Contains("chapter")).First().InnerText
                    });
                    next = nextChapter(root);
                    root = changeSite(root);

                }
                catch (InvalidOperationException)
                {
                    stop = true;
                    chapterList.Dispatcher.Invoke(new UpdateItemsCallback(this.UpdateItems),
                    new object[] { chapters });
                    
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

        private void FailReadingStyles()
        {
            txtNotificator.Text = "The style template was not found!";
        }
        private void UpdateItems(List<ChapterDto> c)
        {
            chapterList.ItemsSource = c;
            txtNotificator.Text = "";
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
                timer.Dispose();
            };
            downloadProgress.Visibility = Visibility.Collapsed;
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
            ICollectionView view = CollectionViewSource.GetDefaultView(listOfBooks);
            view.Filter = CustomFilter;
            bookList.ItemsSource = view;
        }
        private bool CustomFilter(object item)
        {
            return ((BookDto)item).Name.ToLower().Contains(searchBox.Text.ToLower());
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
            //Unselect the current book to prevent issues later on when naming a file
            bookList.SelectedItem = null;

            List<ChapterDto> chapters = new List<ChapterDto>();
            try
            {
                root.Descendants().Where(n => n.GetAttributeValue("id", "").ToLower().Contains("comments")).First().Remove();
            }
            catch { }
            List<HtmlNode> p = new List<HtmlNode>();
            //Get all the links that could be chapters
            try
            {
                p = root.Descendants()
                    .Where(n => n.GetAttributeValue("role", "") == "main"|| n.GetAttributeValue("id", "") == "main"
                            || n.GetAttributeValue("class", "") == "main")
                     .First()
                    .Descendants()
                    .Where(n => n.Name.Equals("a") && !n.InnerHtml.Contains(">") && !n.InnerHtml.Contains("<"))
                    .ToList();
            }
            catch {
                txtNotificator.Text = "No chapters found";
            }
            List<string> urlList = new List<string>();
            //Add all the potential chapters to the chapterlist
            for (int i = 0; i < p.Count(); i++)
            {
                
                if (p.ElementAt(i).GetAttributeValue("href", "") != "" && p.ElementAt(i).InnerText != "")
                {
                    if (!urlList.Contains(p.ElementAt(i).GetAttributeValue("href", "")))
                    {
                        urlList.Add(p.ElementAt(i).GetAttributeValue("href", ""));
                        chapters.Add(new ChapterDto
                        {
                            ChapterId = i + 1,
                            ChapterUrl = p.ElementAt(i).GetAttributeValue("href", ""),
                            DisplayName = HttpUtility.HtmlDecode(p.ElementAt(i).InnerText)
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
            chapterList.ItemsSource = chapters;
        }
    }
}
