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

    private  IEnumerable<TypeDefinition> GetHierarchy(TypeDefinition type)
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
       
        var orderdPocoTypes = _allPocoTypes.OrderBy(t => {
            int count = 0;
            CustomAttribute2 attr2 = new CustomAttribute2() ;
            foreach (var w in GetHierarchy(t))
            {

                count++;
               
                    var attr = w.GetTrackAttribute();
                    if (attr != null) attr2.Attr = attr;

                    _TypeOfCustomAttribute[w.FullName]= attr2;
          
               
            }

            return count;
            }).ToList();


        _tokentype_ObservableBase = moduleWeaver.ModuleDefinition.ImportReference(typeof(ObservableBase)).Resolve();
     //   _tokentype_TrackingBase = moduleWeaver.ModuleDefinition.ImportReference(typeof(TrackingBase)).Resolve();


        foreach (var type in orderdPocoTypes)
        {
            if (!type.IsInterface && !type.IsValueType && !type.IsEnum)
            {
                InjectImplInterface(type);
            }
        }
        
    }

    public bool HasInterface(TypeDefinition type, string interfaceFullName)
    {
        return (type.Interfaces.Any(i => i.InterfaceType.FullName.Equals(interfaceFullName))
                || type.NestedTypes.Any(t => HasInterface(t, interfaceFullName)));
    }
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
        if (attr != null  )
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
            var methodReference_GetTypeFromHandle = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "GetTypeFromHandle"  && md.Parameters.Count==1);
           
            var methodInvokeMmber =  _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "InvokeMember" && md.Parameters.Count == 5);

            var typeTypeRef_2 = _referenceFinder.GetTypeReference(typeof(object));
            var  GetTypeMd =   _referenceFinder.GetMethodReference(typeTypeRef_2, md => md.Name == "GetType" );
            var EqualsMd = _referenceFinder.GetMethodReference(typeTypeRef, md => md.Name == "op_Equality" && md.Parameters.Count==2);

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
                    var propFieldDef =  type.Fields.SingleOrDefault(f => f.Name == propFieldStr);
                    if (propFieldDef == null)
                    {
                        continue;
                    }

                   
                    var IsObservableListType = property.PropertyType.Resolve().IsSubclassOf(_tokentype_ObservableBase);


                    // (property.PropertyType as GenericInstanceType).GenericArguments[0].Resolve().IsSubclassOf(_tokentype_TrackingBase);
                    var md = property.SetMethod;
                    md.Body.Instructions.Clear();

                    var ins1 = md.Body.Instructions;
                    md.Body.InitLocals = true;
                    ArrayType objArrType = new ArrayType(typeSystem.Object);
                    md.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                    md.Body.Variables.Add(new VariableDefinition(objArrType));

                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                    ins1.Add(Instruction.Create(OpCodes.Ldfld, propFieldDef));
                    ins1.Add(Instruction.Create(OpCodes.Box, property.PropertyType));
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                    ins1.Add(Instruction.Create(OpCodes.Box, property.PropertyType));

                    ins1.Add(Instruction.Create(OpCodes.Call, EqualsMd));
                    ins1.Add(Instruction.Create(OpCodes.Stloc_0));
                    ins1.Add(Instruction.Create(OpCodes.Ldloc_0));
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    ins1.Add(Instruction.Create(OpCodes.Ceq)); 
                    ins1.Add(Instruction.Create(OpCodes.Stloc_1));
                    ins1.Add(Instruction.Create(OpCodes.Ldloc_1));
                    var IL_0034 = Instruction.Create(OpCodes.Ldarg_0);
                    ins1.Add(Instruction.Create(OpCodes.Brfalse_S, IL_0034));
                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(Instruction.Create(OpCodes.Ldarg_0));
                    ins1.Add(Instruction.Create(OpCodes.Call, getModifiedPropertiesMethod));
                    ins1.Add(Instruction.Create(OpCodes.Ldstr, property.Name));
                    ins1.Add(Instruction.Create(OpCodes.Ldc_I4_1));

                    
                    ins1.Add(Instruction.Create(OpCodes.Callvirt, set_ItemMethodRef));


                    if (IsObservableListType)
                    {

                        ins1.Add(Instruction.Create(OpCodes.Nop));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldnull));

                        ins1.Add(Instruction.Create(OpCodes.Cgt_Un));
                        ins1.Add(Instruction.Create(OpCodes.Stloc_0));
                        ins1.Add(Instruction.Create(OpCodes.Ldloc_0));
                        ins1.Add(Instruction.Create(OpCodes.Brfalse_S, IL_0034));

                        ins1.Add(Instruction.Create(OpCodes.Nop));
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
                        ins1.Add(Instruction.Create(OpCodes.Stloc_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                        ins1.Add(Instruction.Create(OpCodes.Callvirt, GetTypeMd));

                        ins1.Add(Instruction.Create(OpCodes.Ldstr, "OnParentCallPropertySet"));
                        ins1.Add(Instruction.Create(OpCodes.Ldc_I4,256));
                        ins1.Add(Instruction.Create(OpCodes.Ldnull));
                        ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                        ins1.Add(Instruction.Create(OpCodes.Ldloc_1));
                   

                        ins1.Add(Instruction.Create(OpCodes.Callvirt, methodInvokeMmber));
                        ins1.Add(Instruction.Create(OpCodes.Pop));

                    }

                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(Instruction.Create(OpCodes.Nop));
                    ins1.Add(IL_0034);

                    ins1.Add(Instruction.Create(OpCodes.Ldarg_1));
                    ins1.Add(Instruction.Create(OpCodes.Stfld, propFieldDef));
                    ins1.Add(Instruction.Create(OpCodes.Ret));


                    md = property.GetMethod;
                    md.Body.Instructions.Clear();

                     ins1 = md.Body.Instructions;
                    md.Body.InitLocals = true;
                    md.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                    md.Body.Variables.Add(new VariableDefinition(property.PropertyType));

                    ins1.Add(Instruction.Create(OpCodes.Nop));

                    var IL_26 = Instruction.Create(OpCodes.Ldarg_0);

                    if (IsObservableListType)
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

        // 获取本类或基类是否存在 相同的属性
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
            HasThis = false,//这个地方默认为true，正常属性应该为true
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