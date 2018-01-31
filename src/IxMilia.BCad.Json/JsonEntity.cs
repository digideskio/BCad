// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.BCad.Entities;
using Newtonsoft.Json;

namespace IxMilia.BCad.Json
{
    public class JsonEntity
    {
        [JsonIgnore]
        public Entity BaseEntity { get; }

        public uint Id => BaseEntity.Id;
        public EntityKind Kind => BaseEntity.Kind;
        public CadColor? Color => BaseEntity.Color;

        public JsonEntity()
        {
        }

        public JsonEntity(Entity baseEntity)
        {
            BaseEntity = baseEntity;
        }
    }
}
