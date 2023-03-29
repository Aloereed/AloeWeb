using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AloeWeb_browser
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public static string GetCurLanguage()
        {
            var languages = GlobalizationPreferences.Languages;
            if (languages.Count > 0)
            {
                List<string> lLang = new List<string>();
                lLang.Add("zh-cn、zh、zh-Hans、zh-hans-cn、zh-sg、zh-hans-sg");
                lLang.Add("zh-tw、zh-Hant、zh-mo、zh-hk、zh-hant-hk、zh-hant-mo、zh-hant-tw");
                lLang.Add("de-de、de-at、de-ch、de-lu、de-li");
                lLang.Add("en-us、en、en-au、en-ca、en-gb、en-ie、en-in、en-nz、en-sg、en-za、en-bz、en-hk、en-id、en-jm、en-kz、en-mt、en-my、en-ph、en-pk、en-tt、en-vn、en-zw、en-053、en-021、en-029、en-011、en-018、en-014");
                lLang.Add("fr-fr、fr-be、fr-ca、fr-ch、fr、fr-lu、fr-015、fr-cd、fr-ci、fr-cm、fr-ht、fr-ma、fr-mc、fr-ml、fr-re、frc-latn、frp-latn、fr-155、fr-029、fr-021、fr-011");
                lLang.Add("ja-jp、ja");
                lLang.Add("ko-kr、ko");
                lLang.Add("ru-ru、ru");
                for (int i = 0; i < lLang.Count; i++)
                {
                    if (lLang[i].ToLower().Contains(languages[0].ToLower()))
                    {
                        string temp = lLang[i].ToLower();
                        string[] tempArr = temp.Split('、');

                        return tempArr[0];
                    }
                    else
                        return "en-us";
                }
            }
            return "en-us";
        }
        public App()
        {
            string strCurrentLanguage;
            if (ApplicationData.Current.LocalSettings.Values["CurrentLanguage"] != null)
            {
                strCurrentLanguage = ApplicationData.Current.LocalSettings.Values["CurrentLanguage"].ToString();
                if (strCurrentLanguage == "Auto" || strCurrentLanguage == "Choose")
                {
                    ApplicationLanguages.PrimaryLanguageOverride = GetCurLanguage();
                }
                else
                    ApplicationLanguages.PrimaryLanguageOverride = strCurrentLanguage;
            }
            else
            {
                ApplicationLanguages.PrimaryLanguageOverride = strCurrentLanguage = GetCurLanguage();
                //ApplicationLanguages.PrimaryLanguageOverride = strCurrentLanguage = "en-us";
            }
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            UnhandledException += App_UnhandledException;
        }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            //TODO: 保存用户数据
            await new Windows.UI.Xaml.Controls.ContentDialog
            {
                Title = "Oops, the following accident seems to have happened to your program, please contact the developer.",
                Content = " If the problem is caused by WebView2, you may need to fix the WebView2 Runtime on your computer first. If your computer is configured with a proxy, then you need to set up the UWP proxy loopback or turn off the proxy first. \n"+ e.Exception+":"+e.Message,
                CloseButtonText = "Close",
                DefaultButton = Windows.UI.Xaml.Controls.ContentDialogButton.Close
            }.ShowAsync();
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures"));
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();

                //titlebar code
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                titleBar.ButtonBackgroundColor = Colors.Transparent;
            }

        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                Common.openurl = eventArgs.Uri.AbsoluteUri;
                // TODO: Handle URI activation
                // The received URI is eventArgs.Uri.AbsoluteUri
                Frame rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;


                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }

                if (true)
                {
                    if (rootFrame.Content == null)
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        rootFrame.Navigate(typeof(MainPage), args);
                    }
                    // Ensure the current window is active
                    Window.Current.Activate();

                    //titlebar code
                    var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                    coreTitleBar.ExtendViewIntoTitleBar = true;

                    var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                }
                
            }
        }
    }
}
