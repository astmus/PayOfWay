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
		GeoCoordinate _lastPosition;
		List<GeoCoordinate> _routePoints = new List<GeoCoordinate>();
		MapPolyline _lineOfRoute;
		double _totlaDistance;
		MapLayer _pointsMapLayer;
		bool autoAlignMapView = true;
		public MainPage()
		{
			InitializeComponent();
			//_currentTaxProfile = Settings.LastSelectedProfile;			
			this.Loaded += OnMainPageLoaded;
			// Sample code to localize the ApplicationBar
			
			_lineOfRoute = new MapPolyline();
			_lineOfRoute.StrokeColor = (Color)Application.Current.Resources["PhoneAccentColor"];
			_lineOfRoute.StrokeThickness = 5;			
			map.MapElements.Add(_lineOfRoute);
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

			BuildLocalizedApplicationBar();
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
				_locator = new Geolocator();
				_locator.MovementThreshold = 10;								
				Geoposition position = await _locator.GetGeopositionAsync();				
				Geocoordinate currentPosition = position.Coordinate;
				map.Center = currentPosition.ToGeoCoordinate();				
				_locator.PositionChanged += onLoactorPositionChanged;
				_locator.DesiredAccuracyInMeters = 30;
				//_locator.DesiredAccuracy = PositionAccuracy.High;				
				_lastPosition = map.Center;
				_routePoints.Add(map.Center);
				map.ZoomLevel = 14;
			}
			catch (UnauthorizedAccessException)
			{
				_locator = null;
				if (MessageBox.Show("Геолокации отключена. Перейти к настройкам?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
					LaunchSettings();
			}
			catch (Exception e)
			{
				MessageBox.Show("unknown error");
			}
		}

		private void onLoactorPositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			var position = args.Position.Coordinate.ToGeoCoordinate();
			double distanceToPreviousPoint = 0;

			if (_lastPosition != null)
				distanceToPreviousPoint = _lastPosition.GetDistanceTo(position);
			_lastPosition = position;
			_routePoints.Add(position);
			if (distanceToPreviousPoint < 10) return;
			_totlaDistance += distanceToPreviousPoint;
			
			Action handlePositionChange = () => {
				FormattedTotalDistance = (_totlaDistance / 1000.0).ToString("0.##");
				_lineOfRoute.Path.Add(position);
				DisplayPointAtMap(position, Colors.Red, PointSize.Small);
				SetMapBoundByRoutePoints();
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
			
			if (_locator == null)
			{
				SystemTray.ProgressIndicator.Text = "Определение местоположения";
				SystemTray.ProgressIndicator.IsVisible = true;
				SystemTray.ProgressIndicator.IsIndeterminate = true;
				await InitGeolocator();
				SystemTray.ProgressIndicator.IsIndeterminate = false;
				SystemTray.ProgressIndicator.IsVisible = false;
			}
			DisplayPointAtMap(map.Center);

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

		private void map_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			autoAlignMapView = false;
			SetMapBoundByRoutePoints();
		}

		private void SetMapBoundByRoutePoints()
		{
			if (_routePoints.Count > 1 && autoAlignMapView)
			{
				var rect = LocationRectangle.CreateBoundingRectangle(_routePoints);
				map.SetView(rect);
			}
		}

		private void map_ZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
		{
			autoAlignMapView = false;
		}
	}
}