using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Windows;

namespace Pay_Of_Way
{
	
	class Settings
	{
		private static Settings _instance;
		public static Settings Instance 
		{
			get { return _instance ?? (_instance = new Settings()); }
		}
		private Settings()
		{

		}

		static IsolatedStorageSettings storage = IsolatedStorageSettings.ApplicationSettings;

		private static ObservableCollection<TaxProfile> _availableProfiles;
		internal ObservableCollection<TaxProfile> AvailableProfiles
		{
			get { return _availableProfiles ?? (_availableProfiles = LoadAvailableProfiles()); }
			set { _availableProfiles = value; }
		}

		private ObservableCollection<TaxProfile> LoadAvailableProfiles()
		{
			if (storage.Contains("available_profile"))
				return JsonConvert.DeserializeObject<ObservableCollection<TaxProfile>>(storage["available_profile"] as String);
			else
				return new ObservableCollection<TaxProfile>();
		}

		private TaxProfile _lastSelectedProfile = null;
		public TaxProfile LastSelectedProfile
		{
			get { return _lastSelectedProfile ?? (_lastSelectedProfile = LoadLastSelectedProfile()); }
			set { _lastSelectedProfile = value; }
		}

		private TaxProfile LoadLastSelectedProfile()
		{
			if (storage.Contains("last_selected_profile"))
				return AvailableProfiles[(int)storage["last_selected_profile"]];
			else
				if (AvailableProfiles.Count > 0)
					return AvailableProfiles[0];
				else
					return null;
		}		

		public void Save()
		{
			if (_availableProfiles != null && _availableProfiles.Count != 0)
			{
				storage["available_profile"] = JsonConvert.SerializeObject(_availableProfiles);
				storage["last_selected_profile"] = _availableProfiles.IndexOf(LastSelectedProfile);
			}
			storage.Save();
		}
	}
}
