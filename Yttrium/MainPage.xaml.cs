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
using System.Text.RegularExpressions;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AloeWeb_browser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string OriginalUserAgent;
        string GoogleSignInUserAgent;
        WebView2 WebBrowser;
        Frame TabContent;
        TabViewItem CurrTab;
        public MainPage()
        {
            this.InitializeComponent();
            //creates settings file on app first launch
            SettingsData settings = new SettingsData();
            settings.CreateSettingsFile();
            WebBrowser = FirstWebBrowser;
            TabContent = FirstTabContent;
            CurrTab = FirstTab;
            //google login fix
            WebBrowser.CoreWebView2Initialized += delegate
            {
                OriginalUserAgent = WebBrowser.CoreWebView2.Settings.UserAgent;
                GoogleSignInUserAgent = OriginalUserAgent.Substring(0, OriginalUserAgent.IndexOf("Edg/"))
                .Replace("Mozilla/5.0", "Mozilla/4.0");
            };

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
                WebBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                Uri icoURI = new Uri("https://www.google.com/s2/favicons?sz=64&domain_url=" + WebBrowser.Source);
                CurrTab.IconSource = new Microsoft.UI.Xaml.Controls.BitmapIconSource() { UriSource = icoURI, ShowAsMonochrome = false };
                CurrTab.Header = WebBrowser.CoreWebView2.DocumentTitle.ToString();
                SearchBar.Text = WebBrowser.Source.AbsoluteUri;
                RefreshButton.Visibility = Visibility.Visible;
                StopRefreshButton.Visibility = Visibility.Collapsed;

                //history
                DataTransfer datatransfer = new DataTransfer();
                if (!string.IsNullOrEmpty(SearchBar.Text))
                {
                    datatransfer.SaveSearchTerm(SearchBar.Text, WebBrowser.CoreWebView2.DocumentTitle, WebBrowser.Source.AbsoluteUri);
                }
            }
            catch
            {

            }


            if (WebBrowser.Source.AbsoluteUri.Contains("https"))
            {
                //change icon to lock
                SSLIcon.FontFamily = new FontFamily("Segoe Fluent Icons");
                SSLIcon.Glyph = "\xE72E";

                ToolTip tooltip = new ToolTip
                {
                    Content = "This website has a SSL certificate"
                };
                WebBrowser.CoreWebView2.ServerCertificateErrorDetected += CoreWebView2_ServerCertificateErrorDetected;
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
        private void SearchBar_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchBar.SelectAll();
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
        private void Search()
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
        }

        //home button redirects to homepage
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            TabContent.Content = new HomePage();
            SearchBar.Text = "Home page";
        }

        //opens settings page
        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }


        //handles progressring and refresh behavior
        private void WebBrowser_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            RefreshButton.Visibility = Visibility.Collapsed;
            StopRefreshButton.Visibility = Visibility.Visible;

            var isGoogleLogin = new Uri(args.Uri).Host.Contains("accounts.google.com");
            WebBrowser.CoreWebView2.Settings.UserAgent = isGoogleLogin ? GoogleSignInUserAgent : OriginalUserAgent;
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
            webView.Source = new Uri("https://www.google.com");
            sender.SelectedItem = Tabs.TabItems.Last();
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
                CurrTab = (TabViewItem)(e.AddedItems[0]);
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
    }
}

