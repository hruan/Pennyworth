using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Reactive;

namespace Tests {
	public class TestSessionManager {
		private ObservableCollection<FaultInfo> _faults;
		private ObservableCollection<String> _assemblies;

		public ObservableCollection<FaultInfo> Faults {
			get { return _faults; }
		}

		public ObservableCollection<String> Assemblies {
			get { return _assemblies; }
		}

		public TestSessionManager() {
			_faults = new ObservableCollection<FaultInfo>();
			_assemblies = new ObservableCollection<String>();

			_assemblies.CollectionChanged += AssembliesOnCollectionChanged;
		}

		private void AssembliesOnCollectionChanged(Object sender, NotifyCollectionChangedEventArgs ev) {
			Debug.Print(ev.NewItems.ToString());
		}
	}
}
