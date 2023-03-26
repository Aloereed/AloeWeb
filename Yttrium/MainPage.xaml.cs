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
        public static Frame outFrame;
    };
    public sealed partial class MainPage : Page
    {
        BookmarkFolder bk;
        ObservableCollection<BookmarkLink> bkE;
        string OriginalUserAgent;
        string GoogleSignInUserAgent;
        WebView2 WebBrowser;
        Frame TabContent;
        TabViewItem CurrTab;
        IList<MenuFlyoutItemBase> baseFavList;
        HashSet<string> favurls;
        ApplicationDataContainer localSettings;
        public MainPage()
        {
            this.InitializeComponent();
            localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //creates settings file on app first launch
            SettingsData settings = new SettingsData();
            settings.CreateSettingsFile();
            WebBrowser = FirstWebBrowser;
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
            

        }
        private async void initFav()
        {
            try
            {
                // Read data from a simple setting.
                StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Console.WriteLine(folder.Path);
                StorageFile favfile = await folder.GetFileAsync("fav.list");
                using (var file = File.OpenRead(favfile.Path))
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
                using (var file = File.OpenWrite(favFile.Path))
                {
                    writter.Write(file);
                }
                Console.WriteLine(favFile.Path);
            }
            bk = AloeWeb_browser.Common.bookmarks;
            
            await WebBrowser.EnsureCoreWebView2Async();
            WebBrowser.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            favurls = new HashSet<string>();
            foreach (var b in bk.AllLinks)
            {
                favurls.Add(b.Url);
            }
            Common.bookmarkEdit = new ObservableCollection<BookmarkLink>();
            bkE = Common.bookmarkEdit;
            foreach (var b in bk.AllLinks)
            {
                Common.bookmarkEdit.Add(b);
            }
         
            Common.bookmarkEdit.CollectionChanged += BookmarkEdit_CollectionChanged;
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
            StorageFile favFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("fav.list", CreationCollisionOption.ReplaceExisting);
            var writter = new NetscapeBookmarksWriter(bk);
            using (var file = File.OpenWrite(favFile.Path))
            {
                writter.Write(file);
            }

            
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

            if (favurls.Contains(WebBrowser.Source.AbsoluteUri))
            {
                char c = (char)0xE735;
                AddFavIcon.Glyph= c.ToString();
            }
            else
            {
                char c = (char)0xE734;
                AddFavIcon.Glyph = c.ToString();
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
                if(!SearchBar.Text.Contains("http"))
                    WebBrowser.Source = new Uri("http://"+SearchBar.Text);
                else
                    WebBrowser.Source = new Uri(SearchBar.Text);
            }
            else
            {
                WebBrowser.Source = new Uri("https://www.google.com/search?q=" + SearchBar.Text);
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


        private void SearchBar_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;
                var suitableItems = new List<string>();
                suitableItems.Add("Test");
                sender.ItemsSource = suitableItems;
            }
        }

        private void SearchBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Search();
        }

        private void SearchBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {

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
    }
}

