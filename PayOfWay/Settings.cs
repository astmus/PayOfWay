using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace PayOfWay
{
	
	class Settings
	{
		static IsolatedStorageSettings storage = IsolatedStorageSettings.ApplicationSettings;

		private static ObservableCollection<TaxProfile> _availableProfiles;
		internal static ObservableCollection<TaxProfile> AvailableProfiles
		{
			get { return _availableProfiles ?? (_availableProfiles = LoadAvailableProfiles()); }
			set { _availableProfiles = value; }
		}

		private static ObservableCollection<TaxProfile> LoadAvailableProfiles()
		{
			if (storage.Contains("available_profile"))
				return JsonConvert.DeserializeObject<ObservableCollection<TaxProfile>>(storage["available_profile"] as String);
			else
				return new ObservableCollection<TaxProfile>();
		}

		private static TaxProfile _lastSelectedProfile = null;
		internal static TaxProfile LastSelectedProfile
		{
			get { return _lastSelectedProfile ?? (_lastSelectedProfile = LoadLastSelectedProfile()); }
			set { _lastSelectedProfile = value; }
		}

		private static TaxProfile LoadLastSelectedProfile()
		{
			if (storage.Contains("last_selected_profile"))
				return JsonConvert.DeserializeObject<TaxProfile>(storage["last_selected_profile"] as String);
			else
				return null;
		}

		public static void Save()
		{
			storage["available_profile"] = JsonConvert.SerializeObject(_availableProfiles);
			storage["last_selected_profile"] = JsonConvert.SerializeObject(_lastSelectedProfile);
			storage.Save();
		}
	}
}
