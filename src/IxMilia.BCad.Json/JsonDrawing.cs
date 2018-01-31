// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace IxMilia.BCad.Json
{
    public class JsonDrawing
    {
        [JsonIgnore]
        public Drawing BaseDrawing { get; }

        public List<JsonLayer> Layers { get; } = new List<JsonLayer>();

        public JsonDrawing()
        {
        }

        public JsonDrawing(Drawing baseDrawing)
        {
            BaseDrawing = baseDrawing;
            Layers = BaseDrawing.GetLayers().Select(l => new JsonLayer(l)).ToList();
        }
    }
}
