﻿using System;
using System.IO;
using System.Linq;
using BCad.Dxf;
using BCad.Dxf.Entities;
using Xunit;

namespace BCad.Test.DxfTests
{
    public class DxfEntityTests : AbstractDxfTests
    {
        #region Private helpers

        private static DxfEntity Entity(string entityType, string data)
        {
            var file = Section("ENTITIES", string.Format(@"
999
ill-placed comment
  0
{0}
  5
<handle>
  6
<linetype-name>
  8
<layer>
 48
3.14159
 60
1
 62
1
 67
1
{1}
", entityType, data.Trim()));
            var entity = file.EntitiesSection.Entities.Single();
            Assert.Equal("<handle>", entity.Handle);
            Assert.Equal("<linetype-name>", entity.LinetypeName);
            Assert.Equal("<layer>", entity.Layer);
            Assert.Equal(3.14159, entity.LinetypeScale);
            Assert.False(entity.IsVisible);
            Assert.True(entity.IsInPaperSpace);
            Assert.Equal(DxfColor.FromIndex(1), entity.Color);
            return entity;
        }

        private static DxfEntity EmptyEntity(string entityType)
        {
            var file = Section("ENTITIES", string.Format(@"
  0
{0}", entityType));
            var entity = file.EntitiesSection.Entities.Single();
            Assert.Null(entity.Handle);
            Assert.Null(entity.Layer);
            Assert.Null(entity.LinetypeName);
            Assert.Equal(1.0, entity.LinetypeScale);
            Assert.True(entity.IsVisible);
            Assert.False(entity.IsInPaperSpace);
            Assert.Equal(DxfColor.ByBlock, entity.Color);
            return entity;
        }

        private static void EnsureFileContainsEntity(DxfEntity entity, string text)
        {
            var file = new DxfFile();
            file.EntitiesSection.Entities.Add(entity);
            var stream = new MemoryStream();
            file.Save(stream);
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var actual = new StreamReader(stream).ReadToEnd();
            Assert.Contains(text.Trim(), actual);
        }

        #endregion

        #region Read default value tests

        [Fact]
        public void ReadDefaultLineTest()
        {
            var line = (DxfLine)EmptyEntity("LINE");
            Assert.Equal(0.0, line.P1.X);
            Assert.Equal(0.0, line.P1.Y);
            Assert.Equal(0.0, line.P1.Z);
            Assert.Equal(0.0, line.P2.X);
            Assert.Equal(0.0, line.P2.Y);
            Assert.Equal(0.0, line.P2.Z);
            Assert.Equal(0.0, line.Thickness);
            Assert.Equal(0.0, line.ExtrusionDirection.X);
            Assert.Equal(0.0, line.ExtrusionDirection.Y);
            Assert.Equal(1.0, line.ExtrusionDirection.Z);
        }

        [Fact]
        public void ReadDefaultCircleTest()
        {
            var circle = (DxfCircle)EmptyEntity("CIRCLE");
            Assert.Equal(0.0, circle.Center.X);
            Assert.Equal(0.0, circle.Center.Y);
            Assert.Equal(0.0, circle.Center.Z);
            Assert.Equal(0.0, circle.Radius);
            Assert.Equal(0.0, circle.Normal.X);
            Assert.Equal(0.0, circle.Normal.Y);
            Assert.Equal(1.0, circle.Normal.Z);
            Assert.Equal(0.0, circle.Thickness);
        }

        [Fact]
        public void ReadDefaultArcTest()
        {
            var arc = (DxfArc)EmptyEntity("ARC");
            Assert.Equal(0.0, arc.Center.X);
            Assert.Equal(0.0, arc.Center.Y);
            Assert.Equal(0.0, arc.Center.Z);
            Assert.Equal(0.0, arc.Radius);
            Assert.Equal(0.0, arc.Normal.X);
            Assert.Equal(0.0, arc.Normal.Y);
            Assert.Equal(1.0, arc.Normal.Z);
            Assert.Equal(0.0, arc.StartAngle);
            Assert.Equal(360.0, arc.EndAngle);
            Assert.Equal(0.0, arc.Thickness);
        }

        [Fact]
        public void ReadDefaultEllipseTest()
        {
            var el = (DxfEllipse)EmptyEntity("ELLIPSE");
            Assert.Equal(0.0, el.Center.X);
            Assert.Equal(0.0, el.Center.Y);
            Assert.Equal(0.0, el.Center.Z);
            Assert.Equal(0.0, el.MajorAxis.X);
            Assert.Equal(0.0, el.MajorAxis.Y);
            Assert.Equal(0.0, el.MajorAxis.Z);
            Assert.Equal(0.0, el.Normal.X);
            Assert.Equal(0.0, el.Normal.Y);
            Assert.Equal(1.0, el.Normal.Z);
            Assert.Equal(1.0, el.MinorAxisRatio);
            Assert.Equal(0.0, el.StartParameter);
            Assert.Equal(360.0, el.EndParameter);
        }

        [Fact]
        public void ReadDefaultTextTest()
        {
            var text = (DxfText)EmptyEntity("TEXT");
            Assert.Equal(0.0, text.Location.X);
            Assert.Equal(0.0, text.Location.Y);
            Assert.Equal(0.0, text.Location.Z);
            Assert.Equal(0.0, text.Normal.X);
            Assert.Equal(0.0, text.Normal.Y);
            Assert.Equal(1.0, text.Normal.Z);
            Assert.Equal(0.0, text.Rotation);
            Assert.Equal(1.0, text.TextHeight);
            Assert.Null(text.Value);
            Assert.Null(text.TextStyleName);
            Assert.Equal(0.0, text.Thickness);
            Assert.Equal(1.0, text.RelativeXScaleFactor);
            Assert.Equal(0.0, text.ObliqueAngle);
            Assert.False(text.IsTextBackward);
            Assert.False(text.IsTextUpsideDown);
            Assert.Equal(0.0, text.SecondAlignmentPoint.X);
            Assert.Equal(0.0, text.SecondAlignmentPoint.Y);
            Assert.Equal(0.0, text.SecondAlignmentPoint.Z);
            Assert.Equal(HorizontalTextJustification.Left, text.HorizontalTextJustification);
            Assert.Equal(VerticalTextJustification.Baseline, text.VerticalTextJustification);
        }

        [Fact]
        public void ReadDefaultVertexTest()
        {
            var vertex = (DxfVertex)EmptyEntity("VERTEX");
            Assert.Equal(0.0, vertex.Location.X);
            Assert.Equal(0.0, vertex.Location.Y);
            Assert.Equal(0.0, vertex.Location.Z);
            Assert.Equal(0.0, vertex.StartingWidth);
            Assert.Equal(0.0, vertex.EndingWidth);
            Assert.Equal(0.0, vertex.Bulge);
            Assert.False(vertex.IsExtraCreatedByCurveFit);
            Assert.False(vertex.IsCurveFitTangentDefined);
            Assert.False(vertex.IsSplineVertexCreatedBySplineFitting);
            Assert.False(vertex.IsSplineFrameControlPoint);
            Assert.False(vertex.Is3DPolylineVertex);
            Assert.False(vertex.Is3DPolygonMesh);
            Assert.False(vertex.IsPolyfaceMeshVertex);
            Assert.Equal(0.0, vertex.CurveFitTangentDirection);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex1);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex2);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex3);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex4);
        }

        [Fact]
        public void ReadDefaultSeqendTest()
        {
            var seqend = (DxfSeqend)EmptyEntity("SEQEND");
            // nothing to verify
        }

        [Fact]
        public void ReadDefaultPolylineTest()
        {
            var poly = (DxfPolyline)EmptyEntity("POLYLINE");
            Assert.Equal(0.0, poly.Elevation);
            Assert.Equal(0.0, poly.Normal.X);
            Assert.Equal(0.0, poly.Normal.Y);
            Assert.Equal(1.0, poly.Normal.Z);
            Assert.Equal(0.0, poly.Thickness);
            Assert.Equal(0.0, poly.DefaultStartingWidth);
            Assert.Equal(0.0, poly.DefaultEndingWidth);
            Assert.Equal(0, poly.PolygonMeshMVertexCount);
            Assert.Equal(0, poly.PolygonMeshNVertexCount);
            Assert.Equal(0, poly.SmoothSurfaceMDensity);
            Assert.Equal(0, poly.SmoothSurfaceNDensity);
            Assert.Equal(CurvedAndSmoothSurfaceType.None, poly.SurfaceType);
            Assert.False(poly.IsClosed);
            Assert.False(poly.ContainsCurveFitVerticies);
            Assert.False(poly.ContainsSplineFitVerticies);
            Assert.False(poly.Is3DPolyline);
            Assert.False(poly.Is3DPolygonMesh);
            Assert.False(poly.Is3DMeshClosedInNDirection);
            Assert.False(poly.IsPolyfaceMesh);
            Assert.False(poly.IsContinuousLinetipePattern);
        }

        #endregion

        #region Read specific value tests

        [Fact]
        public void ReadLineTest()
        {
            var line = (DxfLine)Entity("LINE", @"
 10
1.100000E+001
 20
2.200000E+001
 30
3.300000E+001
 11
4.400000E+001
 21
5.500000E+001
 31
6.600000E+001
 39
7.700000E+001
210
8.800000E+001
220
9.900000E+001
230
1.500000E+002
");
            Assert.Equal(11.0, line.P1.X);
            Assert.Equal(22.0, line.P1.Y);
            Assert.Equal(33.0, line.P1.Z);
            Assert.Equal(44.0, line.P2.X);
            Assert.Equal(55.0, line.P2.Y);
            Assert.Equal(66.0, line.P2.Z);
            Assert.Equal(77.0, line.Thickness);
            Assert.Equal(88.0, line.ExtrusionDirection.X);
            Assert.Equal(99.0, line.ExtrusionDirection.Y);
            Assert.Equal(150.0, line.ExtrusionDirection.Z);
        }

        [Fact]
        public void ReadCircleTest()
        {
            var circle = (DxfCircle)Entity("CIRCLE", @"
 10
1.100000E+001
 20
2.200000E+001
 30
3.300000E+001
 40
4.400000E+001
 39
3.500000E+001
210
5.500000E+001
220
6.600000E+001
230
7.700000E+001
");
            Assert.Equal(11.0, circle.Center.X);
            Assert.Equal(22.0, circle.Center.Y);
            Assert.Equal(33.0, circle.Center.Z);
            Assert.Equal(44.0, circle.Radius);
            Assert.Equal(55.0, circle.Normal.X);
            Assert.Equal(66.0, circle.Normal.Y);
            Assert.Equal(77.0, circle.Normal.Z);
            Assert.Equal(35.0, circle.Thickness);
        }

        [Fact]
        public void ReadArcTest()
        {
            var arc = (DxfArc)Entity("ARC", @"
 10
1.100000E+001
 20
2.200000E+001
 30
3.300000E+001
 40
4.400000E+001
210
5.500000E+001
220
6.600000E+001
230
7.700000E+001
 50
8.800000E+001
 51
9.900000E+001
 39
3.500000E+001
");
            Assert.Equal(11.0, arc.Center.X);
            Assert.Equal(22.0, arc.Center.Y);
            Assert.Equal(33.0, arc.Center.Z);
            Assert.Equal(44.0, arc.Radius);
            Assert.Equal(55.0, arc.Normal.X);
            Assert.Equal(66.0, arc.Normal.Y);
            Assert.Equal(77.0, arc.Normal.Z);
            Assert.Equal(88.0, arc.StartAngle);
            Assert.Equal(99.0, arc.EndAngle);
            Assert.Equal(35.0, arc.Thickness);
        }

        [Fact]
        public void ReadEllipseTest()
        {
            var el = (DxfEllipse)Entity("ELLIPSE", @"
 10
1.100000E+001
 20
2.200000E+001
 30
3.300000E+001
 11
4.400000E+001
 21
5.500000E+001
 31
6.600000E+001
210
7.700000E+001
220
8.800000E+001
230
9.900000E+001
 40
1.200000E+001
 41
0.100000E+000
 42
0.400000E+000
");
            Assert.Equal(11.0, el.Center.X);
            Assert.Equal(22.0, el.Center.Y);
            Assert.Equal(33.0, el.Center.Z);
            Assert.Equal(44.0, el.MajorAxis.X);
            Assert.Equal(55.0, el.MajorAxis.Y);
            Assert.Equal(66.0, el.MajorAxis.Z);
            Assert.Equal(77.0, el.Normal.X);
            Assert.Equal(88.0, el.Normal.Y);
            Assert.Equal(99.0, el.Normal.Z);
            Assert.Equal(12.0, el.MinorAxisRatio);
            Assert.Equal(0.1, el.StartParameter);
            Assert.Equal(0.4, el.EndParameter);
        }

        [Fact]
        public void ReadTextTest()
        {
            var text = (DxfText)Entity("TEXT", @"
  1
foo bar
  7
text style name
 10
1.100000E+001
 20
2.200000E+001
 30
3.300000E+001
 39
3.900000E+001
 40
4.400000E+001
 41
4.100000E+001
 50
5.500000E+001
 51
5.100000E+001
 71
255
 72
3
 73
1
 11
9.100000E+001
 21
9.200000E+001
 31
9.300000E+001
 210
6.600000E+001
 220
7.700000E+001
 230
8.800000E+001
");
            Assert.Equal("foo bar", text.Value);
            Assert.Equal("text style name", text.TextStyleName);
            Assert.Equal(11.0, text.Location.X);
            Assert.Equal(22.0, text.Location.Y);
            Assert.Equal(33.0, text.Location.Z);
            Assert.Equal(39.0, text.Thickness);
            Assert.Equal(41.0, text.RelativeXScaleFactor);
            Assert.Equal(44.0, text.TextHeight);
            Assert.Equal(51.0, text.ObliqueAngle);
            Assert.True(text.IsTextBackward);
            Assert.True(text.IsTextUpsideDown);
            Assert.Equal(HorizontalTextJustification.Aligned, text.HorizontalTextJustification);
            Assert.Equal(VerticalTextJustification.Bottom, text.VerticalTextJustification);
            Assert.Equal(91.0, text.SecondAlignmentPoint.X);
            Assert.Equal(92.0, text.SecondAlignmentPoint.Y);
            Assert.Equal(93.0, text.SecondAlignmentPoint.Z);
            Assert.Equal(55.0, text.Rotation);
            Assert.Equal(66.0, text.Normal.X);
            Assert.Equal(77.0, text.Normal.Y);
            Assert.Equal(88.0, text.Normal.Z);
        }

        [Fact]
        public void ReadVertexTest()
        {
            var vertex = (DxfVertex)Entity("VERTEX", @"
 10
1.100000E+001
 20
2.200000E+001
 30
3.300000E+001
 40
4.000000E+001
 41
4.100000E+001
 42
4.200000E+001
 50
5.000000E+001
 70
255
 71
71
 72
72
 73
73
 74
74
");
            Assert.Equal(11.0, vertex.Location.X);
            Assert.Equal(22.0, vertex.Location.Y);
            Assert.Equal(33.0, vertex.Location.Z);
            Assert.Equal(40.0, vertex.StartingWidth);
            Assert.Equal(41.0, vertex.EndingWidth);
            Assert.Equal(42.0, vertex.Bulge);
            Assert.True(vertex.IsExtraCreatedByCurveFit);
            Assert.True(vertex.IsCurveFitTangentDefined);
            Assert.True(vertex.IsSplineVertexCreatedBySplineFitting);
            Assert.True(vertex.IsSplineFrameControlPoint);
            Assert.True(vertex.Is3DPolylineVertex);
            Assert.True(vertex.Is3DPolygonMesh);
            Assert.True(vertex.IsPolyfaceMeshVertex);
            Assert.Equal(50.0, vertex.CurveFitTangentDirection);
            Assert.Equal(71, vertex.PolyfaceMeshVertexIndex1);
            Assert.Equal(72, vertex.PolyfaceMeshVertexIndex2);
            Assert.Equal(73, vertex.PolyfaceMeshVertexIndex3);
            Assert.Equal(74, vertex.PolyfaceMeshVertexIndex4);
        }

        [Fact]
        public void ReadSeqendTest()
        {
            var seqend = (DxfSeqend)Entity("SEQEND", "");
            // nothing to verify
        }

        [Fact]
        public void ReadPolylineTest()
        {
            var poly = (DxfPolyline)Entity("POLYLINE", @"
 30
1.100000E+001
 39
1.800000E+001
 40
4.000000E+001
 41
4.100000E+001
 70
255
 71
71
 72
72
 73
73
 74
74
 75
6
210
2.200000E+001
220
3.300000E+001
230
4.400000E+001
  0
VERTEX
 10
1.200000E+001
 20
2.300000E+001
 30
3.400000E+001
  0
VERTEX
 10
4.500000E+001
 20
5.600000E+001
 30
6.700000E+001
  0
SEQEND
");
            Assert.Equal(11.0, poly.Elevation);
            Assert.Equal(18.0, poly.Thickness);
            Assert.Equal(40.0, poly.DefaultStartingWidth);
            Assert.Equal(41.0, poly.DefaultEndingWidth);
            Assert.Equal(71, poly.PolygonMeshMVertexCount);
            Assert.Equal(72, poly.PolygonMeshNVertexCount);
            Assert.Equal(73, poly.SmoothSurfaceMDensity);
            Assert.Equal(74, poly.SmoothSurfaceNDensity);
            Assert.Equal(CurvedAndSmoothSurfaceType.CubicBSpline, poly.SurfaceType);
            Assert.True(poly.IsClosed);
            Assert.True(poly.ContainsCurveFitVerticies);
            Assert.True(poly.ContainsSplineFitVerticies);
            Assert.True(poly.Is3DPolyline);
            Assert.True(poly.Is3DPolygonMesh);
            Assert.True(poly.Is3DMeshClosedInNDirection);
            Assert.True(poly.IsPolyfaceMesh);
            Assert.True(poly.IsContinuousLinetipePattern);
            Assert.Equal(22.0, poly.Normal.X);
            Assert.Equal(33.0, poly.Normal.Y);
            Assert.Equal(44.0, poly.Normal.Z);
            Assert.Equal(2, poly.Vertices.Count);
            Assert.Equal(12.0, poly.Vertices[0].Location.X);
            Assert.Equal(23.0, poly.Vertices[0].Location.Y);
            Assert.Equal(34.0, poly.Vertices[0].Location.Z);
            Assert.Equal(45.0, poly.Vertices[1].Location.X);
            Assert.Equal(56.0, poly.Vertices[1].Location.Y);
            Assert.Equal(67.0, poly.Vertices[1].Location.Z);
        }

        #endregion

        #region Write default value tests

        [Fact]
        public void WriteDefaultLineTest()
        {
            EnsureFileContainsEntity(new DxfLine(), @"
  0
LINE
 62
0
100
AcDbLine
 10
0.0000000000000000E+000
 20
0.0000000000000000E+000
 30
0.0000000000000000E+000
 11
0.0000000000000000E+000
 21
0.0000000000000000E+000
 31
0.0000000000000000E+000
  0
");
        }

        [Fact]
        public void WriteDefaultCircleTest()
        {
            EnsureFileContainsEntity(new DxfCircle(), @"
  0
CIRCLE
 62
0
100
AcDbCircle
 10
0.0000000000000000E+000
 20
0.0000000000000000E+000
 30
0.0000000000000000E+000
 40
0.0000000000000000E+000
  0
");
        }

        [Fact]
        public void WriteDefaultArcTest()
        {
            EnsureFileContainsEntity(new DxfArc(), @"
  0
ARC
 62
0
100
AcDbCircle
 10
0.0000000000000000E+000
 20
0.0000000000000000E+000
 30
0.0000000000000000E+000
 40
0.0000000000000000E+000
 50
0.0000000000000000E+000
 51
3.6000000000000000E+002
  0
");
        }

        [Fact]
        public void WriteDefaultEllipseTest()
        {
            EnsureFileContainsEntity(new DxfEllipse(), @"
  0
ELLIPSE
 62
0
100
AcDbEllipse
 10
0.0000000000000000E+000
 20
0.0000000000000000E+000
 30
0.0000000000000000E+000
 11
0.0000000000000000E+000
 21
0.0000000000000000E+000
 31
0.0000000000000000E+000
 40
1.0000000000000000E+000
 41
0.0000000000000000E+000
 42
3.6000000000000000E+002
  0
");
        }

        [Fact]
        public void WriteDefaultTextTest()
        {
            EnsureFileContainsEntity(new DxfText(), @"
  0
TEXT
 62
0
100
AcDbText
  1

 10
0.0000000000000000E+000
 20
0.0000000000000000E+000
 30
0.0000000000000000E+000
 40
1.0000000000000000E+000
  0
");
        }

        [Fact]
        public void WriteDefaultPolylineTest()
        {
            EnsureFileContainsEntity(new DxfPolyline(), @"
  0
POLYLINE
 62
0
100
AcDb2dPolyline
 10
0.0000000000000000E+000
 20
0.0000000000000000E+000
 30
0.0000000000000000E+000
  0
SEQEND
  0
");
        }

        #endregion

        #region Write specific value tests TODO

        [Fact]
        public void WriteLineTest()
        {
            EnsureFileContainsEntity(new DxfLine(new DxfPoint(1, 2, 3), new DxfPoint(4, 5, 6))
                {
                    Color = DxfColor.FromIndex(7),
                    Handle = "foo",
                    Layer = "bar",
                    Thickness = 7,
                    ExtrusionDirection = new DxfVector(8, 9, 10)
                }, @"
  0
LINE
  5
foo
  8
bar
 62
7
100
AcDbLine
 10
1.0000000000000000E+000
 20
2.0000000000000000E+000
 30
3.0000000000000000E+000
 11
4.0000000000000000E+000
 21
5.0000000000000000E+000
 31
6.0000000000000000E+000
 39
7.0000000000000000E+000
210
8.0000000000000000E+000
220
9.0000000000000000E+000
230
1.0000000000000000E+001
  0
");
        }

        #endregion
    }
}
