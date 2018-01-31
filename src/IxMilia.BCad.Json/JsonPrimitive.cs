// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.BCad.Primitives;
using Newtonsoft.Json;

namespace IxMilia.BCad.Json
{
    public class JsonPrimitive : IPrimitive
    {
        [JsonIgnore]
        public IPrimitive BasePrimitive { get; }

        public CadColor? Color => BasePrimitive.Color;

        public PrimitiveKind Kind => BasePrimitive.Kind;

        public JsonPrimitive()
        {
        }

        public JsonPrimitive(IPrimitive basePrimitive)
        {
            BasePrimitive = basePrimitive;
        }
    }
}
