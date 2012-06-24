using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Pennyworth {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Icons from: http://www.iconfinder.com/browse/iconset/30_Free_Black_ToolBar_Icons/#readme
    /// </summary>
    public partial class MainWindow {
        private readonly BitmapImage _yayImage = new BitmapImage(new Uri("/Images/Yay.png", UriKind.Relative));
        private readonly BitmapImage _nayImage = new BitmapImage(new Uri("/Images/Nay.png", UriKind.Relative));

        public MainWindow() {
            InitializeComponent();

            Debug.Print("ApplicationBase in current app domain: {0}", AppDomain.CurrentDomain.BaseDirectory);
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            offendingMembers.ItemsSource = null;

            if (e.Data.GetDataPresent("FileDrop")) {
                var offendingItems = new List<OffendingMember>();
                var paths = ((IEnumerable<String>) e.Data.GetData("FileDrop"))
                    .Select(p => new FileInfo(p));

                foreach (var assembly in DropHelper.GetAssembliesFromDropData(paths)) {
                    var currentFile = assembly;
                    using (var helper = new AssemblyTestRunner()) {
                        offendingItems.AddRange(helper.RunTestsFor(currentFile));
                    }
                }

                offendingMembers.ItemsSource = offendingItems;
                imageResult.Source = offendingItems.Any() ? _nayImage : _yayImage;
                log.Items.Add("==== MARK ====");
            } else {
                Debug.WriteLine("No files.");
            }
        }

        private void Log(String message, Boolean timestamp = true) {
            if (timestamp) {
                log.Items.Add(String.Format("{0:o} {1}", DateTime.Now, message));
            } else {
                log.Items.Add(String.Format("{0}", message));
            }
        }
    }
}
