﻿using IxMilia.Dxf.Sections;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Tables
{
    public class DxfLayerTable : DxfTable
    {
        public override DxfTableType TableType
        {
            get { return DxfTableType.Layer; }
        }

        public List<DxfLayer> Layers { get; private set; }

        public DxfLayerTable()
            : this(new DxfLayer[0])
        {
        }

        public DxfLayerTable(IEnumerable<DxfLayer> layers)
        {
            Layers = new List<DxfLayer>(layers);
        }

        internal override IEnumerable<DxfCodePair> GetValuePairs()
        {
            if (Layers.Count == 0)
                yield break;
            yield return new DxfCodePair(0, DxfSection.TableText);
            yield return new DxfCodePair(2, DxfTable.LayerText);
            foreach (var layer in Layers.OrderBy(l => l.Name))
            {
                foreach (var pair in layer.GetValuePairs())
                    yield return pair;
            }

            yield return new DxfCodePair(0, DxfSection.EndTableText);
        }

        internal static DxfLayerTable LayerTableFromBuffer(DxfCodePairBufferReader buffer)
        {
            var table = new DxfLayerTable();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                buffer.Advance();
                if (DxfTablesSection.IsTableEnd(pair))
                {
                    break;
                }

                if (pair.Code == 0 && pair.StringValue == DxfLayerTable.LayerText)
                {
                    var layer = DxfLayer.FromBuffer(buffer);
                    table.Layers.Add(layer);
                }
                else
                {
                    // TODO: layer table options
                }
            }

            return table;
        }
    }
}