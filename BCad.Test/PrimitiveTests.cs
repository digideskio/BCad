﻿using System;
using System.Linq;
using BCad.Extensions;
using BCad.Primitives;
using Xunit;

namespace BCad.Test
{
    public class PrimitiveTests
    {
        private static void Test(IPrimitive first, IPrimitive second, bool withinBounds, params Point[] points)
        {
            var p = first.IntersectionPoints(second, withinBounds).OrderBy(x => x.X).ThenBy(y => y.Y).ThenBy(z => z.Z).ToArray();
            points = points.OrderBy(x => x.X).ThenBy(y => y.Y).ThenBy(z => z.Z).ToArray();
            Assert.Equal(points.Length, p.Length);
            for (int i = 0; i < p.Length; i++)
            {
                Assert.Equal(points[i], p[i]);
            }
        }

        private static PrimitiveLine Line(Point p1, Point p2)
        {
            return new PrimitiveLine(p1, p2, Color.Auto);
        }

        private static PrimitiveEllipse Circle(Point center, double radius)
        {
            return new PrimitiveEllipse(center, radius, Vector.ZAxis, Color.Auto);
        }

        private static PrimitiveEllipse Arc(Point center, double radius, double startAngle, double endAngle)
        {
            return new PrimitiveEllipse(center, radius, startAngle, endAngle, Vector.ZAxis, Color.Auto);
        }

        private static PrimitiveEllipse Ellipse(Point center, double radiusX, double radiusY)
        {
            return new PrimitiveEllipse(center, new Vector(radiusX, 0, 0), Vector.ZAxis, radiusY / radiusX, 0, 360, Color.Auto);
        }

        [Fact]
        public void LineIntersectionTest()
        {
            Test(
                Line(new Point(-1, 0, 0), new Point(1, 0, 0)),
                Line(new Point(0, -1, 0), new Point(0, 1, 0)),
                true,
                new Point(0, 0, 0));
        }

        [Fact]
        public void LinePointDistanceTest()
        {
            var l = Line(new Point(0, 0, 0), new Point(2, 0, 0));
            var p = new Point(1, 1, 0);
            Assert.Equal(new Point(1, 0, 0), l.ClosestPoint(p));
        }

        [Fact]
        public void LinePointDistanceTest2()
        {
            var a = Line(new Point(0, 0, 0), new Point(10, 0, 0));
            var b = new Point(5, 3, 0);
            var c = a.ClosestPoint(b);
            Assert.Equal(new Point(5, 0, 0), c);
        }

        [Fact]
        public void ThreePointCircleTest()
        {
            var a = new Point(0, 0, 0);
            var b = new Point(0, 2, 0);
            var c = new Point(1, 1, 0);
            var circ = PrimitiveEllipse.ThreePointCircle(a, b, c);
            Assert.NotNull(circ);
            Assert.Equal(new Point(0, 1, 0), circ.Center);
            Assert.Equal(Vector.XAxis, circ.MajorAxis);
            Assert.Equal(Vector.ZAxis, circ.Normal);
        }

        [Fact]
        public void LineCircleIntersectionTest1()
        {
            Test(
                Circle(Point.Origin, 2),
                Line(new Point(2, 0, -2), new Point(2, 0, 2)),
                true,
                new Point(2, 0, 0));
        }

        [Fact]
        public void LineCircleIntersectionTest2()
        {
            Test(
                Circle(new Point(1, 0, 0), 1.0),
                Line(new Point(-4, 0, 0), new Point(4, 0, 0)),
                true,
                new Point(2, 0, 0),
                new Point(0, 0, 0));
        }

        [Fact]
        public void LineCircleIntersectionTest3()
        {
            Test(
                Circle(new Point(1, 1, 0), 2),
                Line(new Point(-3, 1, 0), new Point(3, 1, 0)),
                true,
                new Point(-1, 1, 0),
                new Point(3, 1, 0));
        }

        [Fact]
        public void LineCircleIntersectionTest4()
        {
            Test(
                Circle(new Point(1, 1, 0), 2),
                Line(new Point(2, 1, 0), new Point(4, 1, 0)),
                true,
                new Point(3, 1, 0));
        }

        [Fact]
        public void LineCircleIntersectionTestOffPlane()
        {
            Test(
                Circle(Point.Origin, 1),
                Line(new Point(1, 0, 1), new Point(1, 0, -1)),
                true,
                new Point(1, 0, 0));
        }

        [Fact]
        public void LineCircleIntersectionTestOffPlaneOutsideAngle()
        {
            Test(
                Arc(Point.Origin, 1, 90, 270),
                Line(new Point(1, 0, 1), new Point(1, 0, -1)),
                true);
        }

        [Fact]
        public void CircleCircleIntersectionTestSamePlaneOnePoint()
        {
            Test(
                Circle(new Point(1, 1, 0), 2),
                Circle(new Point(4, 1, 0), 1),
                true,
                new Point(3, 1, 0));
        }

        [Fact]
        public void CircleCircleIntersectionTestSamePlaneTwoPoints()
        {
            var x = Math.Sqrt(3.0) / 2.0;
            Test(
                Circle(Point.Origin, 1),
                Circle(new Point(1, 0, 0), 1),
                true,
                new Point(0.5, x, 0),
                new Point(0.5, -x, 0));
        }

        [Fact]
        public void CircleCircleIntersectionTestSamePlaneNoPoints()
        {
            var x = Math.Sqrt(3.0) / 2.0;
            Test(
                Circle(Point.Origin, 1),
                Circle(new Point(3, 0, 0), 1),
                true);
        }

        [Fact]
        public void CircleEllipseIntersectionTestSamePlaneOnePoint()
        {
            // x-axis alignment horizontal
            Test(
                Circle(new Point(1, 0, 0), 1),
                Ellipse(new Point(4, 0, 0), 2, 1),
                true,
                new Point(2, 0, 0));
            // x-axis alignment vertical
            Test(
                Circle(new Point(1, 0, 0), 1),
                Ellipse(new Point(3, 0, 0), 1, 2),
                true,
                new Point(2, 0, 0));
            // y-axis alignment horizontal
            Test(
                Circle(Point.Origin, 1),
                Ellipse(new Point(0, 2, 0), 2, 1),
                true,
                new Point(0, 1, 0));
            // y-axis alignment vertical
            Test(
                Circle(Point.Origin, 1),
                Ellipse(new Point(0, 3, 0), 1, 2),
                true,
                new Point(0, 1, 0));
            // rotates to x-axis alignment
            Test(
                Circle(Point.Origin, 1),
                new PrimitiveEllipse(new Point(-Math.Sqrt(2), Math.Sqrt(2), 0), new Vector(Math.Sqrt(2), Math.Sqrt(2), 0), Vector.ZAxis, 0.5, 0, 360, Color.Auto),
                true,
                new Point(-0.707106781187, 0.707106781187, 0));
        }

        [Fact]
        public void CircleEllipseIntersectionTestSamePlaneTwoPoints()
        {
            Test(
                Circle(new Point(1, 0, 0), 1),
                Ellipse(new Point(3, 0, 0), 2, 1),
                true,
                new Point(1, 0, 0),
                new Point(-1, 0, 0));
        }
    }
}