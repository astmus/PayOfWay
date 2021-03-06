﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PayOfWay.Resources;
using Windows.Devices.Geolocation;
using System.Device.Location;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Phone.Maps.Controls;
using System.Threading.Tasks;

namespace PayOfWay
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		//TaxProfile _currentTaxProfile;
		AddTaxiService _service;
		Geolocator _locator;
		GeoCoordinate _lastPosition;
		List<GeoCoordinate> _routePoints = new List<GeoCoordinate>();
		MapPolyline _lineOfRoute;
		double _totlaDistance;
		public MainPage()
		{
			InitializeComponent();
			//_currentTaxProfile = Settings.LastSelectedProfile;
			this.Loaded += OnMainPageLoaded;
			PhoneApplicationService.Current.Closing += OnApplicationClosing;
			// Sample code to localize the ApplicationBar

			_lineOfRoute = new MapPolyline();
			_lineOfRoute.StrokeColor = (Color)Application.Current.Resources["PhoneAccentColor"];
			_lineOfRoute.StrokeThickness = 5;			
			map.MapElements.Add(_lineOfRoute);

			BuildLocalizedApplicationBar();
		}

		public String FormattedTotalDistance
		{
			get { return (String)GetValue(FormattedTotalDistanceProperty); }
			set { SetValue(FormattedTotalDistanceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FormattedTotalDistance.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FormattedTotalDistanceProperty =
			DependencyProperty.Register("FormattedTotalDistance", typeof(String), typeof(MainPage), new PropertyMetadata("0.00"));



		private async void InitGeolocator()
		{
			try
			{
				_locator = new Geolocator();
				_locator.DesiredAccuracy = PositionAccuracy.High;
				Geoposition position = await _locator.GetGeopositionAsync();
				Geocoordinate currentPosition = position.Coordinate;
				map.Center = currentPosition.ToGeoCoordinate();
				_locator.MovementThreshold = 50;
				_locator.DesiredAccuracy = PositionAccuracy.High;
				_locator.PositionChanged += _locator_PositionChanged;
				_lastPosition = map.Center;
				_routePoints.Add(map.Center);
				map.ZoomLevel = 15;
				
			}
			catch (UnauthorizedAccessException)
			{
				_locator = null;
				if (MessageBox.Show("Геолокации отключена. Перейти к настройкам?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
				{
					LaunchSettings();
				}

			}
			catch (Exception e)
			{
				MessageBox.Show("unknown error");
			}
		}

		private void _locator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			var position = args.Position.Coordinate.ToGeoCoordinate();
			double distanceToPreviousPoint = 0;
			if (_routePoints.Count > 0)
				distanceToPreviousPoint = _routePoints.Last().GetDistanceTo(position);
			_routePoints.Add(position);
			_totlaDistance += distanceToPreviousPoint;
			

			Action handlePositionChange = new Action(() => {
				FormattedTotalDistance = (_totlaDistance / 1000).ToString("0.##");
				_lineOfRoute.Path.Add(position);
				if (_routePoints.Count > 1)
				{
					var rect = LocationRectangle.CreateBoundingRectangle(_routePoints);
					map.SetView(rect);
				}
			});

			if (Dispatcher.CheckAccess())
				handlePositionChange();
			else
				Dispatcher.BeginInvoke(handlePositionChange);
		}

		private void DisplayPointAtMapPosition(GeoCoordinate coordinate)
		{
			Ellipse myCircle = new Ellipse();
			myCircle.Fill = new SolidColorBrush(Colors.Black);
			myCircle.Height = 20;
			myCircle.Width = 20;

			var innerCircle = new Ellipse();
			innerCircle.Fill = new SolidColorBrush(Colors.Blue);
			innerCircle.Height = 14;
			innerCircle.Width = 14;
			Grid canva = new Grid();

			canva.Children.Add(myCircle);
			canva.Children.Add(innerCircle);
			// Create a MapOverlay to contain the circle.
			MapOverlay myLocationOverlay = new MapOverlay();
			myLocationOverlay.Content = canva;
			myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
			myLocationOverlay.GeoCoordinate = coordinate;

			MapLayer myLocationLayer = new MapLayer();
			myLocationLayer.Add(myLocationOverlay);

			map.Layers.Add(myLocationLayer);
		}

		private async static void LaunchSettings()
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
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
			InitGeolocator();
			DisplayPointAtMapPosition(map.Center);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (_locator == null)
				InitGeolocator();
			DisplayPointAtMapPosition(map.Center);
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