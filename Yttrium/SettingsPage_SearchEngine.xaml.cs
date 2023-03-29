using AloeWeb.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace AloeWeb_browser
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage_SearchEngine : Page
    {
        ApplicationDataContainer localSettings;
        public SettingsPage_SearchEngine()
        {
            this.InitializeComponent();
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["newtab"];
                if (nt.Contains("edge"))
                {
                    BingNew.IsChecked = true;
                }else if (nt.Contains("google"))
                {
                    GoogleNew.IsChecked = true;
                }
                else if (nt.Contains("baidu"))
                {
                    BaiduNew.IsChecked = true;
                }
                else if (nt.Contains("yandex"))
                {
                    YandexNew.IsChecked = true;
                }
            }catch (Exception ex)
            {

            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["search"];
                if (nt.Contains("bing"))
                {
                    Bing.IsChecked = true;
                }
                else if (nt.Contains("google"))
                {
                    Google.IsChecked = true;
                }
                else if (nt.Contains("baidu"))
                {
                    Baidu.IsChecked = true;
                }
                else if (nt.Contains("yandex"))
                {
                    Yandex.IsChecked = true;
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["homepage"];
                if (nt is null || nt == "") 
                    HomeasTab.IsChecked = true;
                else
                {
                    CustomHome.IsChecked = true;
                    CustomHomePage.IsEnabled = true;
                    CustomHomePage.Text = nt;
                }
            }
            catch (Exception ex)
            {
                localSettings.Values["homepage"] = "";
            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["favi"];
                if (nt is null || nt == "google")
                    GoogleFav.IsChecked = true;
                else
                {
                    FaviconKit.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
               
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            
            localSettings.Values["newtab"] = "https://ntp.msn.com/edge/ntp?&dsp=0&prerender=1&title="+"New Tab".GetLocalized();
        }

        private void GoogleNew_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["newtab"] = "https://www.google.com";
        }

        private void BaiduNew_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["newtab"] = "https://www.baidu.com";
        }

        private void YandexNew_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["newtab"] = "https://www.yandex.com";
        }

        private void HomeasTab_Click(object sender, RoutedEventArgs e)
        {
            CustomHomePage.IsEnabled = false;
            localSettings.Values["homepage"] = "";
        }

        private void CustomHome_Click(object sender, RoutedEventArgs e)
        {
            CustomHomePage.IsEnabled = true;
            localSettings.Values["homepage"] = CustomHomePage.Text;
        }

        private void CustomHomePage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(CustomHome.IsChecked??false)
                localSettings.Values["homepage"] = CustomHomePage.Text;
        }

        private void Bing_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["search"] = "https://www.bing.com/search?q=";
        }

        private void Google_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["search"] = "https://www.google.com/search?q=";
        }

        private void Baidu_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["search"] = "https://www.baidu.com/s?&wd=";
        }

        private void Yandex_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["search"] = "https://yandex.com/search/?text=";
        }

        private void FavGoogle_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["favi"] = "google";
        }

        private void FaviconKit_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["favi"] = "favk";
        }
    }
}
