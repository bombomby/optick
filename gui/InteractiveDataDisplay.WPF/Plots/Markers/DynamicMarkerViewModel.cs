// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Globalization;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Class that inherits <see cref="MarkerViewModel"/> and is created dynamically. 
    /// Provides properties for bindings to get values from <see cref="DataSeries"/> by its key.
    /// </summary>
    public class DynamicMarkerViewModel : MarkerViewModel
    {
        /// <summary>
        /// Creates a new instance of <see cref="DynamicMarkerViewModel"/> class.
        /// </summary>
        /// <param name="row">The number of marker.</param>
        /// <param name="collection">A collection of data series.</param>
        /// <param name="converters">A value indicating should converters be used or not.</param>
        public DynamicMarkerViewModel(int row, DataCollection collection, bool converters)
            : base(row, collection, converters) { }
    }
    /// <summary>
    /// A wrapper to <see cref="DataCollection"/> that is created dynamically. 
    /// Provides generated properties for bindings to get <see cref="DataSeries"/> from collection by key.
    /// </summary>
    public class DynamicDataCollection
    {
        private DataCollection dataCollection;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicDataCollection"/> class.
        /// </summary>
        /// <param name="collection">A collection of data series.</param>
        public DynamicDataCollection(DataCollection collection)
        {
            dataCollection = collection;
        }

        /// <summary>
        /// Gets <see cref="DataSeries"/> from <see cref="DataCollection"/> by index.
        /// </summary>
        /// <param name="i">Index of required data series.</param>
        /// <returns>Data series of specific index.</returns>
        public DataSeries GetDataSeries(int i)
        {
            return dataCollection[i];
        }
    }
    internal static class DynamicTypeGenerator
    {
        private static ModuleBuilder mb = null;
        private static int typeModelCount = 0;
        private static int typeCollectionCount = 0;

        public static Type GenerateMarkerViewModelType(DataCollection collection)
        {
            if (mb == null)
            {
                AssemblyName aName = new AssemblyName("InteractiveDataDisplayAssembly2");
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
                mb = ab.DefineDynamicModule(aName.Name);
            }
            TypeBuilder tb = mb.DefineType("DynamicMarkerViewModel_" + (typeModelCount++).ToString(CultureInfo.InvariantCulture),
                                            TypeAttributes.Public,
                                            typeof(DynamicMarkerViewModel));

            Type[] parameterTypes = { typeof(int), typeof(DataCollection), typeof(bool) };
            ConstructorBuilder ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
            ILGenerator ctorIL = ctor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_1);
            ctorIL.Emit(OpCodes.Ldarg_2);
            ctorIL.Emit(OpCodes.Ldarg_3);
            ctorIL.Emit(OpCodes.Call, typeof(DynamicMarkerViewModel).GetConstructor(parameterTypes));
            ctorIL.Emit(OpCodes.Ret);
            for (int i = 0; i < collection.Count; i++)
            {
                string name = collection[i].Key;

                if (name != "X")
                {
                    PropertyBuilder property = tb.DefineProperty(name, PropertyAttributes.None, typeof(object), null);
                    MethodAttributes getAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                    MethodBuilder propertyGetAccessor = tb.DefineMethod("get_" + name, getAttr, typeof(object), null);
                    ILGenerator propertyGetIL = propertyGetAccessor.GetILGenerator();
                    MethodInfo method = typeof(MarkerViewModel).GetMethod("GetValue");
                    propertyGetIL.Emit(OpCodes.Ldarg_0);
                    propertyGetIL.Emit(OpCodes.Ldc_I4, i);
                    propertyGetIL.Emit(OpCodes.Call, method);
                    propertyGetIL.Emit(OpCodes.Ret);
                    property.SetGetMethod(propertyGetAccessor);

                    PropertyBuilder propertyOriginal = tb.DefineProperty("Original" + name, PropertyAttributes.None, typeof(object), null);
                    MethodAttributes getAttrOriginal = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                    MethodBuilder propertyOriginalGetAccessor = tb.DefineMethod("get_Original" + name, getAttrOriginal, typeof(object), null);
                    ILGenerator propertyOriginalGetIL = propertyOriginalGetAccessor.GetILGenerator();
                    MethodInfo methodOriginal = typeof(MarkerViewModel).GetMethod("GetOriginalValue");
                    propertyOriginalGetIL.Emit(OpCodes.Ldarg_0);
                    propertyOriginalGetIL.Emit(OpCodes.Ldc_I4, i);
                    propertyOriginalGetIL.Emit(OpCodes.Call, methodOriginal);
                    propertyOriginalGetIL.Emit(OpCodes.Ret);
                    propertyOriginal.SetGetMethod(propertyOriginalGetAccessor);
                }
            }
            return tb.CreateType();
        }
        public static Type GenerateDataCollectionType(DataCollection collection)
        {
            if (mb == null)
            {
                AssemblyName aName = new AssemblyName("InteractiveDataDisplayAssembly1");
                AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
                mb = ab.DefineDynamicModule(aName.Name);
            }

            TypeBuilder tb = mb.DefineType("DynamicDataCollection_" + (typeCollectionCount++).ToString(CultureInfo.InvariantCulture),
                                            TypeAttributes.Public,
                                            typeof(DynamicDataCollection));
            Type[] parameterTypes = { typeof(DataCollection) };
            ConstructorBuilder ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
            ILGenerator ctorIL = ctor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_1);
            ctorIL.Emit(OpCodes.Call, typeof(DynamicDataCollection).GetConstructor(parameterTypes));
            ctorIL.Emit(OpCodes.Ret);
            for (int i = 0; i < collection.Count; i++)
            {
                string name = collection[i].Key;

                PropertyBuilder property = tb.DefineProperty(name, PropertyAttributes.None, typeof(DataSeries), null);
                MethodAttributes getAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                MethodBuilder propertyGetAccessor = tb.DefineMethod("get_" + name, getAttr, typeof(DataSeries), null);
                ILGenerator propertyGetIL = propertyGetAccessor.GetILGenerator();
                MethodInfo method = typeof(DynamicDataCollection).GetMethod("GetDataSeries");
                propertyGetIL.Emit(OpCodes.Ldarg_0);
                propertyGetIL.Emit(OpCodes.Ldc_I4, i);
                propertyGetIL.Emit(OpCodes.Call, method);
                propertyGetIL.Emit(OpCodes.Ret);
                property.SetGetMethod(propertyGetAccessor);
            }
            return tb.CreateType();
        }
    }
}