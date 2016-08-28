using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Device.Location;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.Devices.Geolocation;

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
		private bool ShouldResetSystemTray = false;

		public Geolocator Locator
		{
			get { return _locator ?? (_locator = InitGeolocator()); }
		}
		GeoCoordinate _lastPosition;
		//List<GeoCoordinate> _routePoints = new List<GeoCoordinate>();
		MapPolyline _routePoints;
		double _totlaDistanceInMeters;
		MapLayer _pointsMapLayer;
		DispatcherTimer _timer = new DispatcherTimer();
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
			_timer.Tick += onTimerTick;
			_timer.Interval = TimeSpan.FromSeconds(1);
			_routePoints = new MapPolyline();
			_routePoints.StrokeColor = (Color)Application.Current.Resources["PhoneAccentColor"];
			_routePoints.StrokeThickness = 5;			
			map.MapElements.Add(_routePoints);
			PhoneApplicationService.Current.Deactivated += OnApplicationClosing;
			PhoneApplicationService.Current.Closing += OnApplicationClosing;

			taxProfilesList.ItemsSource = Settings.Instance.AvailableProfiles;
			taxProfilesList.SelectedItem = Settings.Instance.LastSelectedProfile;

			Binding bind = new Binding();
			bind.Path = new PropertyPath("LastSelectedProfile");
			bind.Source = Settings.Instance;
			bind.Mode = BindingMode.TwoWay;
			taxProfilesList.SetBinding(ListPicker.SelectedItemProperty, bind);

			_pointsMapLayer = new MapLayer();			
			map.Layers.Add(_pointsMapLayer);						
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

		public string TotalPrice
		{
			get { return (string)GetValue(TotalPriceProperty); }
			set { SetValue(TotalPriceProperty,value); }
		}

		public static readonly DependencyProperty TotalPriceProperty =
			DependencyProperty.Register("TotalPrice", typeof(string), typeof(MainPage), new PropertyMetadata("0.00"));

		public string TotalTime
		{
			get { return (string)GetValue(TotalTimeProperty); }
			set { SetValue(TotalTimeProperty, value); }
		}
		public static readonly DependencyProperty TotalTimeProperty = DependencyProperty.Register("TotalTime", typeof(string), typeof(MainPage), new PropertyMetadata("00:00"));

		public String FormattedTotalDistance
		{
			get { return (String)GetValue(FormattedTotalDistanceProperty); }
			set { SetValue(FormattedTotalDistanceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FormattedTotalDistance.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FormattedTotalDistanceProperty =
			DependencyProperty.Register("FormattedTotalDistance", typeof(String), typeof(MainPage), new PropertyMetadata("0.00 км"));

		private Geolocator InitGeolocator()
		{
			try
			{
				_locator = new Geolocator();
				_locator.MovementThreshold = 10;
				_locator.DesiredAccuracyInMeters = 30;
				_locator.DesiredAccuracy = PositionAccuracy.High;
				return _locator;				
			}
			catch (UnauthorizedAccessException)
			{
				_locator = null;
				if (MessageBox.Show("Геолокация отключена. Перейти к настройкам?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
					LaunchSettings();
			}
			catch
			{
				MessageBox.Show("unknown error");
			}
			return null;
		}

		void OnLocatorStatusChanged(Geolocator sender, StatusChangedEventArgs args)
		{		
			System.Diagnostics.Debug.WriteLine(args.Status.ToString());
			switch (args.Status)
			{
				case PositionStatus.Initializing:
					Dispatcher.BeginInvoke(() =>
				{
					if (SystemTray.ProgressIndicator != null)
					{
						SystemTray.ProgressIndicator.Text = "Определение местоположения";
						SystemTray.ProgressIndicator.IsVisible = true;
						SystemTray.ProgressIndicator.IsIndeterminate = true;
					}
					else
					map.ZoomLevel = 15;
				});
				Locator.StatusChanged -= OnLocatorStatusChanged;
				break;

				case PositionStatus.Ready:
					Dispatcher.BeginInvoke(() =>
				{
					if (SystemTray.ProgressIndicator != null)
					{
						SystemTray.ProgressIndicator.IsIndeterminate = false;
						SystemTray.ProgressIndicator.IsVisible = false;
					}
					else
						ShouldResetSystemTray = true;
					autoCentrateMap = true;
					map.ZoomLevel = 15;
					(ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
				});			
				Locator.StatusChanged -= OnLocatorStatusChanged;
				break;
				
				case PositionStatus.NotAvailable:
				case PositionStatus.Disabled:
					Dispatcher.BeginInvoke(() =>
				{
					if (MessageBox.Show("Геолокация отключена. Перейти к настройкам?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
						LaunchSettings();
				});
					break;
			}
		}

		private void onLoactorPositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			var position = args.Position.Coordinate.ToGeoCoordinate();
			double distanceToPreviousPoint = 0;

			if (_lastPosition != null)
				distanceToPreviousPoint = _lastPosition.GetDistanceTo(position);
			//if (distanceToPreviousPoint < 10) return;
			_lastPosition = position;			
			_totlaDistanceInMeters += distanceToPreviousPoint;
			
			Action handlePositionChange = () => {
				FormattedTotalDistance = (_totlaDistanceInMeters / 1000.0).ToString("0.## км");				
				TaxProfile currentTax =  taxProfilesList.SelectedItem as TaxProfile;
				if (currentTax != null)
				{
					if (_totlaDistanceInMeters < (currentTax.AmountOfStartKilometers * 1000))
						TotalPrice = currentTax.StartCost.ToString("C2");
					else
						TotalPrice = (currentTax.StartCost + (((_totlaDistanceInMeters / 1000) - currentTax.AmountOfStartKilometers) * currentTax.AfterKilometerCost)).ToString("C2");
				}				
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

		private void OnMainPageLoaded(object sender, RoutedEventArgs e)
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

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (ShouldResetSystemTray)
			{
				SystemTray.ProgressIndicator.IsIndeterminate = false;
				SystemTray.ProgressIndicator.IsVisible = false;
			}
		}

		private void OnConfigNewTaxiServiceCompleted(UIElement obj)
		{
			var control = obj as AddTaxiService;
			TaxProfile profile = control.Profile;
			Settings.Instance.AvailableProfiles.Add(profile);
			Settings.Instance.Save();
			taxProfilesList.SelectedItem = profile;
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
			startButton.IsEnabled = false;
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

			ApplicationBarMenuItem clearMap = new ApplicationBarMenuItem("Очистить карту");
			clearMap.Click += OnClearMapClick;
			ApplicationBar.MenuItems.Add(clearMap);
		}

		void OnClearMapClick(object sender, EventArgs e)
		{			
			var point = _pointsMapLayer.Last();
			_pointsMapLayer.Clear();
			_pointsMapLayer.Add(point);
			_routePoints.Path.Clear();
			_routePoints.Path.Add(point.GeoCoordinate);
		}

		void onCentrateLocationClick(object sender, EventArgs e)
		{
			autoCentrateMap = !autoCentrateMap;
		}

		void onRouteFocusClick(object sender, EventArgs e)
		{
			autoAlignMapView = !autoAlignMapView;
		}

		private int _totalRemainsTime;
		async void onStartButtonClick(object sender, EventArgs e)
		{
			ApplicationBarIconButton button = sender as ApplicationBarIconButton;
			switch(button.Text)
			{
				case "Старт":
					button.Text = "Стоп";
					TotalPrice = 0.ToString("C2");
					OnClearMapClick(null, null);
					onTimerTick(null, null); 
					button.IconUri = new Uri("/Assets/stop.png", UriKind.Relative);
					Locator.PositionChanged += onLoactorPositionChanged;
					PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
					_totalRemainsTime = 0;
					_timer.Start();
					infoExpander.IsExpanded = false;
					break;
				case "Стоп":
					button.Text = "Старт";
					button.IconUri = new Uri("/Assets/play.png", UriKind.Relative);
					Locator.PositionChanged -= onLoactorPositionChanged;
					_timer.Stop();					
					PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
					DisplayPointAtMap((await Locator.GetGeopositionAsync()).Coordinate.ToGeoCoordinate(), Colors.Blue, PointSize.Normal);
					break;
			}
		}

		void onTimerTick(object sender, EventArgs e)
		{			
			TotalTime = TimeSpan.FromSeconds(_totalRemainsTime).ToString(@"mm\:ss");
			_totalRemainsTime++;
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
					{
						DisplayPointAtMap(point);
						_routePoints.Path.Add(point);
					}
					if (_timer.IsEnabled == false)
					{
						_pointsMapLayer.Last().GeoCoordinate = point;
						if (_routePoints.Path.Count == 1)
							_routePoints.Path[0] = point;
					}
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
			if (autoAlignMapView == false) return;
				Dispatcher.BeginInvoke(() =>
				{
					if (_routePoints.Path.Count > 1) 
					{
						var rect = LocationRectangle.CreateBoundingRectangle(_routePoints.Path);
						map.SetView(rect, new Thickness(5, 5, 5, 40));
					}
				});	
		}

		private void map_Loaded(object sender, RoutedEventArgs e)
		{
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "a0122010-7fd3-4781-8d6a-e2cf4d62999f";
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "x2UWa9cH9hlH2kEQNXdu1w";
		}
	}
}