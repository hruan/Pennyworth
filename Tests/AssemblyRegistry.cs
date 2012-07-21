using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests {
	[Serializable]
	public sealed class AssemblyRegistry {
		private readonly List<AssemblyInfo> _registry;

		public AssemblyRegistry() {
			_registry = new List<AssemblyInfo>();
		}

		public Boolean Register(AssemblyInfo info) {
			var knownKey = Exists(info);
			_registry.Add(info);

			return !knownKey;
		}

		public IEnumerable<IGrouping<Guid, AssemblyInfo>> Duplicates() {
			var lookup = _registry.ToLookup(x => x.ManifestGuid);
			return lookup.Where(x => lookup[x.Key].Count() > 1);
		}

		private Boolean Exists(AssemblyInfo needle) {
			_registry.Sort();
			return _registry.BinarySearch(needle) >= 0;
		}
	}

	[Serializable]
	public sealed class AssemblyInfo : IEquatable<AssemblyInfo>, IComparable<AssemblyInfo> {
		public Guid ManifestGuid { get; set; }
		public Guid AssemblyGuid { get; set; }
		public String Path       { get; set; }

		public int CompareTo(AssemblyInfo other) {
			if (ReferenceEquals(this, other)) return 0;
			if (other == null) return 1;

			return ManifestGuid.CompareTo(other.ManifestGuid);
		}

		public Boolean Equals(AssemblyInfo other) {
			if (other == null) return false;

			return ManifestGuid.Equals(other.ManifestGuid);
		}

		public override int GetHashCode() {
			return ManifestGuid.GetHashCode();
		}
	}
}
