using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Pennyworth.Tests {
	public static class StubAssemblyBuilder {
		public static Assembly Create(String name)
		{
			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.RunAndSave, AppDomain.CurrentDomain.BaseDirectory);
			var mb = ab.DefineDynamicModule(name);

			// Create type with public fields
			var pubFieldsType = mb.DefineType(name + "_PublicField", TypeAttributes.Class | TypeAttributes.Public);
			pubFieldsType.DefineField("ThisIsAPublicField", typeof(Int32), FieldAttributes.Public);
			pubFieldsType.CreateType();

			// Create type with a recursive method
			var recMethodType = mb.DefineType(name + "_RecursiveMethod", TypeAttributes.Class | TypeAttributes.Public);
			var recMethodBuilder = recMethodType.DefineMethod("ThisIsARecursiveMethod", MethodAttributes.Public);
			var recMethodIl = recMethodBuilder.GetILGenerator();
			recMethodIl.Emit(OpCodes.Callvirt, recMethodBuilder);
			recMethodIl.Emit(OpCodes.Ret);
			recMethodType.CreateType();

			// Create type with two methods which call one another
			var inRecurType = mb.DefineType(name + "_IndirectRecursiveMethod", TypeAttributes.Class | TypeAttributes.Public);
			var inRecur1 = inRecurType.DefineMethod("IndirectRecursiveA", MethodAttributes.Public);
			var inRecur2 = inRecurType.DefineMethod("IndirectRecursiveB", MethodAttributes.Public);

			var inRecurIl1 = inRecur1.GetILGenerator();
			inRecurIl1.Emit(OpCodes.Callvirt, inRecur2);
			inRecurIl1.Emit(OpCodes.Ret);

			var inRecurIl2 = inRecur2.GetILGenerator();
			inRecurIl2.Emit(OpCodes.Callvirt, inRecur1);
			inRecurIl2.Emit(OpCodes.Ret);
			inRecurType.CreateType();

			return ab;
		}
	}
}
