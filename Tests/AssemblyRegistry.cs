using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tests {
	[Serializable]
	public sealed class AssemblyRegistry {
		private readonly List<GuidInfo> _globalRegistry;

		[NonSerialized]
		private List<GuidInfo> _sessionRegistry;

		public AssemblyRegistry() {
			_globalRegistry = new List<GuidInfo>();
		}

		public void NewSession() {
			_sessionRegistry = new List<GuidInfo>();
		}

		public Boolean RegisterSession(GuidInfo info) {
			Debug.Assert(_sessionRegistry != null);

			var known = Exists(_sessionRegistry, info);
			_sessionRegistry.Add(info);

			return !known;
		}

		public Boolean RegisterGlobally(GuidInfo info) {
			if (info.Guid == Guid.Empty) return true;

			var idx = Find(_globalRegistry, info);

			_globalRegistry.Add(info);
			return idx < 0 || _globalRegistry[idx].Path.Equals(info.Path,
			                                                   StringComparison.OrdinalIgnoreCase);
		}

		public IEnumerable<IGrouping<Guid, GuidInfo>> SessionDuplicates() {
			return Duplicates(_sessionRegistry);
		}

		public IEnumerable<IGrouping<Guid, GuidInfo>> GlobalDuplicates(Guid guid) {
			return Duplicates(_globalRegistry).Where(gi => gi.Key == guid);
		}

		private static IEnumerable<IGrouping<Guid, GuidInfo>> Duplicates(IEnumerable<GuidInfo> list) {
			Debug.Assert(list != null);

			var lookup = list.ToLookup(x => x.Guid);
			return lookup.Where(x => lookup[x.Key].Count() > 1);
		}

		private static Boolean Exists(List<GuidInfo> haystack, GuidInfo needle) {
			return Find(haystack, needle) >= 0;
		}

		private static Int32 Find(List<GuidInfo> haystack, GuidInfo needle) {
			haystack.Sort();
			return haystack.BinarySearch(needle);
		}
	}

	[Serializable]
	public sealed class GuidInfo : IEquatable<GuidInfo>, IComparable<GuidInfo> {
		public Guid   Guid { get; set; }
		public String Path { get; set; }

		public int CompareTo(GuidInfo other) {
			if (ReferenceEquals(this, other)) return 0;
			if (other == null) return 1;

			return Guid.CompareTo(other.Guid);
		}

		public Boolean Equals(GuidInfo other) {
			return other != null && Guid.Equals(other.Guid);
		}

		public override Boolean Equals(object obj) {
			if (ReferenceEquals(this, obj)) return true;

			return Equals(obj as GuidInfo);
		}

		public override int GetHashCode() {
			return Guid.GetHashCode();
		}
	}
}
