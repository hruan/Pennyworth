﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;
using Pennyworth.Helpers;
using Tests;

namespace Pennyworth {
	public sealed partial class MainWindow {
		private readonly BitmapImage _yayImage;
		private readonly BitmapImage _nayImage;
		private readonly Logger      _logger;

		private static readonly TestSessionManager _sessionManager = new TestSessionManager();
		// private static readonly AssemblyRegistry   _registry       = RegistrySerializer.GetRegistry();

		public MainWindow() {
			InitializeComponent();

			_logger = LogManager.GetLogger(GetType().Name);
			var target = LogManager.Configuration.AllTargets.FirstOrDefault() as NLog.Targets.MemoryTarget;
			if (target != null) {
				log.ItemsSource = target.Logs;
			}

			// Icons from: http://www.iconfinder.com/browse/iconset/30_Free_Black_ToolBar_Icons/#readme
			_yayImage = new BitmapImage(new Uri("/Images/Yay.png", UriKind.Relative));
			_nayImage = new BitmapImage(new Uri("/Images/Nay.png", UriKind.Relative));

			versionLabel.Content = "Version: " + Assembly.GetExecutingAssembly().GetName().Version;
		}

		private void Window_Drop(object sender, DragEventArgs e) {
			imageResult.Source = null;
			offendingMembers.ItemsSource = null;

			if (e.Data.GetDataPresent("FileDrop")) {
				var paths = ((IEnumerable<String>) e.Data.GetData("FileDrop"))
					.Select(p => new FileInfo(p))
					.ToList();
				var assemblies = DropHelper.GetAssembliesFromDropData(paths).ToList();
				var basePath   = DropHelper.GetBaseDir(paths);

				if (assemblies.Any()) {
					using (var session = _sessionManager.CreateSession()) {
						assemblies.ForEach(x => _sessionManager.Assemblies.Add(x));
					}
				} else {
					_logger.Info("No assemblies found among dropped files.");
				}
			} else {
				_logger.Info("No files found among dropped items.");
			}

			log.Items.Refresh();
			if (log.HasItems) {
				var last = log.Items.Count - 1;
				log.ScrollIntoView(log.Items[last]);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (!RegistrySerializer.SaveRegistry()) {
				MessageBox.Show("Oops, something went wrong went saving assembly registry. Oh, well!");
			}
		}

		private void checkAssemblyGuid_Checked(object sender, RoutedEventArgs e) {
			BorderBrush = Brushes.Transparent;
			BorderThickness = new Thickness(0);
		}

		private void checkAssemblyGuid_Unchecked(object sender, RoutedEventArgs e) {
			BorderBrush = Brushes.Red;
			BorderThickness = new Thickness(2);
		}
	}
}
