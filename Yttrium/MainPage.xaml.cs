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
        public MainPage()
        {
            this.InitializeComponent();
            localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //creates settings file on app first launch
            SettingsData settings = new SettingsData();
            settings.CreateSettingsFile();
            WebBrowser = FirstWebBrowser;
            webView2s.Add(WebBrowser);
            TabContent = FirstTabContent;
            CurrTab = FirstTab;
            //google login fix
            WebBrowser.CoreWebView2Initialized += delegate
            {
                OriginalUserAgent = WebBrowser.CoreWebView2.Settings.UserAgent+" AloeWeb/1.0";
                GoogleSignInUserAgent = OriginalUserAgent.Substring(0, OriginalUserAgent.IndexOf("Edg/"))
                .Replace("Mozilla/5.0", "Mozilla/4.0");
            };
            
            initFav();

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
                }catch(Exception ex)
                {
                    WebBrowser.Source = new Uri("https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title=" + "New Tab");
                }
                Common.openurl = null;
            }


        }

        private void CoreWebView2_DownloadStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs args)
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
            var firstuse = (string)localSettings.Values["firstuse"];

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
                StorageFile favFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("fav.list", CreationCollisionOption.ReplaceExisting);
                var writter = new NetscapeBookmarksWriter(AloeWeb_browser.Common.bookmarks);
                using (var file = await favFile.OpenStreamForWriteAsync())
                {
                    writter.Write(file);
                }
                Console.WriteLine(favFile.Path);
            }
            bk = AloeWeb_browser.Common.bookmarks;
            favurls = new HashSet<string>();
            await WebBrowser.EnsureCoreWebView2Async();
            WebBrowser.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            
            foreach (var b in bk.AllLinks)
            {
                favurls.Add(b.Url);
            }
            Common.bookmarkEdit = new ObservableCollection<BookmarkLink>();
            //localSettings.Values["search"] = "https://www.google.com/search?q=";
            if (firstuse is null)
            {
                var tmp = new BookmarkLink("https://www.aloereed.com", "Aloereed");
                tmp.IconUrl = "https://www.google.com/s2/favicons?sz=64&domain_url=https://www.aloereed.com";
                bk.Add(tmp);
                localSettings.Values["search"] = "https://www.google.com/search?q=";
                ContentDialog aboutdialog = new AboutDialog();
                var result = await aboutdialog.ShowAsync();
                localSettings.Values["firstuse"] = "notfirst";
            }
            bkE = Common.bookmarkEdit;
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

            initHis();
            await WebBrowser.EnsureCoreWebView2Async();
            if(!((localSettings.Values["downloader"] is null) || ((string)localSettings.Values["downloader"]=="")))
                WebBrowser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
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
            var col = (ObservableCollection<BookmarkLink>)(sender);
            bk.Clear();
            favurls?.Clear();
            foreach(var b in col)
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
            catch(Exception ex) { 
            }
            this.Bindings.Update();


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
                Uri icoURI = new Uri("https://www.google.com/s2/favicons?sz=64&domain_url=" + sender.Source);
                if(!(sender.Parent is null))
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
                    Content = "This website has a SSL certificate"
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
                    Content = "This website is unsafe and doesn't have a SSL certificate"
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
                AddHisButton_Click();
                BookmarkEdit_CollectionChanged(bkE, null);
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
            ToolTipService.SetToolTip(SSLButton, "This website has a SSL certificate, however, some errors are detected.");
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
            webView2s.Add(webView);
            webView.NavigationStarting += WebBrowser_NavigationStarting;
            webView.NavigationCompleted += WebBrowser_NavigationCompleted;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
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
        private async Task<WebView2> ResumeWebpage(TabView sender, string uri, bool ifwait = true)
        {
            WebView2 webView = new WebView2();
            await webView.EnsureCoreWebView2Async();
            var downloader = Windows.Storage.ApplicationData.Current.LocalSettings.Values["downloader"];
            if (!((downloader is null) || ((string)downloader == "")))
                webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            webView.NavigationStarting += WebBrowser_NavigationStarting;
            webView.NavigationCompleted += WebBrowser_NavigationCompleted;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            Frame frame = new Frame();
            frame.Content = webView;
            webView.Source = new Uri(uri);
            //webView.CoreWebView2.Navigate("https://google.com");
            //newTab.Content = new HomePage();
            //sender.TabItems.Add(new TabViewItem() { Content = newTab });
            //sender.SelectedItem = newTab ;
            //SearchBar.Text = newTab.Header.ToString();
            Uri icoURI = new Uri("https://www.google.com/s2/favicons?sz=64&domain_url=" + webView.Source);
            if(ifwait)
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


        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                        if (curtext.Last() == '?' || curtext.Last() == '£¿')
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
                                    var b = new BookmarkLink(term, "[From AI]");
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

        private void chat_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

