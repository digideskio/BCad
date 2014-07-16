﻿using System.Collections.Generic;
using BCad.Entities;
using BCad.Utilities;

namespace BCad.Commands
{
    [ExportUICommand("Edit.Trim", "TRIM", "trim", "tr")]
    public class TrimCommand : AbstractTrimExtendCommand
    {
        protected override string GetBoundsText()
        {
            return "Select cutting edges";
        }

        protected override string GetTrimExtendText()
        {
            return "Entity to trim";
        }

        protected override void DoTrimExtend(SelectedEntity selectedEntity, IEnumerable<Primitives.IPrimitive> boundaryPrimitives, out IEnumerable<Entity> removed, out IEnumerable<Entity> added)
        {
            EditUtilities.Trim(selectedEntity, boundaryPrimitives, out removed, out added);
        }
    }
}