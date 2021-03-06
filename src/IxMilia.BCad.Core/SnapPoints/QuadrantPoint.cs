﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.BCad.SnapPoints
{
    public class QuadrantPoint : SnapPoint
    {
        public QuadrantPoint(Point p)
            : base(p)
        {
        }

        public override SnapPointKind Kind
        {
            get { return SnapPointKind.Quadrant; }
        }
    }
}
