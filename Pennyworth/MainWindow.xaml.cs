using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

using NLog;

namespace Pennyworth {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Icons from: http://www.iconfinder.com/browse/iconset/30_Free_Black_ToolBar_Icons/#readme
    /// </summary>
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

            _yayImage = new BitmapImage(new Uri("/Images/Yay.png", UriKind.Relative));
            _nayImage = new BitmapImage(new Uri("/Images/Nay.png", UriKind.Relative));
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent("FileDrop")) {
                var paths = ((IEnumerable<String>) e.Data.GetData("FileDrop"))
                    .Select(p => new FileInfo(p));

                using (var helper = new AssemblyTestRunner()) {
                    helper.RunTestsFor(DropHelper.GetAssembliesFromDropData(paths));

                    imageResult.Source = helper.Offences.Any() ? _nayImage : _yayImage;
                    offendingMembers.ItemsSource = helper.Offences;
                }
            } else {
                _logger.Info("No assemblies found among dropped data.");
            }

            log.Items.Refresh();
        }
    }
}
