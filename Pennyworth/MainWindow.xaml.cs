using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using NLog;
using Tests;

namespace Pennyworth {
    public partial class MainWindow {
        private readonly BitmapImage _yayImage;
        private readonly BitmapImage _nayImage;

        private readonly Logger _logger;

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
                var basePath = DropHelper.GetBaseDir(paths);

                if (assemblies.Any()) {
                    using (var helper = new TestSession(basePath)) {
                        var testsRan = helper.RunTestsFor(assemblies);

                        imageResult.Source = testsRan && !helper.Faults.Any() ? _yayImage : _nayImage;
                        offendingMembers.ItemsSource = helper.Faults;
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
    }
}
