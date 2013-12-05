﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BCad.Entities;
using BCad.EventArguments;
using BCad.Extensions;
using BCad.Helpers;
using BCad.Primitives;
using BCad.Services;
using SlimDX;
using SlimDX.Direct3D9;

namespace BCad.UI
{
    #region IDisplayPrimitive

    internal interface IDisplayPrimitive : IDisposable
    {
        void RenderNormal(Device device, Matrix projection, Matrix view);
        void RenderSelected(Device device, Matrix projection, Matrix view);
    }

    internal class DisplayPrimitiveLines : IDisplayPrimitive
    {
        public Color4 Color { get; private set; }
        public Vector3[] LineVerticies { get; private set; }
        private SlimDX.Direct3D9.Line solidLine = null;
        private SlimDX.Direct3D9.Line dashedLine = null;

        public DisplayPrimitiveLines(Color4 color, Vector3[] lineVerticies, SlimDX.Direct3D9.Line solidLine, SlimDX.Direct3D9.Line dashedLine)
        {
            Debug.Assert(lineVerticies.Length > 1);

            this.Color = color;
            this.LineVerticies = lineVerticies;
            this.solidLine = solidLine;
            this.dashedLine = dashedLine;
        }

        public void RenderNormal(Device device, Matrix projection, Matrix view)
        {
            // TODO: draw via user primitives
            solidLine.DrawTransformed(LineVerticies, projection * view, Color);
        }

        public void RenderSelected(Device device, Matrix projection, Matrix view)
        {
            dashedLine.DrawTransformed(LineVerticies, projection * view, Color);
        }

        public void Dispose()
        {
        }
    }

    internal class DisplayPrimitiveMesh : IDisplayPrimitive
    {
        public Material Material { get; private set; }
        public Mesh Mesh { get; private set; }
        public Matrix WorldMatrix { get; private set; }
        private Vector3[] boundingCorners = null;
        private Vector3[] outlineCorners = null;
        private PixelShader NormalShader = null;
        private PixelShader SelectedShader = null;

        public DisplayPrimitiveMesh(Mesh mesh, Color4 color, Matrix worldMatrix, PixelShader normalShader, PixelShader selectedShader)
        {
            this.Mesh = mesh;
            this.Material = new Material()
            {
                Diffuse = color,
                Emissive = color
            };
            this.WorldMatrix = worldMatrix;
            this.NormalShader = normalShader;
            this.SelectedShader = selectedShader;

            var boundingBox = Mesh.GetBoundingBox();
            this.boundingCorners = boundingBox.GetCorners();
            for (int i = 0; i < boundingCorners.Length; i++)
            {
                this.boundingCorners[i] = Vector3.Transform(this.boundingCorners[i], worldMatrix).ToVector3();
            }

            this.outlineCorners = this.boundingCorners.Concat(new[] { this.boundingCorners[0] }).ToArray();
        }

        public void RenderNormal(Device device, Matrix projection, Matrix view)
        {
            Render(device, NormalShader);
        }

        public void RenderSelected(Device device, Matrix projection, Matrix view)
        {
            Render(device, SelectedShader);
        }

        private void Render(Device device, PixelShader shader)
        {
            device.SetTransform(TransformState.World, this.WorldMatrix);
            device.Material = Material;
            device.PixelShader = shader;
            Mesh.DrawSubset(0);
        }

        public void Dispose()
        {
            this.Mesh.Dispose();
        }
    }

    #endregion

    #region TransformedEntity class

    internal class TransformedEntity
    {
        public Entity Entity { get; private set; }
        public IDisplayPrimitive[] DisplayPrimitives { get; private set; }

        public TransformedEntity(Entity entity, IEnumerable<IDisplayPrimitive> displayPrimitives)
        {
            this.Entity = entity;
            this.DisplayPrimitives = displayPrimitives.ToArray();
        }
    }

    #endregion

    internal class SlimDXRenderEngine : ISlimDXRenderEngine
    {
        private IViewHost viewHost;
        private IWorkspace workspace;
        private IInputService inputService;
        private Drawing drawing;
        private Device Device { get { return control.Device; } }
        private SlimDX.Direct3D9.Line solidLine;
        private SlimDX.Direct3D9.Line dashedLine;
        private ShaderBytecode selectedMeshBytecode;
        private PixelShader normalPixelShader;
        private PixelShader selectedPixelShader;
        private Matrix projectionMatrix = Matrix.Identity;
        private Matrix projectionWorldMatrix = Matrix.Identity;
        private Matrix projectionViewWorldMatrix = Matrix.Identity;
        private Matrix worldMatrix = Matrix.Identity;
        private Matrix viewMatrix = Matrix.Scaling(1.0f, 1.0f, 0.0f);
        private object drawingGate = new object();
        private Dictionary<uint, TransformedEntity> lines = new Dictionary<uint, TransformedEntity>();
        private IDisplayPrimitive[] rubberBandLines = null;
        private bool selecting = false;
        private System.Windows.Point firstSelectionPoint = new System.Windows.Point();
        private System.Windows.Point currentSelectionPoint = new System.Windows.Point();
        private Color4 autoColor = new Color4();
        private SlimDXControl control;
        private bool lastGeneratorNonNull;

        private const int FullCircleDrawingSegments = 180;
        private const int LowQualityCircleDrawingSegments = 72;

        public SlimDXRenderEngine(SlimDXControl control, IViewHost viewHost, IWorkspace workspace, IInputService inputService)
        {
            this.control = control;
            this.viewHost = viewHost;
            this.workspace = workspace;

            this.workspace = workspace;
            this.inputService = inputService;

            this.workspace.WorkspaceChanged += WorkspaceChanged;
            this.workspace.SettingsManager.PropertyChanged += SettingsManagerPropertyChanged;
            this.workspace.CommandExecuted += CommandExecuted;
            this.workspace.SelectedEntities.CollectionChanged += SelectedEntitiesCollectionChanged;
            this.inputService.ValueRequested += InputServiceValueRequested;
            this.inputService.ValueReceived += InputServiceValueReceived;

            // load the workspace
            WorkspaceChanged(this.workspace, WorkspaceChangeEventArgs.Reset());

            // load settings
            foreach (var setting in new[] { Constants.BackgroundColorString })
                SettingsManagerPropertyChanged(this.workspace.SettingsManager, new PropertyChangedEventArgs(setting));

            control.SetRenderEngine(this);
        }

        public void OnDeviceCreated(object sender, EventArgs e)
        {
        }

        public void OnDeviceDestroyed(object sender, EventArgs e)
        {
        }

        public void OnDeviceLost(object sender, EventArgs e)
        {
        }

        public void OnDeviceReset(object sender, EventArgs e)
        {
            if (solidLine != null)
                solidLine.Dispose();
            if (dashedLine != null)
                dashedLine.Dispose();
            if (selectedMeshBytecode != null)
                selectedMeshBytecode.Dispose();
            if (selectedPixelShader != null)
                selectedPixelShader.Dispose();

            solidLine = new SlimDX.Direct3D9.Line(Device);
            dashedLine = new SlimDX.Direct3D9.Line(Device)
            {
                Width = 1.0f,
                Pattern = 0xF0F0F0F,
                PatternScale = 1
            };

            // prepare shader bytecode
            selectedMeshBytecode = ShaderBytecode.Compile(@"
struct Result
{
    float4 Color : COLOR0;
};

struct Input
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
};

Result PShader(Input pixel)
{
    Result res = (Result)0;
    int p = pixel.Position.x + pixel.Position.y;
    if (p % 2 == 0)
        res.Color = pixel.Color;
    else
        res.Color = float4(0.0f, 0.0f, 0.0f, 0.0f);

    return res;
}
", "PShader", "ps_3_0", ShaderFlags.None);
            normalPixelShader = Device.PixelShader;
            selectedPixelShader = new PixelShader(Device, selectedMeshBytecode);

            Device.SetRenderState(RenderState.Lighting, true);
            Device.SetRenderState(RenderState.AlphaBlendEnable, true);
            Device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            ViewPortChanged();
            DrawingChanged(drawing);
        }

        public void OnMainLoop(object sender, EventArgs args)
        {
            lock (drawingGate)
            {
                var start = DateTime.UtcNow;

                Device.SetTransform(TransformState.Projection, projectionMatrix);
                Device.SetTransform(TransformState.View, viewMatrix);

                var selected = workspace.SelectedEntities;
                foreach (var entityId in lines.Keys)
                {
                    var ent = lines[entityId];
                    var prims = ent.DisplayPrimitives;
                    var len = prims.Length;
                    for (int i = 0; i < len; i++)
                    {
                        if (selected.ContainsHash(entityId.GetHashCode()))
                        {
                            prims[i].RenderSelected(Device, projectionMatrix, viewMatrix);
                        }
                        else
                        {
                            prims[i].RenderNormal(Device, projectionMatrix, viewMatrix);
                        }
                    }
                }

                GenerateRubberBandLines(viewHost.GetCursorPoint());
                if (rubberBandLines != null)
                {
                    for (int i = 0; i < rubberBandLines.Length; i++)
                    {
                        rubberBandLines[i].RenderNormal(Device, projectionMatrix, viewMatrix);
                    }
                }

                if (selecting)
                {
                    var line = currentSelectionPoint.X < firstSelectionPoint.X
                        ? dashedLine
                        : solidLine;
                    var a = new Vector2((float)currentSelectionPoint.X, (float)currentSelectionPoint.Y);
                    var b = new Vector2((float)currentSelectionPoint.X, (float)firstSelectionPoint.Y);
                    var c = new Vector2((float)firstSelectionPoint.X, (float)firstSelectionPoint.Y);
                    var d = new Vector2((float)firstSelectionPoint.X, (float)currentSelectionPoint.Y);
                    var e = new Vector2((float)currentSelectionPoint.X, (float)currentSelectionPoint.Y);
                    line.Draw(new[] { a, b }, autoColor);
                    line.Draw(new[] { b, c }, autoColor);
                    line.Draw(new[] { c, d }, autoColor);
                    line.Draw(new[] { d, e }, autoColor);
                    line.Draw(new[] { e, a }, autoColor);
                }
            }
        }

        #region PropertyChanged functions

        private void SettingsManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case Constants.BackgroundColorString:
                    var bg = workspace.SettingsManager.BackgroundColor;
                    control.ClearColor = bg.ToMediaColor();
                    var backgroundColor = (bg.R << 16) | (bg.G << 8) | bg.B;
                    var brightness = System.Drawing.Color.FromArgb(backgroundColor).GetBrightness();
                    var color = brightness < 0.67 ? 0xFFFFFF : 0x000000;
                    autoColor = new Color4((0xFF << 24) | color);
                    ForceRender();
                    break;
                default:
                    break;
            }
        }

        private void WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.IsDrawingChange)
                DrawingChanged(workspace.Drawing);
            if (e.IsActiveViewPortChange)
                ViewPortChanged();
        }

        private void DrawingChanged(Drawing drawing)
        {
            lock (drawingGate)
            {
                this.drawing = drawing;
                var start = DateTime.UtcNow;
                Parallel.ForEach(lines.Values, ent => Parallel.ForEach(ent.DisplayPrimitives, p => p.Dispose()));

                // TODO: diff the drawing and only remove/generate the necessary elements
                lines.Clear();
                foreach (var layer in drawing.GetLayers().Where(l => l.IsVisible))
                {
                    // TODO: parallelize this.  requires `lines` to be concurrent dictionary
                    foreach (var entity in layer.GetEntities())
                    {
                        lines[entity.Id] = GenerateEntitySegments(entity, layer.Color);
                    }
                }

                // clear rubber band lines
                rubberBandLines = null;
                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                inputService.WriteLineDebug("DrawingChanged in {0} ms", elapsed);
            }

            ForceRender();
        }

        private void ViewPortChanged()
        {
            var bottomLeft = workspace.ActiveViewPort.BottomLeft;
            var height = (float)workspace.ActiveViewPort.ViewHeight;
            var width = (float)(height * viewHost.DisplayWidth / viewHost.DisplayHeight);
            projectionMatrix = Matrix.Identity
                * Matrix.Translation((float)-bottomLeft.X, (float)-bottomLeft.Y, 0)
                * Matrix.Translation(-width / 2.0f, -height / 2.0f, 0)
                * Matrix.Scaling(2.0f / width, 2.0f / height, 1.0f);
            projectionWorldMatrix = projectionMatrix * worldMatrix;
            projectionViewWorldMatrix = projectionMatrix * viewMatrix * worldMatrix;
            ForceRender();
        }

        private void SelectedEntitiesCollectionChanged(object sender, EventArgs e)
        {
            ForceRender();
        }

        private void CommandExecuted(object sender, CommandExecutedEventArgs e)
        {
            rubberBandLines = null;
            selecting = false;
        }

        private void InputServiceValueReceived(object sender, ValueReceivedEventArgs e)
        {
            selecting = false;
            ForceRender();
        }

        private void InputServiceValueRequested(object sender, ValueRequestedEventArgs e)
        {
            GenerateRubberBandLines(viewHost.GetCursorPoint());
            selecting = false;
            ForceRender();
        }

        #endregion

        #region Primitive generator functions

        private Color4 GetDisplayColor(IndexedColor layerColor, IndexedColor primitiveColor)
        {
            Color4 display;
            if (!primitiveColor.IsAuto)
                display = new Color4(primitiveColor.ToInt());
            else if (!layerColor.IsAuto)
                display = new Color4(layerColor.ToInt());
            else
                display = autoColor;

            return display;
        }

        private void GenerateRubberBandLines(Point worldPoint)
        {
            var generator = inputService.PrimitiveGenerator;
            rubberBandLines = generator == null
                ? null
                : generator(worldPoint).Select(p => GenerateDisplayPrimitive(p, autoColor, false)).ToArray();

            if (generator != null || lastGeneratorNonNull)
            {
                ForceRender();
            }

            lastGeneratorNonNull = generator != null;
        }

        private TransformedEntity GenerateEntitySegments(Entity entity, IndexedColor layerColor)
        {
            return new TransformedEntity(entity,
                entity.GetPrimitives().Select(p => GenerateDisplayPrimitive(p, GetDisplayColor(layerColor, p.Color))));
        }

        private IDisplayPrimitive GenerateDisplayPrimitive(IPrimitive primitive, Color4 color, bool highQuality = true)
        {
            IDisplayPrimitive display;
            Vector normal = null, right = null, up = null;
            Matrix4 trans;
            switch (primitive.Kind)
            {
                case PrimitiveKind.Text:
                    var text = (PrimitiveText)primitive;
                    var f = System.Drawing.SystemFonts.DefaultFont;
                    var sc = (float)text.Height;
                    var rad = text.Rotation * MathHelper.DegreesToRadians;
                    normal = text.Normal;
                    right = new Vector(Math.Cos(rad), Math.Sin(rad), 0.0).Normalize();
                    up = normal.Cross(right).Normalize();
                    var mesh = Mesh.CreateText(Device, f, text.Value, highQuality ? 0.2f : 0.5f, float.Epsilon);
                    trans = Matrix4.FromUnitCircleProjection(normal, right, up, text.Location, sc, sc, sc);
                    display = new DisplayPrimitiveMesh(mesh, color, trans.ToMatrix(), normalPixelShader, selectedPixelShader);
                    break;
                case PrimitiveKind.Line:
                    var line = (PrimitiveLine)primitive;
                    display = new DisplayPrimitiveLines(
                        color,
                        new[] {
                            new Vector3((float)line.P1.X, (float)line.P1.Y, (float)line.P1.Z),
                            new Vector3((float)line.P2.X, (float)line.P2.Y, (float)line.P2.Z)
                        },
                        solidLine,
                        dashedLine);
                    break;
                case PrimitiveKind.Ellipse:
                    var el = (PrimitiveEllipse)primitive;
                    double startAngle = el.StartAngle;
                    double endAngle = el.EndAngle;
                    double radiusX = el.MajorAxis.Length;
                    double radiusY = radiusX * el.MinorAxisRatio;
                    var center = el.Center;
                    normal = el.Normal;
                    right = el.MajorAxis;

                    normal = normal.Normalize();
                    right = right.Normalize();
                    up = normal.Cross(right).Normalize();
                    startAngle *= MathHelper.DegreesToRadians;
                    endAngle *= MathHelper.DegreesToRadians;
                    var coveringAngle = endAngle - startAngle;
                    if (coveringAngle < 0.0) coveringAngle += MathHelper.TwoPI;
                    var fullSegCount = highQuality ? FullCircleDrawingSegments : LowQualityCircleDrawingSegments;
                    var segCount = Math.Max(3, (int)(coveringAngle / MathHelper.TwoPI * (double)fullSegCount));
                    var segments = new Vector3[segCount];
                    var angleDelta = coveringAngle / (double)(segCount - 1);
                    var angle = startAngle;
                    trans = Matrix4.FromUnitCircleProjection(normal, right, up, center, radiusX, radiusY, 1.0);
                    var start = DateTime.UtcNow;
                    for (int i = 0; i < segCount; i++, angle += angleDelta)
                    {
                        var result = trans.Transform(new Point(Math.Cos(angle), Math.Sin(angle), 0));
                        segments[i] = new Vector3((float)result.X, (float)result.Y, (float)result.Z);
                    }
                    var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                    display = new DisplayPrimitiveLines(
                        color,
                        segments,
                        solidLine,
                        dashedLine);
                    break;
                default:
                    throw new ArgumentException("primitive.Kind");
            }

            return display;
        }

        #endregion

        private void ForceRender()
        {
            control.ForceRendering();
        }

    }
}
