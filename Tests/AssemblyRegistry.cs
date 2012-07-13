using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests {
	public sealed class AssemblyRegistry<TKey, TElem> : MarshalByRefObject {
		private readonly Dictionary<TKey, List<TElem>> _registry;

		public AssemblyRegistry() {
			_registry = new Dictionary<TKey, List<TElem>>();
		}

		public Boolean Register(TKey key, TElem elem) {
			if (_registry.ContainsKey(key)) {
				_registry[key].Add(elem);
				return false;
			}
			
			_registry[key] = new List<TElem> { elem };
			return true;
		}

		public IEnumerable<List<TElem>> Duplicates() {
			return _registry.Values.Where(x => x.Count > 1);
		}
	}
}