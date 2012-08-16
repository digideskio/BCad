﻿using System;
using System.Linq;
using System.Windows.Media.Media3D;
using BCad.Entities;
using BCad.Primitives;

namespace BCad.Extensions
{
    public static class PlaneExtensions
    {
        public static bool Contains(this Plane plane, Point p)
        {
            var v = p - plane.Point;
            return v.IsZeroVector || v.IsOrthoganalTo(plane.Normal);
        }

        public static bool Contains(this Plane plane, IPrimitive primitive)
        {
            switch (primitive.Kind)
            {
                case PrimitiveKind.Ellipse:
                    var el = (PrimitiveEllipse)primitive;
                    return plane.Contains(el.Center)
                        && plane.Normal.IsParallelTo(el.Normal)
                        && plane.Normal.IsOrthoganalTo(el.MajorAxis);
                case PrimitiveKind.Line:
                    var line = (PrimitiveLine)primitive;
                    return plane.Contains(line.P1) && plane.Contains(line.P2);
                case PrimitiveKind.Text:
                    var t = (PrimitiveText)primitive;
                    return plane.Contains(t.Location)
                        && plane.Normal.IsParallelTo(t.Normal);
                default:
                    throw new ArgumentException("primitive.Kind");
            }
        }

        public static bool Contains(this Plane plane, Entity entity)
        {
            return entity.GetPrimitives().All(p => plane.Contains(p));
        }

        public static Matrix3D ToXYPlaneProjection(this Plane plane)
        {
            var right = Vector.XAxis;
            if (plane.Normal.IsParallelTo(right))
                right = Vector.YAxis;
            var up = right.Cross(plane.Normal).Normalize();
            right = up.Cross(plane.Normal).Normalize();
            var matrix = PrimitiveExtensions.FromUnitCircleProjection(plane.Normal, right, up, Point.Origin, 1.0, 1.0, 1.0);
            return matrix;
        }

        public static Point ToXYPlane(this Plane plane, Point point)
        {
            return point.Transform(plane.ToXYPlaneProjection());
        }

        public static Point FromXYPlane(this Plane plane, Point point)
        {
            var matrix = plane.ToXYPlaneProjection();
            matrix.Invert();
            return point.Transform(matrix);
        }
    }
}