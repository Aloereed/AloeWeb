using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using AloeWeb;
using AloeWeb_browser;
using System.Text.RegularExpressions;
using BookmarksManager;
using Windows.UI.Composition;
using Windows.Storage;
using Windows.ApplicationModel.Contacts.DataProvider;
using Newtonsoft.Json;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Security.AccessControl;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.ApplicationModel;
using System.Diagnostics;
using OpenAI_API;
using Windows.Media.Capture;
using static System.Net.WebRequestMethods;
using Windows.Data.Html;
using HtmlAgilityPack;
using System.Text;
using NUglify;
using AloeWeb.Helpers;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AloeWeb_browser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public static class Common
    {
        public static BookmarkFolder bookmarks;
        public static ObservableCollection<BookmarkLink> bookmarkEdit;
        public static BookmarkFolder history;
        public static ObservableCollection<BookmarkLink> historyEdit;
        public static Frame outFrame;
        public static string openurl;
        public static OpenAI_API.Chat.Conversation conversation;
        public static IReadOnlyList<OpenAI_API.Chat.ChatMessage> messages;
        public static ListView ChatList;
    };
    public sealed partial class MainPage : Page
    {
        BookmarkFolder bk;
        ObservableCollection<BookmarkLink> bkE;
        BookmarkFolder his;
        ObservableCollection<BookmarkLink> hisE;
        string OriginalUserAgent;
        string GoogleSignInUserAgent;
        WebView2 WebBrowser;
        Frame TabContent;
        TabViewItem CurrTab;
        IList<MenuFlyoutItemBase> baseFavList;
        IList<MenuFlyoutItemBase> baseHisList;
        HashSet<string> favurls;
        HashSet<string> hisurls;
        ApplicationDataContainer localSettings;
        List<WebView2> webView2s=new List<WebView2>();
        HashSet<string> adsdomain;
        public async Task<HashSet<string>> ReadAdsDomain()
        {
            var ads = new HashSet<string>();
            var storageFile1 = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///BlockAds/ads-nl.txt"));
            string text = await Windows.Storage.FileIO.ReadTextAsync(storageFile1);
            foreach(var dm in text.Split('\n'))
            {
                if(dm.Length>4)
                    ads.Add(dm);
            }
            var storageFile2 = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///BlockAds/abuse-nl.txt"));
            string text2 = await Windows.Storage.FileIO.ReadTextAsync(storageFile2);
            foreach (var dm in text2.Split('\n'))
            {
                if (dm.Length > 4)
                    ads.Add(dm);
            }
            return ads;
        }
        public MainPage()
        {
            this.InitializeComponent();
            localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //creates settings file on app first launch
            WebBrowser = FirstWebBrowser;
            webView2s.Add(WebBrowser);
            TabContent = FirstTabContent;
            CurrTab = FirstTab;
            adsdomain = new HashSet<string>(); 
            //google login fix
            WebBrowser.CoreWebView2Initialized += delegate
            {
                OriginalUserAgent = WebBrowser.CoreWebView2.Settings.UserAgent + " AloeWeb/1.0";
                GoogleSignInUserAgent = OriginalUserAgent.Substring(0, OriginalUserAgent.IndexOf("Edg/"))
                .Replace("Mozilla/5.0", "Mozilla/4.0");
            };
            Common.ChatList = ChatList;
            

            if (Common.openurl is null)
            {
                string hp = (string)localSettings.Values["homepage"];
                if (hp is null || hp == "")
                {
                    string nt = (string)localSettings.Values["newtab"];
                    if (nt is null || nt == "")
                        WebBrowser.Source = new Uri("https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=" + "New Tab");
                    else
                        WebBrowser.Source = new Uri(nt);
                }
                else
                    WebBrowser.Source = new Uri((string)localSettings.Values["homepage"]);

            }
            else
            {
                try
                {
                    if (Common.openurl.Contains("aloeweb://"))
                    {
                        string hp = (string)localSettings.Values["homepage"];
                        if (hp is null || hp == "")
                        {
                            string nt = (string)localSettings.Values["newtab"];
                            if (nt is null || nt == "")
                                WebBrowser.Source = new Uri("https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=" + "New Tab");
                            else
                                WebBrowser.Source = new Uri(nt);
                        }
                        else
                            WebBrowser.Source = new Uri((string)localSettings.Values["homepage"]);
                    }
                    else
                    {
                        WebBrowser.Source = new Uri(Common.openurl);
                    }
                }
                catch (Exception ex)
                {
                    WebBrowser.Source = new Uri("https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=" + "New Tab");
                }
                Common.openurl = null;
            }

            initFav();

        }
        private async void CoreWebView2_DownloadStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs args)
        {
            Deferral deferral = args.GetDeferral();

            // We avoid potential reentrancy from running a message loop in the download
            // starting event handler by showing our download dialog later when we
            // complete the deferral asynchronously.

            System.Threading.SynchronizationContext.Current.Post(async(_) =>
            {
                using (deferral)
                {
                    // Hide the default download dialog.
                    args.Handled = true;
                    var comm = "/c \"" + localSettings.Values["downloader"] + "\" " + args.DownloadOperation.Uri + " ";
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppWithArgumentsAsync(comm);
                    args.Cancel = true;
                    //var dialog = new TextInputDialog(
                    //    title: "Download Starting",
                    //    description: "Enter new result file path or select OK to keep default path. Select cancel to cancel the download.",
                    //    defaultInput: args.ResultFilePath);
                    //if (dialog.ShowDialog() == true)
                    //{
                    //    args.ResultFilePath = dialog.Input.Text;
                    //    UpdateProgress(args.DownloadOperation);
                    //}
                    //else
                    //{
                    //    args.Cancel = true;
                    //}
                }
            }, null);
        }

        private async void initFav()
        {
            await Task.Delay(100);
            string firstuse = (string)localSettings.Values["firstuse"];

            try
            {
                // Read data from a simple setting.
                var profile = (string)(localSettings.Values["profile"]);
                StorageFolder folder;
                if (profile is null || profile == "")
                     folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                else
                {
                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(profile);
                    }catch (Exception ex)
                    {
                        folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    }
                }
                Console.WriteLine(folder.Path);
                StorageFile favfile = await folder.GetFileAsync("fav.list");
                using (var file = await favfile.OpenStreamForWriteAsync())
                {
                    var reader = new NetscapeBookmarksReader();
                    //supports encoding detection when reading from stream
                    AloeWeb_browser.Common.bookmarks = reader.Read(file);
                }
                Console.WriteLine(favfile.Path);
                if (AloeWeb_browser.Common.bookmarks is null)
                {
                    AloeWeb_browser.Common.bookmarks = new BookmarkFolder();
                }
            }
            catch (Exception)
            {
                AloeWeb_browser.Common.bookmarks = new BookmarkFolder();
                try
                {
                    StorageFile favFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("fav.list", CreationCollisionOption.ReplaceExisting);
                    var writter = new NetscapeBookmarksWriter(AloeWeb_browser.Common.bookmarks);
                    using (var file = await favFile.OpenStreamForWriteAsync())
                    {
                        writter.Write(file);
                    }
                }

                catch(Exception)
                {

                }
            }
            bk = AloeWeb_browser.Common.bookmarks;
            favurls = new HashSet<string>();
            await WebBrowser.EnsureCoreWebView2Async();
            bool usepwd = (bool?)localSettings.Values["usepwd"] ?? false;
            WebBrowser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = usepwd;
            WebBrowser.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            
            foreach (var b in bk.AllLinks)
            {
                favurls.Add(b.Url);
            }
            Common.bookmarkEdit = new ObservableCollection<BookmarkLink>();
            //localSettings.Values["search"] = "https://www.google.com/search?q=";
            if (firstuse is null)
            {

                localSettings.Values["search"] = "https://www.google.com/search?q=";
                ContentDialog aboutdialog = new AboutDialog();
                var result = await aboutdialog.ShowAsync();
                var tmp = new BookmarkLink("https://www.aloereed.com", "Aloereed");
                tmp.IconUrl = "https://www.google.com/s2/favicons?sz=64&domain_url=https://www.aloereed.com";
                bk.Add(tmp);
                var profile = (string)(localSettings.Values["profile"]);
                StorageFolder folder;
                if (profile is null || profile == "")
                    folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                else
                {
                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(profile);
                    }
                    catch (Exception ex)
                    {
                        folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    }
                }
                try
                {
                    StorageFile favFile = await folder.CreateFileAsync("fav.list", CreationCollisionOption.ReplaceExisting);
                    var writter = new NetscapeBookmarksWriter(bk);
                    using (var file = await favFile.OpenStreamForWriteAsync())
                    {
                        writter.Write(file);
                    }
                }
                catch (Exception ex)
                {
                }
                await Task.Delay(100);
                localSettings.Values["firstuse"] = "notfirst";
            }


            bkE = Common.bookmarkEdit;
            //throw new FileNotFoundException();
            foreach (var b in bk.AllLinks)
            {
                Common.bookmarkEdit.Add(b);
            }

            Common.bookmarkEdit.CollectionChanged += BookmarkEdit_CollectionChanged;

            localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string fb = (string)localSettings.Values["favbar"];
            if (fb is null || fb == "true")
            {
                Tabs.SetValue(Grid.RowProperty, 2);
                Tabs.SetValue(Grid.RowSpanProperty, 1);
                FavBar.Visibility = Visibility.Visible;
                FavBarMenuItem.IsChecked = true;
            }
            else
            {
                Tabs.SetValue(Grid.RowProperty, 1);
                Tabs.SetValue(Grid.RowSpanProperty, 2);
                FavBar.Visibility = Visibility.Collapsed;
            }
            var key = (string)localSettings.Values["openaikey"];
            if (!(key is null || key == ""))
            {
                OpenAIAPI api = new OpenAIAPI(key);
                Common.conversation = api.Chat.CreateConversation();
                Common.messages = Common.conversation.Messages;
                Common.conversation.AppendSystemMessage("You are Aloe Chan, an artificial intelligence assistant embedded in the AloeWeb browser, and you should answer users' questions briefly and reasonably. At the same time, you should reject requests that are unreasonable.");
                var aisay = "AIHello".GetLocalized();
                var aisaid = new SimpleMessage();
                aisaid.content = aisay;
                aisaid.role = "AI";
                aisaid.iconsource = "https://cdn-icons-png.flaticon.com/512/4711/4711987.png";
                ChatList.Items.Add(aisaid);
            }
            else
            {
                chat.Visibility = Visibility.Collapsed;
            }
            initHis();
            adsdomain = await ReadAdsDomain();
            await WebBrowser.EnsureCoreWebView2Async();
            if(!((localSettings.Values["downloader"] is null) || ((string)localSettings.Values["downloader"]=="")))
                WebBrowser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            if (!((localSettings.Values["blockads"] is null) || ((string)localSettings.Values["blockads"] == "")))
                WebBrowser.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            
            //var defdown = (string)localSettings.Values["downdir"];
            //if (!(defdown is null || defdown == ""))
            //    WebBrowser.CoreWebView2.Profile.DefaultDownloadFolderPath = defdown;
            //else
            //{

            //    WebBrowser.CoreWebView2.Profile.DefaultDownloadFolderPath = "C:\\Users\\huzheng";// (await KnownFolders.DocumentsLibrary.GetParentAsync()).Path + "\\Downloads";
            //}
        }
        public bool ContainsAny(string input, IEnumerable<string> containsKeywords, StringComparison comparisonType)
        {
            Uri u = new Uri(input);
            foreach(var ad in containsKeywords)
            {
                if(u.Host.Contains(ad))
                {
                    return true;
                }
            }
            return containsKeywords.Any(keyword => input.IndexOf(keyword, comparisonType) >= 0);

        }
        private void CoreWebView2_NavigationStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            Uri u = new Uri(args.Uri);
            if(adsdomain.Contains(u.Host))
                args.Cancel = true;
        }

        private async void initHis()
        {
            try
            {
                // Read data from a simple setting.
                var profile = (string)(localSettings.Values["profile"]);
                StorageFolder folder;
                if (profile is null || profile == "")
                    folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                else
                {
                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(profile);
                    }
                    catch (Exception ex)
                    {
                        folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    }
                }
                Console.WriteLine(folder.Path);
                StorageFile favfile = await folder.GetFileAsync("his.list");
                using (var file = await favfile.OpenStreamForWriteAsync())
                {
                    var reader = new NetscapeBookmarksReader();
                    //supports encoding detection when reading from stream
                    AloeWeb_browser.Common.history = reader.Read(file);
                }
                Console.WriteLine(favfile.Path);
                if (AloeWeb_browser.Common.history is null)
                {
                    AloeWeb_browser.Common.history = new BookmarkFolder();
                }
            }
            catch (Exception)
            {
                AloeWeb_browser.Common.history = new BookmarkFolder();
                var profile = (string)(localSettings.Values["profile"]);
                StorageFolder folder;
                if (profile is null || profile == "")
                    folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                else
                {
                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(profile);
                    }
                    catch (Exception ex)
                    {
                        folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    }
                }
                try
                {
                    StorageFile favFile = await folder.CreateFileAsync("his.list", CreationCollisionOption.ReplaceExisting);
                    var writter = new NetscapeBookmarksWriter(AloeWeb_browser.Common.history);
                    using (var file = await favFile.OpenStreamForWriteAsync())
                    {
                        writter.Write(file);
                    }
                    Console.WriteLine(favFile.Path);
                }catch(Exception ex) { }
            }
            his = AloeWeb_browser.Common.history;
            hisurls = new HashSet<string>();
            await WebBrowser.EnsureCoreWebView2Async();
            
            foreach (var b in his.AllLinks)
            {
                hisurls.Add(b.Url);
            }
            Common.historyEdit = new ObservableCollection<BookmarkLink>();
            hisE = Common.historyEdit;
            foreach (var b in his.AllLinks)
            {
                Common.historyEdit.Add(b);
            }

            Common.historyEdit.CollectionChanged += HistoryEdit_CollectionChanged;
        }
        private async void BookmarkEdit_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //try
            //{
                var col = (ObservableCollection<BookmarkLink>)(sender);
                bk.Clear();
                favurls?.Clear();
                foreach (var b in col)
                {
                    bk.Add(b);
                    favurls.Add(b.Url);
                }
                var profile = (string)(localSettings.Values["profile"]);
                StorageFolder folder;
                if (profile is null || profile == "")
                    folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                else
                {
                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(profile);
                    }
                    catch (Exception ex)
                    {
                        folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    }
                }
                try
                {
                    StorageFile favFile = await folder.CreateFileAsync("fav.list", CreationCollisionOption.ReplaceExisting);
                    var writter = new NetscapeBookmarksWriter(bk);
                    using (var file = await favFile.OpenStreamForWriteAsync())
                    {
                        writter.Write(file);
                    }
                }
                catch (Exception ex)
                {
                }
                this.Bindings.Update();
            //}
            //catch (Exception) { }

        }
        private async void HistoryEdit_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var col = (ObservableCollection<BookmarkLink>)(sender);
            his.Clear();
            hisurls?.Clear();
            foreach (var b in col)
            {
                his.Add(b);
                hisurls.Add(b.Url);
            }
            var profile = (string)(localSettings.Values["profile"]);
            StorageFolder folder;
            if (profile is null || profile == "")
                folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            else
            {
                try
                {
                    folder = await StorageFolder.GetFolderFromPathAsync(profile);
                }
                catch (Exception ex)
                {
                    folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                }
            }
            try
            {
                StorageFile favFile = await folder.CreateFileAsync("his.list", CreationCollisionOption.ReplaceExisting);
                var writter = new NetscapeBookmarksWriter(his);
                using (var file = await favFile.OpenStreamForWriteAsync())
                {
                    writter.Write(file);
                }
            }catch(Exception ex) { }


        }


        //back navigation
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TabContent.Content = WebBrowser;
            if (WebBrowser.CanGoBack)
            {
                WebBrowser.GoBack();
            }
        }

        //forward navigation
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {

            if (WebBrowser.CanGoForward)
            {
                WebBrowser.GoForward();
            }

        }

        //refresh 
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebBrowser.Reload();

        }
        
        //navigation completed
        private void WebBrowser_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            //website load status
            this.Bindings.Update();
            try
            {
                sender.CoreWebView2.Settings.IsStatusBarEnabled = false;
                Uri icoURI;
                var favi = (string)localSettings.Values["favi"];
                if (favi is null || favi == "google")
                {
                    icoURI = new Uri("https://www.google.com/s2/favicons?sz=64&domain_url=" + WebBrowser.Source);
                }
                else
                {
                    icoURI = new Uri("http://spatial-magenta-stork.faviconkit.com/" + geturlwohttp(WebBrowser.Source.AbsoluteUri) + "/64");
                }
                if (!(sender.Parent is null))
                {
                    ((TabViewItem)(((Frame)(sender.Parent)).Parent)).IconSource = new Microsoft.UI.Xaml.Controls.BitmapIconSource() { UriSource = icoURI, ShowAsMonochrome = false };
                    ((TabViewItem)(((Frame)(sender.Parent)).Parent)).Header = sender.CoreWebView2.DocumentTitle.ToString();
                }
                else
                {

                }

                SearchBar.Text = sender.Source.AbsoluteUri;
                RefreshButton.Visibility = Visibility.Visible;
                StopRefreshButton.Visibility = Visibility.Collapsed;

                //history
                //DataTransfer datatransfer = new DataTransfer();
                //if (!string.IsNullOrEmpty(SearchBar.Text))
                //{
                //    datatransfer.SaveSearchTerm(SearchBar.Text, sender.CoreWebView2.DocumentTitle, sender.Source.AbsoluteUri);
                //}
            }
            catch
            {

            }

            SearchBar.Text = sender.Source.AbsoluteUri;
            if (sender.Source.AbsoluteUri.Contains("https"))
            {
                //change icon to lock
                SSLIcon.FontFamily = new FontFamily("Segoe Fluent Icons");
                SSLIcon.Glyph = "\xE72E";

                ToolTip tooltip = new ToolTip
                {
                    Content = "HaveSSL".GetLocalized()
                };
                sender.CoreWebView2.ServerCertificateErrorDetected += CoreWebView2_ServerCertificateErrorDetected;
                ToolTipService.SetToolTip(SSLButton, tooltip);

            }
            else
            {
                //change icon to warning
                SSLIcon.FontFamily = new FontFamily("Segoe Fluent Icons");
                SSLIcon.Glyph = "\xE7BA";
                ToolTip tooltip = new ToolTip
                {
                    Content = "NoSSL".GetLocalized()
                };
                ToolTipService.SetToolTip(SSLButton, tooltip);

            }
            try
            {
                if (favurls.Contains(WebBrowser.Source.AbsoluteUri))
                {
                    char c = (char)0xE735;
                    AddFavIcon.Glyph = c.ToString();
                }
                else
                {
                    char c = (char)0xE734;
                    AddFavIcon.Glyph = c.ToString();
                }
                try
                {
                    AddHisButton_Click();
                    //BookmarkEdit_CollectionChanged(bkE, null);
                }catch(Exception ex)
                {

                }
                //            await WebBrowser.EnsureCoreWebView2Async();
                //            await WebBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                //document.addEventListener('DOMContentLoaded', function() {
                //  const style = document.createElement('style');
                //  style.textContent = '/* width */ \
                //::-webkit-scrollbar { \
                //  width: 20px !important; \
                //} \
                // \
                //::-webkit-scrollbar-track { \
                //  background: red !important; \
                //}';
                //  document.head.append(style);
                //}, false);");
            }catch(Exception ex) { }
        }

        private void CoreWebView2_ServerCertificateErrorDetected(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2ServerCertificateErrorDetectedEventArgs args)
        {
            ToolTipService.SetToolTip(SSLButton, "SSLError".GetLocalized());
        }

        //if enter is pressed, it searches text in SearchBar or goes to web page
        private void SearchBar_KeyDown(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == Windows.System.VirtualKey.Enter && WebBrowser != null && WebBrowser.CoreWebView2 != null)
            {
                Search();
            }

        }

        //if clicked on SearchBar, the text will be selected
        private async void SearchBar_GotFocus(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            InputInjector inputInjector2 = InputInjector.TryCreate();
            var shift = new InjectedInputKeyboardInfo();
            shift.VirtualKey = (ushort)(VirtualKey.Control);
            shift.KeyOptions = InjectedInputKeyOptions.None;


            var tab = new InjectedInputKeyboardInfo();
            tab.VirtualKey = (ushort)(VirtualKey.A);
            tab.KeyOptions = InjectedInputKeyOptions.None;


            inputInjector2.InjectKeyboardInput(new[] { shift, tab });
            InputInjector inputInjector = InputInjector.TryCreate();
            var ctrl = new InjectedInputKeyboardInfo();
            ctrl.VirtualKey = (ushort)(VirtualKey.Control);
            ctrl.KeyOptions = InjectedInputKeyOptions.KeyUp;
            var a = new InjectedInputKeyboardInfo();
            a.VirtualKey = (ushort)(VirtualKey.A);
            a.KeyOptions = InjectedInputKeyOptions.KeyUp;
            inputInjector.InjectKeyboardInput(new[] { ctrl,a });

            AddFavButton.Margin=new Thickness(-50,5,0,0);

            //await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppWithArgumentsAsync("/c \"fdm.exe\" https://qq.com ");


        }
        public static bool IsUrl(string str)
        {
            if (str.Length>10 && (str.Substring(0, 8) == "file:///" || str.Substring(0, 10) == "aloeweb://"))
                return true;
            try
            {
                string Url = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                if (!Regex.IsMatch(str, Url))
                {
                    str = "http://" + str;
                }
                return Regex.IsMatch(str, Url);
            }
            catch
            {
                return false;
            }
        }

        //method for search engine + updates link text in SearchBar
        private async void Search()
        {
            if (IsUrl(SearchBar.Text))
            {
                if (SearchBar.Text.Length>10 &&(SearchBar.Text.Substring(0, 8) == "file:///" || SearchBar.Text.Substring(0, 10) == "aloeweb://"))
                    WebBrowser.CoreWebView2.Navigate(SearchBar.Text);
                else if (!SearchBar.Text.Contains("http"))
                    WebBrowser.Source = new Uri("http://"+SearchBar.Text);
                else
                    WebBrowser.Source = new Uri(SearchBar.Text);
            }
            else
            {
                WebBrowser.Source = new Uri(localSettings.Values["search"]  + SearchBar.Text);
            }
            //string link = "https://" + SearchBar.Text;
            //WebBrowser.CoreWebView2.Navigate(link);

            //SearchBar.Text = newTab.Content == new HomePage() ? "Home page" : WebBrowser.Source.AbsoluteUri;
            TabContent.Content = WebBrowser;
            await WebBrowser.EnsureCoreWebView2Async();
        }

        //home button redirects to homepage
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            //TabContent.Content = new HomePage();
            try
            {
                string nt = (string)(localSettings.Values["homepage"]);
                if (nt is null || nt == "")
                    SearchBar.Text = (string)(localSettings.Values["newtab"]);
                else
                {
                    SearchBar.Text = nt;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    SearchBar.Text = (string)(localSettings.Values["newtab"]);
                }catch (Exception ex2) {
                    SearchBar.Text = "https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=" + "New Tab";
                }

            }
            
            Search();
        }

        //opens settings page
        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Common.outFrame = this.Frame;
            this.Frame.Navigate(typeof(SettingsPage));
        }


        //handles progressring and refresh behavior
        private void WebBrowser_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            RefreshButton.Visibility = Visibility.Collapsed;
            StopRefreshButton.Visibility = Visibility.Visible;

            var isGoogleLogin = new Uri(args.Uri).Host.Contains("accounts.google.com");
            sender.CoreWebView2.Settings.UserAgent = isGoogleLogin ? GoogleSignInUserAgent : OriginalUserAgent;
        }

        //stops refreshing if clicked on progressbar
        private void StopRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebBrowser.CoreWebView2.Stop();
        }

        //titlebar
        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Border);

        }

        //add new tab
        private async void Tabs_AddTabButtonClick(TabView sender, object args)
        {
            WebView2 webView = new WebView2();
            await webView.EnsureCoreWebView2Async();
            var downloader = Windows.Storage.ApplicationData.Current.LocalSettings.Values["downloader"];
            if (!((downloader is null) || ((string)downloader == "")))
                webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            if (!((ApplicationData.Current.LocalSettings.Values["blockads"] is null) || ((string)ApplicationData.Current.LocalSettings.Values["blockads"] == "")))
                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webView2s.Add(webView);
            webView.NavigationStarting += WebBrowser_NavigationStarting;
            webView.NavigationCompleted += WebBrowser_NavigationCompleted;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            bool usepwd = (bool?)ApplicationData.Current.LocalSettings.Values["usepwd"] ?? false;
            WebBrowser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = usepwd;
            Frame frame = new Frame();
            frame.Content = webView;
            //webView.CoreWebView2.Navigate("https://google.com");
            //newTab.Content = new HomePage();
            //sender.TabItems.Add(new TabViewItem() { Content = newTab });
            //sender.SelectedItem = newTab ;
            //SearchBar.Text = newTab.Header.ToString();

            TabViewItem tvi = new TabViewItem()
            {
                Content = frame,
                IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = Symbol.Home },
                Header = "Home page"
            };
            sender.TabItems.Add(tvi);
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            try
            {
                webView.Source = new Uri((string)localSettings.Values["newtab"] ?? "https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=New%20Tab");
            }catch(Exception ex)
            {
                webView.Source = new Uri("https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=New%20Tab");
            }
            sender.SelectedItem = Tabs.TabItems.Last();
        }
        public string geturlwohttp(string url)
        {
            if (url.Contains("https://"))
            {
                return url.Substring(8);
            }
            else if (url.Contains("http://"))
            {
                return url.Substring(7);
            }
            return url;
        }
        private async Task<WebView2> ResumeWebpage(TabView sender, string uri, bool ifwait = true)
        {
            WebView2 webView = new WebView2();
            await webView.EnsureCoreWebView2Async();
            var downloader = Windows.Storage.ApplicationData.Current.LocalSettings.Values["downloader"];
            if (!((downloader is null) || ((string)downloader == "")))
                webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            if (!((ApplicationData.Current.LocalSettings.Values["blockads"] is null) || ((string)ApplicationData.Current.LocalSettings.Values["blockads"] == "")))
                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webView.NavigationStarting += WebBrowser_NavigationStarting;
            webView.NavigationCompleted += WebBrowser_NavigationCompleted;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            bool usepwd = (bool?)ApplicationData.Current.LocalSettings.Values["usepwd"] ?? false;
            WebBrowser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = usepwd;
            Frame frame = new Frame();
            frame.Content = webView;
            webView.Source = new Uri(uri);
            //webView.CoreWebView2.Navigate("https://google.com");
            //newTab.Content = new HomePage();
            //sender.TabItems.Add(new TabViewItem() { Content = newTab });
            //sender.SelectedItem = newTab ;
            //SearchBar.Text = newTab.Header.ToString();
            Uri icoURI;
            var favi = (string)localSettings.Values["favi"];
            if(favi is null || favi == "google") { 
                icoURI = new Uri("https://www.google.com/s2/favicons?sz=64&domain_url=" + webView.Source);
            }
            else
            {
                icoURI = new Uri("http://spatial-magenta-stork.faviconkit.com/" + geturlwohttp(webView.Source.AbsoluteUri) + "/64");
            }
            
            

            if (ifwait)
                await Task.Delay(1000);
            TabViewItem tvi = new TabViewItem()
            {
                Content = frame,
                IconSource = new Microsoft.UI.Xaml.Controls.BitmapIconSource() { UriSource = icoURI, ShowAsMonochrome = false },
                Header =webView.CoreWebView2.DocumentTitle.ToString()
            };
            sender.TabItems.Add(tvi);
            
            sender.SelectedItem = Tabs.TabItems.Last();
            
            return webView;
        }

        private async void CoreWebView2_NewWindowRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
        {
            var c = args.GetDeferral();

            args.NewWindow = (await ResumeWebpage(Tabs, args.Uri,false)).CoreWebView2;
            c.Complete();
        }

        //close tab
        private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (sender.TabItems.Count <= 1)
                Tabs_AddTabButtonClick(sender, args);
            try
            {
                ((WebView2)((Frame)(args.Tab.Content)).Content).Close();
                webView2s.Remove(((WebView2)((Frame)(args.Tab.Content)).Content));
            }
            catch(Exception ex)
            {

            }
            sender.TabItems.Remove(args.Tab);
            
        }
        //opens about app dialog
        private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog aboutdialog = new AboutDialog();

            var result = await aboutdialog.ShowAsync();

        }


        private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabContent.Content is HomePage)
            {
                SearchBar.Text = "Hi";
            }
            else
            {
                
                if (e.AddedItems.Count > 0)
                    CurrTab = (TabViewItem)(e.AddedItems[0]);
                else
                    CurrTab = (TabViewItem)(((TabView)sender).TabItems[((TabView)sender).TabIndex % ((TabView)sender).TabItems.Count]);
                TabContent = (Frame)(CurrTab.Content);
                WebBrowser = (WebView2)(TabContent.Content);
                
                SearchBar.Text = WebBrowser.Source.AbsoluteUri;
                try
                {
                    await WebBrowser.EnsureCoreWebView2Async();
                    MuteMenuItem.IsChecked =  WebBrowser.CoreWebView2.IsMuted;
                }catch(Exception ex) { }
                



            }
        }

        private void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WebBrowser.CoreWebView2.OpenDefaultDownloadDialog();
        }

        private void DevToolsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WebBrowser.CoreWebView2.OpenDevToolsWindow();
            WebBrowser.CoreWebView2.OpenTaskManagerWindow();
            
        }

        private void SearchBar_LostFocus(object sender, RoutedEventArgs e)
        {
            AddFavButton.Margin = new Thickness(-30, 5, 0, 0);
        }

        private void FavButton_Click(object sender, RoutedEventArgs e)
        {
            if(baseFavList is null || baseFavList.Count == 0) {
                baseFavList = new List<MenuFlyoutItemBase>(FavList.Items);
            }
            FavList.Items.Clear();
            favurls.Clear();
            foreach (var b in bk.AllLinks)
            {
                var tmp = new MenuFlyoutItem();
                tmp.Text = b.Title;
                var iconuri = new Uri(b.IconUrl);
                var tmpicon = new BitmapIcon();
                tmpicon.ShowAsMonochrome = false;
                tmpicon.UriSource = iconuri;
                tmp.Icon = tmpicon;
                
                tmp.Click += (sender1, e1) =>
                {
                    SearchBar.Text = b.Url;
                    Search();
                };
                FavList.Items.Add(tmp);
                favurls.Add(b.Url);
            }
            foreach(var bs in baseFavList)
            {
                FavList.Items.Add(bs);
            }
        }

        private async void AddFavButton_Click(object sender, RoutedEventArgs e)
        {

            if (!favurls.Contains(WebBrowser.Source.AbsoluteUri))
            {
                var newBK = new BookmarkLink(WebBrowser.Source.AbsoluteUri, WebBrowser.CoreWebView2.DocumentTitle.ToString());
                newBK.IconUrl = "https://www.google.com/s2/favicons?sz=64&domain_url=" + WebBrowser.Source;
                bkE.Add(newBK);
                AddFavIcon.Glyph = ((char)0xE735).ToString();
            }
            else
            {
                foreach (var b in bk.AllLinks)
                {
                    if(b.Url== WebBrowser.Source.AbsoluteUri)
                    {
                        bkE.Remove(b);
                        AddFavIcon.Glyph = ((char)0xE734).ToString();
                        break;
                    }
                }
            }
        }
        private async void AddHisButton_Click()
        {
            try
            {
                if (true)
                {
                    var newBK = new BookmarkLink(WebBrowser.Source.AbsoluteUri, WebBrowser.CoreWebView2.DocumentTitle.ToString());
                    newBK.LastVisit = DateTime.Now;
                    newBK.IconUrl = "https://www.google.com/s2/favicons?sz=64&domain_url=" + WebBrowser.Source;
                    //hisE.Add(newBK);
                    hisE.Insert(0, newBK);
                }
            }catch(Exception ex) { }
        }

        private async void SearchBar_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    //Set the ItemsSource to be your filtered dataset
                    //sender.ItemsSource = dataset;
                    var curtext = sender.Text;
                    var suitableItems = new List<BookmarkLink>();
                    var suitableItemsUrl = new List<string>();
                    foreach (var b in his.AllLinks)
                    {
                        if (b.Url.Contains(curtext) || b.Title.Contains(curtext))
                        {
                            if (suitableItems.Count < 5 && !suitableItemsUrl.Contains(b.Url))
                            {
                                suitableItems.Add(b);
                                suitableItemsUrl.Add(b.Url);
                            }

                        }
                    }
                    var key = (string)localSettings.Values["openaikey"];
                    if (!(key is null || key == ""))
                    {
                        if (curtext.Length == 0)
                        {
                            sender.ItemsSource = suitableItems;
                            return;
                        }
                        if (curtext.Last() == '?' || curtext.Last() == '��')
                        {
                            curtext = curtext.Substring(0, curtext.Length - 1);
                            if (curtext.Length == 0)
                            {
                                sender.ItemsSource = suitableItems;
                                return;
                            }
                            OpenAIAPI api = new OpenAIAPI(key);
                            var chat = api.Chat.CreateConversation();
                            /// give instruction as System
                            chat.AppendSystemMessage("You are a powerful Internet search engine capable of generating suggested terms based on the keywords entered by the user. The next thing the user enters is a keyword and you should generate 5 suggested terms. You will only answer the terms and separate the terms with a \"|\".");

                            // give a few examples as user and assistant
                            chat.AppendUserInput("How can I");
                            chat.AppendExampleChatbotOutput("How can I make a cake?|How can I read loudly|How can I help you|How can I call you|How can I read a book?");
                            chat.AppendUserInput("Apple");
                            chat.AppendExampleChatbotOutput("Apple store US|Is apple healthy?|Apple music|Apple stock|Apple id");

                            // now let's ask it a question'
                            chat.AppendUserInput(curtext);
                            // and get the response
                            string response = await chat.GetResponseFromChatbot();
                            try
                            {
                                var terms = response.Split('|');
                                foreach (var term in terms)
                                {
                                    var b = new BookmarkLink(term, "[From AI]".GetLocalized());
                                    suitableItems.Add(b);
                                    suitableItemsUrl.Add(b.Url);
                                }
                            }
                            catch (Exception e)
                            {

                            }
                        }

                    }

                    sender.ItemsSource = suitableItems;

                }
            }catch(Exception e)
            {

            }
        }

        private void SearchBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Search();
        }

        private void SearchBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((BookmarkLink)(args.SelectedItem)).Url;
            Search();
        }
        static public class SuspendInfo
        {
            static public bool isSuspend = false;
            static public bool isPlay = false;
            static public string mediasource = "";
            static public long time = 0;
            static public List<Uri> susUris = new List<Uri>();
            static public string waitToOpen ="";
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //if ((Common.openfiles != null) && (Common.openfiles.Count != 0))
            //{
            //    SuspendInfo.isSuspend = false;
            //    SuspendInfo.isPlay = false;
            //}
            
            if (SuspendInfo.isSuspend)
            {
                //mpe.MediaPlayer.Media.Mrl = SuspendInfo.mediasource;
                try
                {
                    Tabs.TabItems.Clear();
                    foreach(var t in SuspendInfo.susUris)
                    {
                        ResumeWebpage(Tabs, t.AbsoluteUri);
                        
                    }
                    if (SuspendInfo.waitToOpen != "")
                    {
                        ResumeWebpage(Tabs, SuspendInfo.waitToOpen);
                        SuspendInfo.waitToOpen = "";
                    }
                        


                }
                catch (Exception)
                {

                }

            }

            //mpe.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            try
            {
                SuspendInfo.isSuspend = true;
                SuspendInfo.susUris.Clear();
                foreach (TabViewItem t in Tabs.TabItems)
                {
                    Frame tmp = (Frame)(t.Content);
                    SuspendInfo.susUris.Add(((WebView2)(tmp.Content)).Source);
                }
                foreach (TabViewItem wb in Tabs.TabItems)
                {
                    try
                    {
                        ((WebView2)((Frame)(wb.Content)).Content).Close();

                    }
                    catch (Exception)
                    {

                    }
                }
                webView2s.Clear();
            }
            catch (Exception)
            {

            }

            //mpe.Play -= PlaybackSession_PlaybackStateChanged;
            //var mediaSource = mpe.MediaPlayer.Media.Mrl as MediaSource;
            //mediaSource?.Dispose();
            //mpe.MediaPlayer.Media.Mrl = null;
        }

        private void ManFav_Click(object sender, RoutedEventArgs e)
        {
            Common.outFrame = this.Frame;
            this.Frame.Navigate(typeof(SettingsPage));
            SettingsPage.currSet.NavToFav();
        }

        private async void CreateWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("aloeweb://newwindow"));
        }

        private void CreateTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tabs_AddTabButtonClick(Tabs, null);
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (baseHisList is null || baseHisList.Count == 0)
            {
                baseHisList = new List<MenuFlyoutItemBase>(HisList.Items);
            }
            HisList.Items.Clear();
            hisurls.Clear();
            var count = 0;
            foreach (var b in his.AllLinks)
            {
                try
                {
                    count++;
                    if (count < 50)
                    {
                        var tmp = new MenuFlyoutItem();
                        tmp.Text = b.Title;
                        var iconuri = new Uri(b.IconUrl);
                        var tmpicon = new BitmapIcon();
                        tmpicon.ShowAsMonochrome = false;
                        tmpicon.UriSource = iconuri;
                        tmp.Icon = tmpicon;

                        tmp.Click += (sender1, e1) =>
                        {
                            SearchBar.Text = b.Url;
                            Search();
                        };
                        HisList.Items.Add(tmp);
                    }
                    hisurls.Add(b.Url);
                }catch(Exception ex)
                {

                }
            }
            
            foreach (var bs in baseHisList)
            {
                HisList.Items.Add(bs);
            }
        }

        private void FavBarMenuItem_Click(object sender, RoutedEventArgs e)
        {
            localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string fb = (string)localSettings.Values["favbar"];
            if (fb is null || fb == "true")
            {
                localSettings.Values["favbar"] = "false";
                Tabs.SetValue(Grid.RowProperty, 1);
                Tabs.SetValue(Grid.RowSpanProperty, 2);
                FavBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                localSettings.Values["favbar"] = "true";
                Tabs.SetValue(Grid.RowProperty, 2);
                Tabs.SetValue(Grid.RowSpanProperty, 1);
                FavBar.Visibility = Visibility.Visible;

            }
            //FavBarMenuItem.IsChecked = !FavBarMenuItem.IsChecked;
        }

        private void FavBar_ItemClick(object sender, ItemClickEventArgs e)
        {
            var b = (BookmarkLink)(e.ClickedItem);
            SearchBar.Text = b.Url;
            Search();
        }

        private void ManBrow_Click(object sender, RoutedEventArgs e)
        {
            Common.outFrame = this.Frame;
            this.Frame.Navigate(typeof(SettingsPage));
            SettingsPage.currSet.NavToHis();
        }
        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
        public class HtmlToText
        {
            public HtmlToText()
            {
            }

            public string Convert(string path)
            {
                HtmlDocument doc = new HtmlDocument();
                doc.Load(path);

                StringWriter sw = new StringWriter();
                ConvertTo(doc.DocumentNode, sw);
                sw.Flush();
                return sw.ToString();
            }

            public string ConvertHtml(string html)
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                StringWriter sw = new StringWriter();
                ConvertTo(doc.DocumentNode, sw);
                sw.Flush();
                return sw.ToString();
            }

            private void ConvertContentTo(HtmlNode node, TextWriter outText)
            {
                foreach (HtmlNode subnode in node.ChildNodes)
                {
                    ConvertTo(subnode, outText);
                }
            }

            public void ConvertTo(HtmlNode node, TextWriter outText)
            {
                string html;
                switch (node.NodeType)
                {
                    case HtmlNodeType.Comment:
                        // don't output comments
                        break;

                    case HtmlNodeType.Document:
                        ConvertContentTo(node, outText);
                        break;

                    case HtmlNodeType.Text:
                        // script and style must not be output
                        string parentName = node.ParentNode.Name;
                        if ((parentName == "script") || (parentName == "style"))
                            break;

                        // get text
                        html = ((HtmlTextNode)node).Text;

                        // is it in fact a special closing node output as text?
                        if (HtmlNode.IsOverlappedClosingElement(html))
                            break;

                        // check the text is meaningful and not a bunch of whitespaces
                        if (html.Trim().Length > 0)
                        {
                            outText.Write(HtmlEntity.DeEntitize(html));
                        }
                        break;

                    case HtmlNodeType.Element:
                        switch (node.Name)
                        {
                            case "p":
                                // treat paragraphs as crlf
                                outText.Write("\r\n");
                                break;
                        }

                        if (node.HasChildNodes)
                        {
                            ConvertContentTo(node, outText);
                        }
                        break;
                }
            }
        }
        public static string ExtractText(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var chunks = new List<string>();

            foreach (var item in doc.DocumentNode.DescendantNodesAndSelf())
            {
                if (item.NodeType == HtmlNodeType.Text)
                {
                    if (item.InnerText.Trim() != "")
                    {
                        chunks.Add(item.InnerText.Trim());
                    }
                }
            }
            return String.Join(" ", chunks);
        }
        private async void chat_Click(object sender, RoutedEventArgs e)
        {
            //var html = await WebBrowser.CoreWebView2.ExecuteScriptAsync("document.body.textContent");
            //var htmldecoded = JsonConvert.DeserializeObject(html).ToString();
            //HtmlToText htmlToText = new HtmlToText();
            //var plain = Uglify.HtmlToText(htmldecoded);
            if (ChatGrid.Visibility == Visibility.Collapsed)
            {
                ChatGrid.Visibility = Visibility.Visible;
                Tabs.SetValue(Grid.ColumnSpanProperty, 3);
            }
            else
            {
                ChatGrid.Visibility = Visibility.Collapsed;
                Tabs.SetValue(Grid.ColumnSpanProperty, 4);
            }
        }

        private async void CaptureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WebBrowser.Focus(FocusState.Programmatic);
            await Task.Delay(500);
            InputInjector inputInjector2 = InputInjector.TryCreate();
            var shift = new InjectedInputKeyboardInfo();
            shift.VirtualKey = (ushort)(VirtualKey.Control);
            shift.KeyOptions = InjectedInputKeyOptions.None;


            var tab = new InjectedInputKeyboardInfo();
            tab.VirtualKey = (ushort)(VirtualKey.Shift);
            tab.KeyOptions = InjectedInputKeyOptions.None;

            var s1 = new InjectedInputKeyboardInfo();
            s1.VirtualKey = (ushort)(VirtualKey.S);
            s1.KeyOptions = InjectedInputKeyOptions.None;

            inputInjector2.InjectKeyboardInput(new[] { shift, tab,s1 });
            await Task.Delay(500);
            InputInjector inputInjector = InputInjector.TryCreate();
            var ctrl = new InjectedInputKeyboardInfo();
            ctrl.VirtualKey = (ushort)(VirtualKey.Control);
            ctrl.KeyOptions = InjectedInputKeyOptions.KeyUp;

            var a = new InjectedInputKeyboardInfo();
            a.VirtualKey = (ushort)(VirtualKey.Shift);
            a.KeyOptions = InjectedInputKeyOptions.KeyUp;

            var s2 = new InjectedInputKeyboardInfo();
            s2.VirtualKey = (ushort)(VirtualKey.S);
            s2.KeyOptions = InjectedInputKeyOptions.KeyUp;
            inputInjector.InjectKeyboardInput(new[] { ctrl, a,s2 });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Common.conversation.AppendUserInput(userinput.Text);
            SimpleMessage user = new SimpleMessage();
            user.content = userinput.Text;
            user.role = "User".GetLocalized();
            user.iconsource = "https://upload.wikimedia.org/wikipedia/commons/9/99/Sample_User_Icon.png";
            userinput.Text = "";
            ChatList.Items.Add(user);
            var aisay = await Common.conversation.GetResponseFromChatbot();
            var aisaid = new SimpleMessage();
            aisaid.content = aisay;
            aisaid.role = "AI";
            aisaid.iconsource = "https://cdn-icons-png.flaticon.com/512/4711/4711987.png";
            ChatList.Items.Add(aisaid);
        }

        private void userinput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Button_Click(null, null);
            }
            
        }

        private void TranslateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string tg = (string)localSettings.Values["translator"];
            if(tg is null || tg=="google")
                WebBrowser.Source = new Uri("https://translate.google.com/translate?u=" + WebBrowser.Source + "& client=webapp");
            else if(tg=="baidu")
                WebBrowser.Source = new Uri("http://fanyi.baidu.com/transpage?query=" + WebBrowser.Source + " &source=url&ie=utf8");
        }

        private async void PrivacyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await WebBrowser.EnsureCoreWebView2Async();
            
        }

        private void MuteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MuteMenuItem.IsChecked)
                {
                    WebBrowser.CoreWebView2.IsMuted = true;
                }
                else
                {
                    WebBrowser.CoreWebView2.IsMuted = false;
                }
            }
            catch (Exception ex) { }
        }
    }
    public class SimpleMessage
    {
        public string content;
        public string role;
        public string iconsource;
    }
}

