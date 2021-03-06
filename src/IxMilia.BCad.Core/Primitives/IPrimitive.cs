﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.BCad.Primitives
{
    public interface IPrimitive
    {
        CadColor? Color { get; }
        PrimitiveKind Kind { get; }
    }
}
