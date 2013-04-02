// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------


using ContosoCookbook.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ContosoCookbook.Data;
using Windows.ApplicationModel.Search;
using Windows.UI.ApplicationSettings;
using Callisto.Controls;
using Windows.UI;
using Windows.Storage;
using Windows.Networking.PushNotifications;
using Windows.Security.Cryptography;
using System.Net.Http;
using Windows.UI.Notifications;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;

// The Grid App template is documented at http://go.microsoft.com/fwlink/?LinkId=234226

namespace ContosoCookbook
{
  
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        Windows.UI.Core.CoreDispatcher _dispatcher = null;

 
        public Windows.UI.Core.CoreDispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
        }

        private Color _background = Color.FromArgb(255, 0, 77, 96);
         
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
 
        }

#if XAMLSPY 
         private FirstFloor.XamlSpy.XamlSpyService service;      
#endif

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Do not repeat app initialization when already running, just ensure that
            // the window is active

#if XAMLSPY 
            
            this.service = new FirstFloor.XamlSpy.XamlSpyService(this);
            this.service.StartService();
#endif 
            if (args.PreviousExecutionState == ApplicationExecutionState.Running)
            {
                if (!String.IsNullOrEmpty(args.Arguments))
                    ((Frame)Window.Current.Content).Navigate(typeof(ItemDetailPage), args.Arguments);
                Window.Current.Activate();
                return;
            }

            // Load recipe data
            await RecipeDataSource.LoadLocalDataAsync();

            // Register handler for SuggestionsRequested events from the search pane
            SearchPane.GetForCurrentView().SuggestionsRequested += OnSuggestionsRequested;

            // Register handler for CommandsRequested events from the settings pane
            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;

            // Clear tiles and badges
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();

            // Register for push notifications
            var profile = NetworkInformation.GetInternetConnectionProfile();

            if (profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                try
                { 
                    var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                    var buffer = CryptographicBuffer.ConvertStringToBinary(channel.Uri, BinaryStringEncoding.Utf8);
                    var uri = CryptographicBuffer.EncodeToBase64String(buffer);
                    var client = new HttpClient();
                    var response = await client.GetAsync(new Uri("http://ContosoRecipes8.cloudapp.net?uri=" + uri + "&type=tile"));

                    if (!response.IsSuccessStatusCode)
                    {
                        var dialog = new MessageDialog("Unable to open push notification channel");
                        dialog.ShowAsync();
                    }
                }
                catch (Exception )
                {
                    var dialog = new MessageDialog("Unable to open push notification channel");
                //     dialog.ShowAsync();
                }
            }

            // Initialize CurrentAppSimulator
            var file = await Package.Current.InstalledLocation.GetFileAsync("Data\\license.xml");
            await Windows.ApplicationModel.Store.CurrentAppSimulator.ReloadSimulatorAsync(file);
     
            // Create a Frame to act as the navigation context and associate it with
            // a SuspensionManager key
            var rootFrame = new Frame();
            SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

            // If the app was activated from a secondary tile, show the recipe
            if (!String.IsNullOrEmpty(args.Arguments))
            {
                rootFrame.Navigate(typeof(ItemDetailPage), args.Arguments);
                Window.Current.Content = rootFrame;
                Window.Current.Activate();
                return;
            }

            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Restore the saved session state only when appropriate
                await SuspensionManager.RestoreAsync();
            }

            // If the app was closed by the user the last time it ran, and if "Remember
            // "where I was" is enabled, restore the navigation state
            if (args.PreviousExecutionState == ApplicationExecutionState.ClosedByUser)
            {
                if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("Remember"))
                {
                    bool remember = (bool)ApplicationData.Current.RoamingSettings.Values["Remember"];
                    if (remember)
                        await SuspensionManager.RestoreAsync();
                }
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(GroupedItemsPage), "AllGroups"))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Place the frame in the current Window and ensure that it is active
            Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        /// Invoked when the application is activated to display search results.
        /// </summary>
        /// <param name="args">Details about the activation request.</param>
        protected async override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
        {
            // Reinitialize the app if a new instance was launched for search
            if (args.PreviousExecutionState == ApplicationExecutionState.NotRunning ||
                args.PreviousExecutionState == ApplicationExecutionState.ClosedByUser ||
                args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Load recipe data
                await RecipeDataSource.LoadLocalDataAsync();

                // Register handler for SuggestionsRequested events from the search pane
                SearchPane.GetForCurrentView().SuggestionsRequested += OnSuggestionsRequested;

                // Register handler for CommandsRequested events from the settings pane
                SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;

                // Add a Frame control to the window
                var rootFrame = new Frame();
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                Window.Current.Content = rootFrame;
            }

            ContosoCookbook.SearchResultsPage.Activate(args.QueryText, args.PreviousExecutionState);
        }

        void OnSuggestionsRequested(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs args)
        {
            string query = args.QueryText.ToLower();
            string[] terms = { "salt", "pepper", "water", "egg", "vinegar", "flour", "rice", "sugar", "oil" };

            foreach (var term in terms)
            {
                if (term.StartsWith(query))
                    args.Request.SearchSuggestionCollection.AppendQuerySuggestion(term);
            }
        }

        void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // Add an About command
            var about = new SettingsCommand("about", "About", (handler) =>
            {
                var settings = new SettingsFlyout();
                settings.Content = new AboutUserControl();
                settings.HeaderBrush = new SolidColorBrush(_background);
                settings.Background = new SolidColorBrush(_background);
                settings.HeaderText = "About";
                settings.IsOpen = true;
            });

            args.Request.ApplicationCommands.Add(about);

            // Add a Preferences command
            var preferences = new SettingsCommand("preferences", "Preferences", (handler) =>
            {
                var settings = new SettingsFlyout();
                settings.Content = new PreferencesUserControl();
                settings.HeaderBrush = new SolidColorBrush(_background);
                settings.Background = new SolidColorBrush(_background);
                settings.HeaderText = "Preferences";
                settings.IsOpen = true;
            });

            args.Request.ApplicationCommands.Add(preferences);
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            _dispatcher = args.Window.Dispatcher;

            base.OnWindowCreated(args);
        }
    }
}
