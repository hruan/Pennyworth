using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pennyworth.Inspection {
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

		/// <summary>
		/// Check if <paramref name="value"/> is known in registry
		/// </summary>
		/// <param name="value">value to check</param>
		/// <param name="globally">whether to check the global registry</param>
		/// <returns>
		/// <c>true</c> if <paramref name="value"/> is known; <c>false</c>
		/// otherwise
		/// </returns>
		public Boolean Known(GuidInfo value, Boolean globally = false) {
			if (value.Guid == Guid.Empty) return false;

			var registry = globally ? _globalRegistry : _sessionRegistry;
			var idx      = Find(registry, value);

			registry.Add(value);

			if (!globally) return idx >= 0;
			return idx >= 0 && registry.Where(x => x.Guid == value.Guid)
				                   .Any(x => !x.Path.Equals(value.Path,
				                                            StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Find all duplicates in session registry
		/// </summary>
		/// <returns>found duplicates if any</returns>
		public IEnumerable<IGrouping<Guid, GuidInfo>> FindDuplicates() {
			return FindDuplicates(Guid.Empty);
		}

		/// <summary>
		/// Find duplicates of given <paramref name="value"/>
		/// </summary>
		/// <param name="value">duplicates of given value</param>
		/// <param name="globally">whether to search in the global registry</param>
		/// <returns>found duplicates for given <paramref name="value"/></returns>
		public IEnumerable<IGrouping<Guid, GuidInfo>> FindDuplicates(Guid value, Boolean globally = false) {
			var registry = globally ? _globalRegistry : _sessionRegistry;
			var dups = FindDuplicatesIn(registry);

			return value == Guid.Empty ? dups : dups.Where(gi => gi.Key == value);
		}

		private static IEnumerable<IGrouping<Guid, GuidInfo>> FindDuplicatesIn(IEnumerable<GuidInfo> list) {
			Debug.Assert(list != null);

			var lookup = list.ToLookup(x => x.Guid);
			return lookup.Where(x => lookup[x.Key].Count() > 1);
		}

		/// <summary>
		/// Search for <see cref="GuidInfo"/> within given list
		/// </summary>
		/// <param name="haystack">list within which to search for <paramref name="needle"/></param>
		/// <param name="needle">item to search for</param>
		/// <returns>
		/// index for the <paramref name="needle"/> in <paramref name="haystack"/>;
		/// less than zero if not found
		/// </returns>
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
