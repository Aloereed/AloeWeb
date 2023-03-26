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
using BookmarksManager;
using Windows.UI.Xaml.Shapes;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace AloeWeb_browser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage_Favorites : Page
    {
        public SettingsPage_Favorites()
        {
            this.InitializeComponent();
        }

        private void SetFavList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(e.ClickedItem)).Url;
            Common.outFrame.Navigate(typeof(MainPage));
        }

        private void FontIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void SetFavList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {
                FrameworkElement b = (FrameworkElement)e.OriginalSource;
                MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(b.DataContext)).Url;
            }catch(Exception ex)
            {
                try
                {
                    var b = (TextBlock)e.OriginalSource;
                    MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(b.DataContext)).Url;
                }catch(Exception ex2)
                {
                    Ellipse b = (Ellipse)e.OriginalSource;
                    MainPage.SuspendInfo.waitToOpen = ((BookmarkLink)(b.DataContext)).Url;
                }
            }
            Common.outFrame.Navigate(typeof(MainPage));
        }

        private void RemoveFavs_Click(object sender, RoutedEventArgs e)
        {
            foreach(var bl in SetFavList.SelectedItems)
            {
                Common.bookmarkEdit.Remove((BookmarkLink)bl);
            }
        }
    }
}
