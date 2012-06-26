﻿using System;
using System.Linq;
using System.Reflection;

namespace Pennyworth.Tests {
    [Test]
    public sealed class PublicFieldTest : AbstractTest {
        public PublicFieldTest(Assembly assembly, String path)
            : base(assembly, path) {}

        public override void Run() {
            _faultyMembers.AddRange(_assembly.GetTypes()
                .Where(t => !t.IsNested)
                .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Public))
                // Apparently, enums have a special public field named value__
                .Where(fi => fi.DeclaringType != null && !fi.DeclaringType.IsEnum));
        }
    }
}
