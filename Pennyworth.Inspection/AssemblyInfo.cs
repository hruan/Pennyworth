using System;

namespace Pennyworth.Inspection {
	[Serializable]
	public sealed class AssemblyInfo : IEquatable<AssemblyInfo>, IComparable<AssemblyInfo> {
		public Guid AssemblyId { get; set; }
		public Guid AssemblyGuid { get; set; }
		public String Path { get; set; }

		public int CompareTo(AssemblyInfo other)
		{
			if (ReferenceEquals(this, other))
				return 0;
			if (other == null)
				return 1;

			return AssemblyId.CompareTo(other.AssemblyId);
		}

		public Boolean Equals(AssemblyInfo other)
		{
			return other != null && AssemblyId.Equals(other.AssemblyId);
		}

		public override Boolean Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
				return true;

			return Equals(obj as AssemblyInfo);
		}

		public override int GetHashCode()
		{
			return AssemblyId.GetHashCode();
		}
	}
}
