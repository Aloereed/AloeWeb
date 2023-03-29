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
    public sealed partial class SettingsPage_General : Page
    {
        ApplicationDataContainer localSettings;
        public SettingsPage_General()
        {
            this.InitializeComponent();
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["profile"];
                if (nt is null || nt == Windows.Storage.ApplicationData.Current.LocalFolder.Path)
                {
                    uselocal.IsChecked = true;
                }else 
                {
                    useother.IsChecked = true;
                    Customprofile.IsEnabled = true;
                    Customprofile.Text = nt;
                }
            }catch (Exception ex)
            {

            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["downdir"];
                if (nt is null || nt == "")
                {
                    
                }
                else
                {
                    Downdir.Text = nt;
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["downloader"];
                if (nt is null || nt == "") 
                    useEmbeddedDownloader.IsChecked = true;
                else
                {
                    useFDM.IsChecked = true;
                    CustomDownloader.IsEnabled = true;
                    CustomDownloader.Text = nt;
                }
            }
            catch (Exception ex)
            {
                localSettings.Values["downloader"] = "";
            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["openaikey"];
                if (nt is null || nt == "")
                    usenogpt.IsChecked = true;
                else
                {
                    usegpt.IsChecked = true;
                    CustomAPI.IsEnabled = true;
                    CustomAPI.Text = nt;
                }
            }
            catch (Exception ex)
            {
            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["translator"];
                if (nt is null || nt == "google")
                    usegoogle.IsChecked = true;
                else
                {
                    usebaidu.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool nt = (bool?)localSettings.Values["usepwd"]??false;
                if (nt)
                    usepwd.IsChecked = true;
            }
            catch (Exception ex)
            {
            }
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string nt = (string)localSettings.Values["blockads"];
                if (nt is null || nt == "")
                    useadblock.IsChecked = false;
                else
                {
                    useadblock.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
            }
        }



        private void uselocal_Click(object sender, RoutedEventArgs e)
        {
            Customprofile.IsEnabled = false;
            localSettings.Values["profile"] = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            Customprofile.Text = (string)localSettings.Values["profile"];
        }

        private void useother_Click(object sender, RoutedEventArgs e)
        {
            Customprofile.IsEnabled = true;
            try
            {
                if (Directory.Exists(Customprofile.Text))
                {
                    localSettings.Values["profile"] = Customprofile.Text;
                }
            }
            catch (Exception)
            { }
            
        }

        private void useEmbeddedDownloader_Click(object sender, RoutedEventArgs e)
        {
            CustomDownloader.IsEnabled = false;
            localSettings.Values["downloader"] = "";
            if (useEmbeddedDownloader.IsChecked ?? false)
            {
                try
                {
                    if (Directory.Exists(Downdir.Text))
                    {
                        localSettings.Values["downdir"] = Downdir.Text;
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void useFDM_Click(object sender, RoutedEventArgs e)
        {
            CustomDownloader.IsEnabled =  true;
            try
            {
                if (File.Exists(CustomDownloader.Text))
                {
                    localSettings.Values["downloader"] = CustomDownloader.Text;
                }
            }
            catch (Exception)
            { }
            
        }

        private void Customprofile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(useother.IsChecked??false)
            {
                try
                {
                    if (Directory.Exists(Customprofile.Text))
                    {
                        localSettings.Values["profile"] = Customprofile.Text;
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void CustomDownloader_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (useFDM.IsChecked ?? false)
            {
                try
                {
                    if (File.Exists(CustomDownloader.Text))
                    {
                        localSettings.Values["downloader"] = CustomDownloader.Text;
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void usenogpt_Click(object sender, RoutedEventArgs e)
        {
            CustomAPI.IsEnabled = false;
            localSettings.Values["openaikey"] = "";
        }

        private void usegpt_Click(object sender, RoutedEventArgs e)
        {
            CustomAPI.IsEnabled = true;
            localSettings.Values["openaikey"] = CustomAPI.Text;
        }

        private void CustomAPI_TextChanged(object sender, TextChangedEventArgs e)
        {
            localSettings.Values["openaikey"] = CustomAPI.Text;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string temp = ((ComboBoxItem)(LanguageSet.SelectedItem)).Content.ToString();
            if (temp != "Choose Language")
            {
                string[] tempArr = temp.Split(' ');
                ApplicationData.Current.LocalSettings.Values["CurrentLanguage"] = tempArr[0];
            }
        }

        private void usegoogle_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["translator"] = "google";
        }
        private void usebaidu_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["translator"] = "baidu";
        }

        private void usepwd_Click(object sender, RoutedEventArgs e)
        {
            if(usepwd.IsChecked == true)
            {
                localSettings.Values["usepwd"] = true;
            }
            else
            {
                localSettings.Values["usepwd"] = false;
            }
        }

        private void useadblock_Click(object sender, RoutedEventArgs e)
        {
            if (usepwd.IsChecked == true)
            {
                localSettings.Values["blockads"] = "yes";
            }
            else
            {
                localSettings.Values["blockads"] = "";
            }
        }

        private void Downdir_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (useEmbeddedDownloader.IsChecked ?? false)
            {
                try
                {
                    if (Directory.Exists(Downdir.Text))
                    {
                        localSettings.Values["downdir"] = Downdir.Text;
                    }
                }
                catch (Exception)
                { }
            }
        }
    }
}
