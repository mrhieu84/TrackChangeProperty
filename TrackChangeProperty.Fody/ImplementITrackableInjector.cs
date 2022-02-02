#pragma warning disable 618

using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeSystem = Mono.Cecil.TypeSystem;
using System;
using TrackChangeProperty.Fody;
using TrackChangePropertyLib;

internal class CustomAttribute2
{
    public CustomAttribute Attr { get; set; }

}
public class ImplementITrackableInjector
{
    private readonly MsCoreReferenceFinder msCoreReferenceFinder;
    private readonly TypeSystem typeSystem;
    private readonly List<TypeDefinition> _allPocoTypes;
    private readonly ModuleWeaver moduleWeaver;

    private InterfaceImplementation trackInterfaceImplementation = null;

    private TypeReference trackableTypeRef = null;
    private TypeReference dicPropTypeRef = null;

    public ImplementITrackableInjector(ModuleWeaver moduleWeaver, MsCoreReferenceFinder msCoreReferenceFinder,
        TypeSystem typeSystem, List<TypeDefinition> allPocoTypes)
    {
        this.moduleWeaver = moduleWeaver;
        this.msCoreReferenceFinder = msCoreReferenceFinder;
        this.typeSystem = typeSystem;
        this._allPocoTypes = allPocoTypes;
    }

    private Dictionary<string, CustomAttribute2> _TypeOfCustomAttribute = new Dictionary<string, CustomAttribute2>();

    private IEnumerable<TypeDefinition> GetHierarchy(TypeDefinition type)
    {

        while (type != null)
        {
            yield return type;
            if (type.BaseType == null)
            {
                type = null;
            }
            else
            {
                try
                {
                    type = type.BaseType?.Resolve();
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    type = null;
                }
            }
        }



    }



    private TypeDefinition _tokentype_ObservableBase;
    //  private TypeDefinition _tokentype_TrackingBase;

    public void Execute()
    {

        var PocoTypes = _allPocoTypes.Select(t => {
            // int count = 0;
            CustomAttribute2 attr2 = new CustomAttribute2();
            foreach (var w in GetHierarchy(t))
            {

                //  count++;

                var attr = w.GetTrackAttribute();
                if (attr != null) attr2.Attr = attr;

                _TypeOfCustomAttribute[w.FullName] = attr2;


            }

            return t;
        }).ToList();


        _tokentype_ObservableBase = moduleWeaver.ModuleDefinition.ImportReference(typeof(ObservableBase)).Resolve();
        //   _tokentype_TrackingBase = moduleWeaver.ModuleDefinition.ImportReference(typeof(TrackingBase)).Resolve();


        foreach (var type in PocoTypes)
        {
            if (!type.IsInterface && !type.IsValueType && !type.IsEnum)
            {
                InjectImplInterface(type);
            }
        }

    }

    //public bool HasInterface(TypeDefinition type, string interfaceFullName)
    //{
    //    return (type.Interfaces.Any(i => i.InterfaceType.FullName.Equals(interfaceFullName))
    //            || type.NestedTypes.Any(t => HasInterface(t, interfaceFullName)));
    //}
    //static CustomAttribute GetCustomBaseType(TypeDefinition type)
    //{
    //    var baseType = type.BaseType;

    //    while (baseType != null)
    //    {
    //        var definition = baseType.Resolve();
    //        if (definition == null)
    //            break;
    //        var attr = definition.GetTrackAttribute();
    //        if (attr != null) return attr;

    //        baseType = definition.BaseType;
    //    }

    //    return null;
    //}

    /// <summary>
    /// Process POCO Type implement ITrackable interface
    /// </summary>
    /// <param name="type">POCO Type</param>
    private void InjectImplInterface(TypeDefinition type)
    {
        // moduleWeaver.lo("Process Type impl Interface");
        _TypeOfCustomAttribute.TryGetValue(type.FullName, out CustomAttribute2 attr2);

        var attr = attr2?.Attr;
        if (attr != null)
        {
            // Check Type is Implement the ITrackable interface
            var hasITrackableInterface = type.Interfaces.Any(i => i.InterfaceType.Name == "ITrackable");
            if (hasITrackableInterface)
            {
                return;
            }

            if (trackableTypeRef == null)
            {
                var bronzeAssam = attr.AttributeType.Module.Assembly;
                var assName = attr.AttributeType.Scope.Name;
                var reference = attr.AttributeType.Module.AssemblyReferences.SingleOrDefault(a => a.Name == assName);
                if (reference != null)
                {
                    bronzeAssam = moduleWeaver.ModuleDefinition.AssemblyResolver.Resolve(reference);
                }

                TypeReference trackableType = bronzeAssam.MainModule.GetTypes().FirstOrDefault(t => t.Name.EndsWith(".ITrackable") || t.Name == "ITrackable");
                trackableTypeRef = this.moduleWeaver.ModuleDefinition.ImportReference(trackableType);
            }

            if (dicPropTypeRef == null)
            {
                dicPropTypeRef = trackableTypeRef.Resolve().Properties.SingleOrDefault(p => p.Name == "ModifiedProperties").PropertyType;
                dicPropTypeRef = moduleWeaver.ModuleDefinition.ImportReference(dicPropTypeRef);
            }
            var dicTypeDefinition = dicPropTypeRef.Resolve();

            // Add IsTracking property
            //var isTrackingProp = InjectProperty(type, "IsTracking", typeSystem.Boolean, true);
            //if (!isTrackingProp.FromBaseClass)
            //{
            //    isTrackingProp.Prop.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.NonSerializedReference));
            //    if (msCoreReferenceFinder.JsonIgnoreAttributeReference != null)
            //    {
            //        isTrackingProp.Prop.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.JsonIgnoreAttributeReference));
            //    }
            //}

            // Add ModifiedProperties property
            var modifiedPropertiesProp = InjectProperty(type, "ModifiedProperties", dicPropTypeRef, true);
            if (!modifiedPropertiesProp.FromBaseClass)
            {
                modifiedPropertiesProp.Prop.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.NonSerializedReference));
                if (msCoreReferenceFinder.JsonIgnoreAttributeReference != null)
                {
                    modifiedPropertiesProp.Prop.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.JsonIgnoreAttributeReference));
                }
            }

            // Implement the ITrackable interface
            if (trackInterfaceImplementation == null)
            {
                trackInterfaceImplementation = new InterfaceImplementation(trackableTypeRef);
            }
            type.Interfaces.Add(trackInterfaceImplementation);

            if (modifiedPropertiesProp.Field != null)
            {
                var dicCtor = dicTypeDefinition.GetConstructors().SingleOrDefault(c => c.Parameters.Count == 0);

                var dicConstructorInfoRef = type.Module.ImportReference(dicCtor);
                dicConstructorInfoRef = dicConstructorInfoRef.MakeGeneric(typeSystem.String, typeSystem.Boolean);

                var currentTypeCtor = type.GetConstructors().SingleOrDefault(c => c.Parameters.Count == 0);

                if (currentTypeCtor != null)
                {
                    var processor = currentTypeCtor.Body.GetILProcessor();
                    var firstInstruction = currentTypeCtor.Body.Instructions[1];

                    var instructions = new List<Instruction>() {
                        processor.Create(OpCodes.Ldarg_0),
                        processor.Create(OpCodes.Newobj, dicConstructorInfoRef),
                        processor.Create(OpCodes.Stfld, modifiedPropertiesProp.Field),
                    };

                    foreach (var instruction in instructions)
                    {
                        processor.InsertBefore(firstInstruction, instruction);
                    }
                }
            }

            ReferenceFinder _referenceFinder = new ReferenceFinder(moduleWeaver.ModuleDefinition);

            var typeTypeRef = _referenceFinder.GetTypeReference(typeof(Type));
            var methodReference_GetTypeFromHandle = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "GetTypeFromHandle" && md.Parameters.Count == 1);

            var methodInvokeMmber = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "InvokeMember" && md.Parameters.Count == 5);

            var typeTypeRef_2 = _referenceFinder.GetTypeReference(typeof(object));
            var GetTypeMd = _referenceFinder.GetMethodReference(typeTypeRef_2, md => md.Name == "GetType");
            var EqualsMd = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "op_Equality" && md.Parameters.Count == 2);

            var InEqualsMd = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "op_Inequality" && md.Parameters.Count == 2);

            var _set_ItemMothed = dicTypeDefinition.Methods.FirstOrDefault(m => m.Name == "set_Item");
            var set_ItemMethodRef = type.Module.ImportReference(_set_ItemMothed);
            set_ItemMethodRef = set_ItemMethodRef.MakeGeneric(typeSystem.String, typeSystem.Boolean);

            var getModifiedPropertiesMethod = moduleWeaver.ModuleDefinition.ImportReference(modifiedPropertiesProp.Prop.GetMethod);

            foreach (var property in type.Properties)
            {
                if (property.Name != "ModifiedProperties"
                    && property.SetMethod != null && property.SetMethod.IsPublic)
                {
                    var propFieldStr = $"<{property.Name}>k__BackingField";
                    var propFieldDef = type.Fields.SingleOrDefault(f => f.Name == propFieldStr);
                    if (propFieldDef == null)
                    {
                        continue;
                    }


                    var IsObservableType = property.PropertyType.Resolve().IsSubclassOf(_tokentype_ObservableBase);


                    // (property.PropertyType as GenericInstanceType).GenericArguments[0].Resolve().IsSubclassOf(_tokentype_TrackingBase);
                    var md = property.SetMethod;
                    md.Body.Instructions.Clear();

                    var ins1 = md.Body.Instructions;
                    md.Body.InitLocals = true;
                    ArrayType objArrType = new ArrayType(typeSystem.Object);
                    //md.Body.Variables.Add(new VariableDefinition(property.PropertyType));
                    var localObjectArray = new VariableDefinition(objArrType);

                    md.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                    md.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));


                    md.Body.Variables.Add(new VariableDefinition(objArrType));
                  
                    if (IsObservableType)
                    {
                        md.Body.Variables.Add(new VariableDefinition(property.PropertyType));

                        md.Body.Variables.Add(localObjectArray);
                        //IL_0000: nop
                        ins1.Add(Instruction.Create(OpCodes.Nop));

                        //IL_0001: ldarg.0
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_0));


                        //ins1.Add(Instruction.Create(OpCodes.Call, property.GetMethod));
                        ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));
                        ins1.Add(Instruction.Create(OpCodes.Box, property.PropertyType));

                        ins1.Add(Instruction.Create(OpCodes.Stloc_3));
                    }


                    //IL_0000: nop
                    ins1.Add(Instruction.Create(OpCodes.Nop));

                    //IL_0001: ldarg.0
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_0));


                    // ins1.Add(Instruction.Create(OpCodes.Call, property.GetMethod));
                    ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));
                    ins1.Add(Instruction.Create(OpCodes.Box, property.PropertyType));

                    //IL_000c: ldarg.1
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_1));

                    //IL_000d: box valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime>
                    ins1.Add(Instruction.Create(OpCodes.Box, property.PropertyType));
                    ins1.Add(Instruction.Create(OpCodes.Call, EqualsMd));

                    //IL_0017: stloc.0
                    ins1.Add(Instruction.Create(OpCodes.Stloc_0));

                    //IL_0018: ldloc.0
                    ins1.Add(Instruction.Create(OpCodes.Ldloc_0));

                    //IL_0019: ldc.i4.0
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_0));

                    //IL_001a: ceq
                    ins1.Add(Instruction.Create(OpCodes.Ceq));

                    //IL_001c: stloc.1
                    ins1.Add(Instruction.Create(OpCodes.Stloc_1));

                    //IL_001d: ldloc.1
                    ins1.Add(Instruction.Create(OpCodes.Ldloc_1));

                    //IL_001e: brfalse.s IL_0034
                    var IL_0034 = Instruction.Create(OpCodes.Nop);
                    ins1.Add(Instruction.Create(OpCodes.Brfalse, IL_0034));


                    //IL_0020: nop
                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                    //IL_0035: ldarg.1
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_1));

                    //IL_0036: stfld valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.DateTime> AssemblyToProcess.Class1::k__BackingField
                    ins1.Add(Instruction.Create(OpCodes.Stfld, propFieldDef));


                    //IL_0021: ldarg.0
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_0));

                    //IL_0022: call instance class [mscorlib]System.Collections.Generic.Dictionary`2<string, bool> AssemblyToProcess.Class1::get_ModifiedProperties()


                    ins1.Add(Instruction.Create(OpCodes.Call, getModifiedPropertiesMethod));

                    //IL_0027: ldstr "Prop1"
                    ins1.Add(Instruction.Create(OpCodes.Ldstr, property.Name));

                    //IL_002c: ldc.i4.1
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_1));

                    //IL_002d: callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string, bool>::set_Item(!0, !1)
                    //var _set_ItemMothed = typeof(Dictionary<string, bool>).GetMethod("set_Item", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance);

                    ins1.Add(Instruction.Create(OpCodes.Callvirt, set_ItemMethodRef));

                    //IL_0032: nop
                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    var IL_47 = Instruction.Create(OpCodes.Nop);
                    var IL_77 = Instruction.Create(OpCodes.Nop);

                    if (IsObservableType)
                    {
                        ins1.Add(Instruction.Create(OpCodes.Nop));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldnull));

                        ins1.Add(Instruction.Create(OpCodes.Cgt_Un));
                        ins1.Add(Instruction.Create(OpCodes.Stloc_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldloc_1));

                        ins1.Add(Instruction.Create(OpCodes.Brfalse_S, IL_47));

                        ins1.Add(Instruction.Create(OpCodes.Nop));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_3));
                        ins1.Add(Instruction.Create(OpCodes.Newarr, typeSystem.Object));

                        ins1.Add(Instruction.Create(OpCodes.Dup));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                        ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));

                        ins1.Add(Instruction.Create(OpCodes.Dup));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldloc_3));
                        ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));

                        ins1.Add(Instruction.Create(OpCodes.Dup));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_2));
                        ins1.Add(Instruction.Create(OpCodes.Ldstr, property.Name));
                        ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));

                        ins1.Add(Instruction.Create(OpCodes.Stloc_2));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_1));

                        ins1.Add(Instruction.Create(OpCodes.Callvirt, GetTypeMd));

                        ins1.Add(Instruction.Create(OpCodes.Ldstr, "OnParentCallPropertySet"));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4, 256));
                        ins1.Add(Instruction.Create(OpCodes.Ldnull));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldloc_2));


                        ins1.Add(Instruction.Create(OpCodes.Callvirt, methodInvokeMmber));
                        ins1.Add(Instruction.Create(OpCodes.Pop));
                    }


                    //IL_0033: nop
                    //  ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(Instruction.Create(OpCodes.Br_S, IL_77));
                    ins1.Add(IL_47);
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_2));
                    ins1.Add(Instruction.Create(OpCodes.Newarr, typeSystem.Object));

                    ins1.Add(Instruction.Create(OpCodes.Dup));
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_0));

                    ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                    ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));

                    ins1.Add(Instruction.Create(OpCodes.Dup));
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                    ins1.Add(Instruction.Create(OpCodes.Ldstr, property.Name));
                    ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));


                    ins1.Add(Instruction.Create(OpCodes.Stloc_S, localObjectArray));
                    ins1.Add(Instruction.Create(OpCodes.Ldloc_3));
                    ins1.Add(Instruction.Create(OpCodes.Callvirt, GetTypeMd));


                    ins1.Add(Instruction.Create(OpCodes.Ldstr, "OnParentSetNull"));
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4, 256));
                    ins1.Add(Instruction.Create(OpCodes.Ldnull));
                    ins1.Add(Instruction.Create(OpCodes.Ldloc_3));

                    ins1.Add(Instruction.Create(OpCodes.Ldloc_S, localObjectArray));
                    ins1.Add(Instruction.Create(OpCodes.Callvirt, methodInvokeMmber));
                    ins1.Add(Instruction.Create(OpCodes.Pop));
                    ins1.Add(Instruction.Create(OpCodes.Nop));


                    ins1.Add(IL_77);



                    //IL_0034: ldarg.0
                    ins1.Add(IL_0034);


                    //IL_003b: ret
                    ins1.Add(Instruction.Create(OpCodes.Ret));

                    md = property.GetMethod;
                    md.Body.Instructions.Clear();

                    ins1 = md.Body.Instructions;
                    md.Body.InitLocals = true;
                    md.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                    md.Body.Variables.Add(new VariableDefinition(property.PropertyType));

                    ins1.Add(Instruction.Create(OpCodes.Nop));

                    var IL_26 = Instruction.Create(OpCodes.Ldarg_0);

                    if (IsObservableType)
                    {

                        ins1.Add(Instruction.Create(OpCodes.Nop));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));
                        ins1.Add(Instruction.Create(OpCodes.Ldnull));

                        ins1.Add(Instruction.Create(OpCodes.Cgt_Un));
                        ins1.Add(Instruction.Create(OpCodes.Stloc_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldloc_0));

                        ins1.Add(Instruction.Create(OpCodes.Brfalse_S, IL_26));

                        ins1.Add(Instruction.Create(OpCodes.Nop));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));
                        ins1.Add(Instruction.Create(OpCodes.Callvirt, GetTypeMd));

                        ins1.Add(Instruction.Create(OpCodes.Ldstr, "OnParentCallPropertyGet"));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4, 256));
                        ins1.Add(Instruction.Create(OpCodes.Ldnull));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));

                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_2));

                        ins1.Add(Instruction.Create(OpCodes.Newarr, typeSystem.Object));
                        ins1.Add(Instruction.Create(OpCodes.Dup));

                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_0));

                        ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));

                        ins1.Add(Instruction.Create(OpCodes.Dup));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldstr, property.Name));
                        ins1.Add(Instruction.Create(OpCodes.Stelem_Ref));

                        ins1.Add(Instruction.Create(OpCodes.Callvirt, methodInvokeMmber));
                        ins1.Add(Instruction.Create(OpCodes.Pop));

                    }


                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(IL_26);

                    ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));

                    ins1.Add(Instruction.Create(OpCodes.Stloc_1));

                    var IL_30 = Instruction.Create(OpCodes.Ldloc_1);
                    ins1.Add(Instruction.Create(OpCodes.Br_S, IL_30));
                    ins1.Add(IL_30);
                    ins1.Add(Instruction.Create(OpCodes.Ret));


                }
            }







        }
    }

    public class PropAndField
    {
        public bool FromBaseClass { get; set; }

        public PropertyDefinition Prop { get; set; }
        public FieldDefinition Field { get; set; }

        public PropAndField()
        {

        }

        public PropAndField(PropertyDefinition prop, FieldDefinition field, bool fromBaseClass)
        {
            this.Prop = prop;
            this.Field = field;
            this.FromBaseClass = fromBaseClass;
        }
    }

    private PropAndField InjectProperty(TypeDefinition typeDefinition, string propName, TypeReference propType, bool isPublic)
    {
        var name = propName;
        if (typeDefinition.HasGenericParameters)
        {
            var message =
                $"Skipped public field '{typeDefinition.Name}.{name}' because generic types are not currently supported. You should make this a public property instead.";
            //  moduleWeaver.LogWarning(message);
            return null;
        }

        // »ñÈ¡±¾Àà»ò»ùÀàÊÇ·ñ´æÔÚ ÏàÍ¬µÄÊôÐÔ
        Func<TypeDefinition, PropAndField> GetExistProp = null;
        GetExistProp = (TypeDefinition type) =>
        {
            try
            {
                var prop = type.Properties.SingleOrDefault(p => p.Name == propName);
                if (prop != null)
                {
                    return new PropAndField(prop, null, true);
                }
                else
                {
                    if (type.BaseType.FullName != "System.Object")
                    {
                        var baseType = type.BaseType.Resolve();
                        return GetExistProp(baseType);
                    }
                }
            }
            catch (System.Exception ex1)
            {
                var ss = ex1.Message;
            }

            return null;
        };


        var result = GetExistProp(typeDefinition);
        if (result != null)
        {
            return result;
        }

        FieldDefinition field = new FieldDefinition(name, FieldAttributes.Private, propType);
        field.Name = $"<{name}>k__BackingField";
        field.IsPublic = false;
        field.IsPrivate = true;

        typeDefinition.Fields.Add(field);

        var get = InjectPropertyGet(field, name, isPublic);
        typeDefinition.Methods.Add(get);

        var propertyDefinition = new PropertyDefinition(name, PropertyAttributes.None, field.FieldType)
        {
            HasThis = false,//Õâ¸öµØ·½Ä¬ÈÏÎªtrue£¬Õý³£ÊôÐÔÓ¦¸ÃÎªtrue
            GetMethod = get
        };


        var isReadOnly = field.Attributes.HasFlag(FieldAttributes.InitOnly);
        if (!isReadOnly)
        {
            var set = InjectPropertySet(field, name, isPublic);
            typeDefinition.Methods.Add(set);
            propertyDefinition.SetMethod = set;
        }
        foreach (var customAttribute in field.CustomAttributes)
        {
            propertyDefinition.CustomAttributes.Add(customAttribute);
        }
        field.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.CompilerGeneratedReference));

        typeDefinition.Properties.Add(propertyDefinition);
        propertyDefinition.HasThis = false;
        return new PropAndField(propertyDefinition, field, false);
    }

    private MethodDefinition InjectPropertyGet(FieldDefinition field, string name, bool isPublic = true)
    {
        MethodAttributes methodAttributes = MethodAttributes.HideBySig | MethodAttributes.Virtual;
        if (isPublic)
        {
            methodAttributes = MethodAttributes.Public | methodAttributes;
        }
        else
        {
            methodAttributes = MethodAttributes.Private | methodAttributes;
        }

        var get = new MethodDefinition("get_" + name, methodAttributes, field.FieldType);
        var instructions = get.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
        instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        var inst = Instruction.Create(OpCodes.Ldloc_0);
        instructions.Add(Instruction.Create(OpCodes.Br_S, inst));
        instructions.Add(inst);
        instructions.Add(Instruction.Create(OpCodes.Ret));
        get.Body.Variables.Add(new VariableDefinition(field.FieldType));
        get.Body.InitLocals = true;
        get.SemanticsAttributes = MethodSemanticsAttributes.Getter;
        get.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.CompilerGeneratedReference));
        return get;
    }

    private MethodDefinition InjectPropertySet(FieldDefinition field, string name, bool isPublic = true)
    {
        MethodAttributes methodAttributes = MethodAttributes.HideBySig | MethodAttributes.Virtual;
        if (isPublic)
        {
            methodAttributes = MethodAttributes.Public | methodAttributes;
        }
        else
        {
            methodAttributes = MethodAttributes.Private | methodAttributes;
        }

        var set = new MethodDefinition("set_" + name, methodAttributes, typeSystem.Void);
        var instructions = set.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        instructions.Add(Instruction.Create(OpCodes.Stfld, field));
        instructions.Add(Instruction.Create(OpCodes.Ret));
        set.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, field.FieldType));
        set.SemanticsAttributes = MethodSemanticsAttributes.Setter;
        set.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.CompilerGeneratedReference));
        return set;
    }
}
#pragma warning restore 618