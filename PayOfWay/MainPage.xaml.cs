using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PayOfWay.Resources;

namespace PayOfWay
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		//TaxProfile _currentTaxProfile;
		AddTaxiService _service;
		public MainPage()
		{
			InitializeComponent();
			//_currentTaxProfile = Settings.LastSelectedProfile;
			this.Loaded += OnMainPageLoaded;
			PhoneApplicationService.Current.Closing += OnApplicationClosing;
			// Sample code to localize the ApplicationBar
			BuildLocalizedApplicationBar();
		}

		private void OnApplicationClosing(object sender, ClosingEventArgs e)
		{
			Settings.Save();
		}

		private void DisplayAddNewTaxiServiceDialog()
		{
			_service = new AddTaxiService();
			NavigationService.Navigate(new Uri("/CustomDialogPage.xaml", UriKind.Relative));
		}

		private void OnMainPageLoaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= OnMainPageLoaded;

			var profiles = Settings.AvailableProfiles;
			if (profiles.Count == 0)
				DisplayAddNewTaxiServiceDialog();
			
			profilesList.ItemsSource = Settings.AvailableProfiles;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			if (e.Content is CustomDialogPage)
			{
				CustomDialogPage msgMox = e.Content as CustomDialogPage;
				msgMox.CustomView = _service;
				msgMox.dismissedWithOk += OnConfigNewTaxiServiceCompleted;
			}
		}

		private void OnConfigNewTaxiServiceCompleted(UIElement obj)
		{
			var control = obj as AddTaxiService;
			TaxProfile profile = control.Profile;
			Settings.AvailableProfiles.Add(profile);
		}
		
		// Sample code for building a localized ApplicationBar
		private void BuildLocalizedApplicationBar()
		{
			// Set the page's ApplicationBar to a new instance of ApplicationBar.
			ApplicationBar = new ApplicationBar();

			// Create a new button and set the text value to the localized string from AppResources.
			ApplicationBarIconButton startButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
			startButton.Text = "Старт";
			ApplicationBar.Buttons.Add(startButton);
			ApplicationBarIconButton stopButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
			stopButton.Text = "Стоп";
			ApplicationBar.Buttons.Add(stopButton);

			// Create a new menu item with the localized string from AppResources.
			ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem("Добавить службу");
			appBarMenuItem.Click += OnAddTaxiServiceClick;
			ApplicationBar.MenuItems.Add(appBarMenuItem);
		}

		private void OnAddTaxiServiceClick(object sender, EventArgs e)
		{
			DisplayAddNewTaxiServiceDialog();
		}
	}
}