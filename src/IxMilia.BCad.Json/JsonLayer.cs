// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace IxMilia.BCad.Json
{
    public class JsonLayer
    {
        [JsonIgnore]
        public Layer BaseLayer { get; }

        public string Name => BaseLayer.Name;
        public CadColor? Color => BaseLayer.Color;
        public bool IsVisible => BaseLayer.IsVisible;
        public List<JsonEntity> Entities { get; } = new List<JsonEntity>();

        public JsonLayer()
        {
        }

        public JsonLayer(Layer baseLayer)
        {
            BaseLayer = baseLayer;
            Entities = BaseLayer.GetEntities().Select(e => new JsonEntity(e)).ToList();
        }
    }
}
