using AloeWeb;
using BookmarksManager;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace AloeWeb_browser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage_History : Page
    {
        public SettingsPage_History()
        {
            this.InitializeComponent();
        }

        private void SetHisList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(e.ClickedItem)).Url;
            Common.outFrame.Navigate(typeof(MainPage));
        }

        private void FontIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void SetHisList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {
                FrameworkElement b = (FrameworkElement)e.OriginalSource;
                MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(b.DataContext)).Url;
            }
            catch (Exception ex)
            {
                try
                {
                    var b = (TextBlock)e.OriginalSource;
                    MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(b.DataContext)).Url;
                }
                catch (Exception ex2)
                {
                    Ellipse b = (Ellipse)e.OriginalSource;
                    MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(b.DataContext)).Url;
                }
            }
            Common.outFrame.Navigate(typeof(MainPage));
        }

        private void RemoveHiss_Click(object sender, RoutedEventArgs e)
        {
            foreach (var bl in SetHisList.SelectedItems)
            {
                Common.historyEdit.Remove((BookmarkLink)bl);
            }
        }

        private void RemoveAllHis_Click(object sender, RoutedEventArgs e)
        {
            Common.historyEdit.Clear();
        }

        private async void RemoveAllCookies_Click(object sender, RoutedEventArgs e)
        {
            WebView2 tmp=new WebView2();
            await tmp.EnsureCoreWebView2Async();
            tmp.CoreWebView2.CookieManager.DeleteAllCookies();
            ContentDialog aboutdialog = new ContentDialog();
            aboutdialog.Title = "cookman".GetLocalized();
            aboutdialog.Content = "cookalld".GetLocalized();
            aboutdialog.CloseButtonText = "OK";
            var result = await aboutdialog.ShowAsync();
            tmp.Close();
        }

        private async void RemoveAllOther_Click(object sender, RoutedEventArgs e)
        {
            WebView2 tmp = new WebView2();
            await tmp.EnsureCoreWebView2Async();
            await tmp.CoreWebView2.Profile.ClearBrowsingDataAsync();
            ContentDialog aboutdialog = new ContentDialog();
            aboutdialog.Title = "dataman".GetLocalized();
            aboutdialog.Content = "datalld".GetLocalized();
            aboutdialog.CloseButtonText = "OK";
            var result = await aboutdialog.ShowAsync();
            tmp.Close();
        }
    }
}
