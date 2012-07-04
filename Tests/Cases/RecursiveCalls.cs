﻿using System;
using System.Reflection;

namespace Tests.Cases {
    [TestCase("Recursion")]
    public sealed class RecursiveCalls : AbstractTest {
        public RecursiveCalls(Assembly assembly, String path)
            : base(assembly, path) { }

        public override void Run() {
            _faultyMembers.AddRange(new MethodCallHelper(_assembly).GetRecursiveCalls());
        }
    }
}