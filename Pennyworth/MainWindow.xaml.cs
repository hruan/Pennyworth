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
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            offendingMembers.ItemsSource = null;

            if (e.Data.GetDataPresent("FileDrop")) {
                var offendingItems = new List<OffendingMember>();
                var omc            = new OffendingMemberComparer();
                var paths = ((IEnumerable<String>) e.Data.GetData("FileDrop"))
                    .Select(p => new FileInfo(p));

                imageResult.Source = _yayImage;
                foreach (var assembly in DropHelper.GetAssembliesFromDropData(paths)) {
                    var currentFile = assembly;
                    var tester      = new AssemblyTest(currentFile);

                    Log(String.Format("Peeking inside {0}", currentFile));

                    if (tester.HasPublicFields()) {
                        Log(String.Format("{0} has public fields!", currentFile));
                        imageResult.Source = _nayImage;
                        offendingItems.AddRange(tester.PublicFields.Select(fi => new OffendingMember {
                            Path       = currentFile,
                            MemberInfo = fi
                        }).Distinct(omc));
                    }

                    if (tester.GetRecursiveMembers().Any()) {
                        Log(String.Format("{0} has recursive members!", currentFile));
                        imageResult.Source = _nayImage;
                        offendingItems.AddRange(tester.GetRecursiveMembers().Select(methodInfo => new OffendingMember {
                            Path       = currentFile,
                            MemberInfo = methodInfo
                        }).Distinct(omc));
                    }
                }

                offendingMembers.ItemsSource = offendingItems;
                log.Items.Add("==== MARK ====");
                log.ScrollIntoView(log.Items[log.Items.Count - 1]);
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

    internal struct OffendingMember {
        public String Path { get; set; }
        public MemberInfo MemberInfo { get; set; }
    }

    internal class OffendingMemberComparer : EqualityComparer<OffendingMember> {
        public override bool Equals(OffendingMember x, OffendingMember y) {
            return x.MemberInfo.DeclaringType == y.MemberInfo.DeclaringType
                   && x.MemberInfo.Name == y.MemberInfo.Name;
        }

        public override int GetHashCode(OffendingMember obj) {
            return obj.MemberInfo.DeclaringType.GetHashCode();
        }
    }
}
