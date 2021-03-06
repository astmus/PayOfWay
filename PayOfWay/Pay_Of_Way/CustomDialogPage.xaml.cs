﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Pay_Of_Way
{
	public partial class CustomDialogPage : PhoneApplicationPage
	{
		public event Action<UIElement> dismissedWithOk;
		public UIElement CustomView
		{
			set
			{
				LayoutRoot.Children.Insert(0, value);
			}
			private get
			{
				return LayoutRoot.Children[0];
			}
		}

		public CustomDialogPage()
		{
			InitializeComponent();
		}

		private void Ok_Button_Click(object sender, RoutedEventArgs e)
		{			
			if (dismissedWithOk != null)
				dismissedWithOk(CustomView);
			NavigationService.GoBack();
		}

		private void Cancel_Button_Click(object sender, RoutedEventArgs e)
		{
			NavigationService.GoBack();
		}
	}
}