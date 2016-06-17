using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using System.Device.Location;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Phone.Maps.Controls;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Pay_Of_Way
{
	struct PointSize
	{
		public static PointSize Normal = new PointSize(){BigCircle = 20, InnerCircle = 14};
		public static PointSize Small = new PointSize(){BigCircle = 10, InnerCircle = 6};
		public uint BigCircle{get;set;}
		public uint InnerCircle{get;set;}
	}

	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		//TaxProfile _currentTaxProfile;
		AddTaxiService _service;
		Geolocator _locator;

		public Geolocator Locator
		{
			get { return _locator ?? (_locator = new Geolocator()); }
		}
		GeoCoordinate _lastPosition;
		//List<GeoCoordinate> _routePoints = new List<GeoCoordinate>();
		MapPolyline _routePoints;
		double _totlaDistance;
		MapLayer _pointsMapLayer;
		bool _autoAlignMapView;
		bool autoAlignMapView 
		{
			get { return _autoAlignMapView; }
			set 
			{ 
				_autoAlignMapView = value;
				if (_autoAlignMapView) autoCentrateMap = false;
				if (_autoAlignMapView) Locator.PositionChanged += SetMapBoundByRoutePoints;
				else Locator.PositionChanged -= SetMapBoundByRoutePoints;
				_routeFocus.IconUri = UriForAlignMapView;
				SetMapBoundByRoutePoints();
			}
		}

		bool _autoCentrateMap;
		bool autoCentrateMap
		{
			get { return _autoCentrateMap; }
			set
			{
				_autoCentrateMap = value;
				if (_autoCentrateMap) autoAlignMapView = false;
				if (_autoCentrateMap) Locator.PositionChanged += CentrateMapByLastPoint;
				else Locator.PositionChanged -= CentrateMapByLastPoint;
				_centarteMap.IconUri = UriForCentrateMapButton;
				CentrateMapByLastPoint(_lastPosition);
			}
		}

		public MainPage()
		{
			InitializeComponent();
			BuildLocalizedApplicationBar();
			//_currentTaxProfile = Settings.LastSelectedProfile;			
			this.Loaded += OnMainPageLoaded;
			// Sample code to localize the ApplicationBar
			
			_routePoints = new MapPolyline();
			_routePoints.StrokeColor = (Color)Application.Current.Resources["PhoneAccentColor"];
			_routePoints.StrokeThickness = 5;			
			map.MapElements.Add(_routePoints);
			PhoneApplicationService.Current.Deactivated += OnApplicationClosing;
			PhoneApplicationService.Current.Closing += OnApplicationClosing;

			profilesList.ItemsSource = Settings.Instance.AvailableProfiles;
			profilesList.SelectedItem = Settings.Instance.LastSelectedProfile;

			Binding bind = new Binding();
			bind.Path = new PropertyPath("LastSelectedProfile");
			bind.Source = Settings.Instance;
			bind.Mode = BindingMode.TwoWay;
			profilesList.SetBinding(ListPicker.SelectedItemProperty, bind);

			_pointsMapLayer = new MapLayer();			
			map.Layers.Add(_pointsMapLayer);						
			autoCentrateMap = true;
			Locator.StatusChanged += OnLocatorStatusChanged;
		}

		Uri UriForCentrateMapButton
		{
			get { return _autoCentrateMap ? new System.Uri("/Assets/location.png", UriKind.Relative) : new Uri("/Assets/locationTrans.png", UriKind.Relative); }
		}

		Uri UriForAlignMapView
		{
			get { return _autoAlignMapView ? new System.Uri("/Assets/rect.png", UriKind.Relative) : new Uri("/Assets/rectTrans.png", UriKind.Relative); }
		}

		private void OnApplicationClosing(object sender, EventArgs e)
		{
			Settings.Instance.Save();	
		}

		public String FormattedTotalDistance
		{
			get { return (String)GetValue(FormattedTotalDistanceProperty); }
			set { SetValue(FormattedTotalDistanceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FormattedTotalDistance.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FormattedTotalDistanceProperty =
			DependencyProperty.Register("FormattedTotalDistance", typeof(String), typeof(MainPage), new PropertyMetadata("0.00"));

		private async Task InitGeolocator()
		{
			try
			{				
				//_locator.StatusChanged += _locator_StatusChanged;
				Locator.MovementThreshold = 10;
				Locator.DesiredAccuracyInMeters = 30;
				/*Geoposition position = await _locator.GetGeopositionAsync();								
				_locator.DesiredAccuracyInMeters = 30;
				_lastPosition = map.Center;
				_routePoints.Path.Add(_lastPosition);
				DisplayPointAtMap(_lastPosition);*/
				//_routePoints.Add(map.Center);
				
			}
			catch (UnauthorizedAccessException)
			{
				_locator = null;
				if (MessageBox.Show("Геолокация отключена. Перейти к настройкам?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
					LaunchSettings();
			}
			catch (Exception e)
			{
				MessageBox.Show("unknown error");
			}
		}

		void OnLocatorStatusChanged(Geolocator sender, StatusChangedEventArgs args)
		{
			if (args.Status == PositionStatus.Ready)
			{
				Dispatcher.BeginInvoke(async () =>
				{
					SystemTray.ProgressIndicator.Text = "Определение местоположения";
					SystemTray.ProgressIndicator.IsVisible = true;
					SystemTray.ProgressIndicator.IsIndeterminate = true;
					await InitGeolocator();
					map.ZoomLevel = 15;
					SystemTray.ProgressIndicator.IsIndeterminate = false;
					SystemTray.ProgressIndicator.IsVisible = false;
				});
				Locator.StatusChanged -= OnLocatorStatusChanged;
			}
		}

		private void onLoactorPositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			var position = args.Position.Coordinate.ToGeoCoordinate();
			double distanceToPreviousPoint = 0;

			if (_lastPosition != null)
				distanceToPreviousPoint = _lastPosition.GetDistanceTo(position);
			if (distanceToPreviousPoint < 10) return;
			_lastPosition = position;			
			_totlaDistance += distanceToPreviousPoint;
			
			Action handlePositionChange = () => {
				//FormattedTotalDistance = (_totlaDistance / 1000.0).ToString("0.##");
				FormattedTotalDistance = _totlaDistance.ToString();
				_routePoints.Path.Add(position);
				DisplayPointAtMap(position, Colors.Red, PointSize.Small);				
			};

			if (Dispatcher.CheckAccess())
				handlePositionChange();
			else
				Dispatcher.BeginInvoke(handlePositionChange);			
		}

		private void DisplayPointAtMap(GeoCoordinate coordinate)
		{
			DisplayPointAtMap(coordinate, Colors.Blue, PointSize.Normal);
		}

		private void DisplayPointAtMap(GeoCoordinate coordinate, Color insideColor, PointSize size)
		{
			Ellipse myCircle = new Ellipse();
			myCircle.Fill = new SolidColorBrush(Colors.Black);
			myCircle.Height = size.BigCircle;
			myCircle.Width = size.BigCircle;

			var innerCircle = new Ellipse();
			innerCircle.Fill = new SolidColorBrush(insideColor);
			innerCircle.Height = size.InnerCircle;
			innerCircle.Width = size.InnerCircle;
			Grid canva = new Grid();

			canva.Children.Add(myCircle);
			canva.Children.Add(innerCircle);
			// Create a MapOverlay to contain the circle.
			MapOverlay locationOverlay = new MapOverlay();
			locationOverlay.Content = canva;
			locationOverlay.PositionOrigin = new Point(0.5, 0.5);
			locationOverlay.GeoCoordinate = coordinate;
			_pointsMapLayer.Add(locationOverlay);
		}

		private async static void LaunchSettings()
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
		}

		private void DisplayAddNewTaxiServiceDialog()
		{
			_service = new AddTaxiService();
			NavigationService.Navigate(new Uri("/CustomDialogPage.xaml", UriKind.Relative));
		}

		private async void OnMainPageLoaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= OnMainPageLoaded;		
			
			
			//DisplayPointAtMap(map.Center);

			if (Settings.Instance.AvailableProfiles.Count == 0)
			{
				var box = new CustomMessageBox();
				box.Title = "Внимание";
				box.Message = "Нет ни одной службы такси. Добавить сейчас?";			
				box.LeftButtonContent = "Да";
				box.RightButtonContent = "Нет";
				box.Dismissed += box_Dismissed;
				box.Show();
			}
		}

		void box_Dismissed(object sender, DismissedEventArgs e)
		{
			if (e.Result == CustomMessageBoxResult.LeftButton)
				DisplayAddNewTaxiServiceDialog();
		}

		/*protected async override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
		}*/

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
			Settings.Instance.AvailableProfiles.Add(profile);
			profilesList.SelectedItem = profile;
		}

		ApplicationBarIconButton _routeFocus;
		ApplicationBarIconButton _centarteMap;
		// Sample code for building a localized ApplicationBar
		private void BuildLocalizedApplicationBar()
		{
			// Set the page's ApplicationBar to a new instance of ApplicationBar.
			ApplicationBar = new ApplicationBar();

			// Create a new button and set the text value to the localized string from AppResources.
			ApplicationBarIconButton startButton = new ApplicationBarIconButton(new Uri("/Assets/play.png", UriKind.Relative));
			startButton.Text = "Старт";
			startButton.Click += onStartButtonClick;
			ApplicationBar.Buttons.Add(startButton);
			/*ApplicationBarIconButton stopButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
			stopButton.Text = "Стоп";
			ApplicationBar.Buttons.Add(stopButton);*/
			_routeFocus = new ApplicationBarIconButton(UriForAlignMapView);
			_routeFocus.Text = "Фокус";
			_routeFocus.Click += onRouteFocusClick;
			ApplicationBar.Buttons.Add(_routeFocus);

			_centarteMap = new ApplicationBarIconButton(UriForCentrateMapButton);
			_centarteMap.Text = "Следить";
			_centarteMap.Click += onCentrateLocationClick;
			ApplicationBar.Buttons.Add(_centarteMap);

			// Create a new menu item with the localized string from AppResources.
			ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem("Добавить службу");
			appBarMenuItem.Click += OnAddTaxiServiceClick;
			ApplicationBar.MenuItems.Add(appBarMenuItem);
		}

		void onCentrateLocationClick(object sender, EventArgs e)
		{
			autoCentrateMap = !autoCentrateMap;
		}

		void onRouteFocusClick(object sender, EventArgs e)
		{
			autoAlignMapView = !autoAlignMapView;
		}

		void onStartButtonClick(object sender, EventArgs e)
		{
			ApplicationBarIconButton button = sender as ApplicationBarIconButton;
			switch(button.Text)
			{
				case "Старт":
					button.Text = "Стоп";
					button.IconUri = new Uri("/Assets/stop.png", UriKind.Relative);
					_locator.PositionChanged += onLoactorPositionChanged;
					PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
					break;
				case "Стоп":
					button.Text = "Старт";
					button.IconUri = new Uri("/Assets/play.png", UriKind.Relative);
					_locator.PositionChanged -= onLoactorPositionChanged;
					PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
					break;
			}
		}		

		private void OnAddTaxiServiceClick(object sender, EventArgs e)
		{
			DisplayAddNewTaxiServiceDialog();
		}
		void SetMapBoundByRoutePoints(Geolocator sender, PositionChangedEventArgs args)
		{
			SetMapBoundByRoutePoints();
		}
		void CentrateMapByLastPoint(Geolocator sender, PositionChangedEventArgs args)
		{
			CentrateMapByLastPoint(args.Position.Coordinate.ToGeoCoordinate());
		}

		private void CentrateMapByLastPoint(GeoCoordinate position = null)
		{
			if (!autoCentrateMap) return;
			Action action = () =>
			{
				if (_routePoints.Path.Count > 0 || position != null)
				{
					var point = position ?? _routePoints.Path.Last();
					map.Center = point;
					if (_pointsMapLayer.Count == 0)
						DisplayPointAtMap(point);
					else
						_pointsMapLayer.First().GeoCoordinate = point;
					_lastPosition = point;
				}
			};
			
			if (Dispatcher.CheckAccess())
				action();
			else
				Dispatcher.BeginInvoke(action);
		}

		private void SetMapBoundByRoutePoints()
		{
			if (autoAlignMapView && _routePoints.Path.Count > 1)
			{
				var rect = LocationRectangle.CreateBoundingRectangle(_routePoints.Path);
				map.SetView(rect,new Thickness(5,80,5,5));
			}
		}
	}
}