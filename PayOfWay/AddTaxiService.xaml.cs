using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Input;
using System.Windows.Media;

namespace PayOfWay
{
	public partial class AddTaxiService : UserControl
	{
		public TaxProfile Profile { get; set; }
		public AddTaxiService(TaxProfile profile = null)
		{
			InitializeComponent();
			Profile = profile ?? new TaxProfile();
			DataContext = Profile;
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			var box = sender as TextBox;
			box.SelectAll();
		}

		private void LayoutRoot_BindingValidationError(object sender, ValidationErrorEventArgs e)
		{
			TextBox t = (TextBox)e.OriginalSource;
			if (e.Error.Exception is FormatException)
				t.Text = "1";
			else
				t.Text = e.Error.ErrorContent.ToString();
			
			e.Handled = true;
		}
	}
}
