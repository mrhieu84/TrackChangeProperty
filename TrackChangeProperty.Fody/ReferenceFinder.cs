﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackChangeProperty.Fody
{
    class ReferenceFinder
    {
        private readonly ModuleDefinition _moduleDefinition;

        public ReferenceFinder(ModuleDefinition moduleDefinition)
        {
            _moduleDefinition = moduleDefinition;
        }

        public MethodReference GetMethodReference(Type declaringType, Func<MethodDefinition, bool> predicate)
        {
            return GetMethodReference(GetTypeReference(declaringType), predicate);
        }

        public MethodReference GetMethodReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
        {
            var typeDefinition = typeReference.Resolve();

            MethodDefinition methodDefinition;
            do
            {
                methodDefinition = typeDefinition.Methods.FirstOrDefault(predicate);
                typeDefinition = typeDefinition.BaseType == null
                    ? null
                    : typeDefinition.BaseType.Resolve();
            } while (methodDefinition == null && typeDefinition != null);

            return _moduleDefinition.ImportReference(methodDefinition);
        }

        public TypeReference GetTypeReference(Type type)
        {
            return _moduleDefinition.ImportReference(type);
        }
    }
}
