using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PayOfWay
{
	public class TaxProfile : INotifyDataErrorInfo
	{
		private Lazy<Dictionary<String, List<String>>> errors = new Lazy<Dictionary<string, List<string>>>(() =>
		{
			return new Dictionary<String, List<String>>();
		});

		private string _nameLengthError = null;
		private string _name = "Служба";
		public string Name
		{
			get { return _name; }
			set
			{
				if (value.Length > 0)
				{
					_name = value;
					RemoveError(_nameLengthError);
				}
				else
					AddError(_nameLengthError ?? (_nameLengthError = "Служба"));
			}
		}

		private string _lowStartCostError = null;
		private Single _startCost = 1;
		public Single StartCost
		{
			get { return _startCost; }
			set
			{
				if (value > 0)
				{
					_startCost = value;
					RemoveError(_lowStartCostError);
				}
				else
					AddError(_lowStartCostError ?? (_lowStartCostError = "1"));
			}
		}

		private string _lowAmountKilometersError = null;
		private Single _amountOfStartKilometers = 1;
		public Single AmountOfStartKilometers
		{ 
			get { return _amountOfStartKilometers; }
			set
			{
				if (value > 0)
				{
					_amountOfStartKilometers = value;
					RemoveError(_lowAmountKilometersError);
				}
				else
					AddError(_lowAmountKilometersError ?? (_lowAmountKilometersError = "1"));
			}
		}

		private string _lowKilometerCost = null;
		private Single _afterKilometerCost = 1;
		public Single AfterKilometerCost
		{
			get { return _afterKilometerCost; }
			set
			{
				if (value > 0)
				{
					_afterKilometerCost = value;
					RemoveError(_lowKilometerCost);
				}
				else
					AddError(_lowKilometerCost ?? (_lowKilometerCost = "1"));
			}
		}

		#region INotifyDataErrorInfo Members

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		public System.Collections.IEnumerable GetErrors(string propertyName)
		{
			if (String.IsNullOrEmpty(propertyName) ||
				!errors.Value.ContainsKey(propertyName)) return null;
			return errors.Value[propertyName];
		}

		public bool HasErrors
		{
			get { return errors.Value.Count > 0; }
		}

		private void AddError(string message, [CallerMemberName]string propertyName = "")
		{
			List<String> messages;
			if (!errors.Value.ContainsKey(propertyName))
			{
				messages = new List<String>();
				errors.Value.Add(propertyName, messages);
			}
			else
				messages = errors.Value[propertyName];

			if (messages.Contains(message) == false)
			{
				messages.Add(message);
				ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
			}
		}

		private void RemoveError(string message, [CallerMemberName]string propertyName = "")
		{
			if (message == null) return;
			List<String> messages;
			if (errors.Value.ContainsKey(propertyName))
				messages = errors.Value[propertyName];
			else
				return;

			if (messages.Contains(message))
			{
				messages.Remove(message);
				if (messages.Count == 0) errors.Value.Remove(propertyName);
				ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
			}
		}
		#endregion
	}
}