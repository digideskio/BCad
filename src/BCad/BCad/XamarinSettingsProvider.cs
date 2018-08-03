// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.BCad.Settings;
using IxMilia.BCad.SnapPoints;

namespace IxMilia.BCad
{
    [ExportSetting(AllowedSnapPoints, typeof(SnapPointKind), SnapPointKind.All)]
    [ExportSetting(AngleSnap, typeof(bool), true)]
    [ExportSetting(BackgroundColor, typeof(CadColor), "#FF2F2F2F")]
    [ExportSetting(CursorSize, typeof(int), 60)]
    [ExportSetting(EntitySelectionRadius, typeof(double), 3.0)]
    [ExportSetting(HotPointColor, typeof(CadColor), "#FF0000FF")]
    [ExportSetting(LayerDialogId, typeof(string), "Default")]
    [ExportSetting(Ortho, typeof(bool), false)]
    [ExportSetting(PlotDialogId, typeof(string), "Default")]
    [ExportSetting(PointSize, typeof(double), 15.0)]
    [ExportSetting(PointSnap, typeof(bool), true)]
    [ExportSetting(RendererId, typeof(string), "Skia")]
    [ExportSetting(RibbonOrder, typeof(string[]), new[] { "home", "view", "settings", "debug" })]
    [ExportSetting(SnapAngleDistance, typeof(double), 30.0)]
    [ExportSetting(SnapAngles, typeof(double[]), new[] { 0.0, 90.0, 180.0, 270.0 })]
    [ExportSetting(SnapPointColor, typeof(CadColor), "#FFFFFF00")]
    [ExportSetting(SnapPointDistance, typeof(double), 15.0)]
    [ExportSetting(SnapPointSize, typeof(double), 15.0)]
    [ExportSetting(TextCursorSize, typeof(int), 18)]
    public class XamarinSettingsProvider
    {
        public const string Prefix = "UI.";
        public const string AllowedSnapPoints = Prefix + nameof(AllowedSnapPoints);
        public const string AngleSnap = Prefix + nameof(AngleSnap);
        public const string BackgroundColor = Prefix + nameof(BackgroundColor);
        public const string CursorSize = Prefix + nameof(CursorSize);
        public const string EntitySelectionRadius = Prefix + nameof(EntitySelectionRadius);
        public const string HotPointColor = Prefix + nameof(HotPointColor);
        public const string LayerDialogId = Prefix + nameof(LayerDialogId);
        public const string Ortho = Prefix + nameof(Ortho);
        public const string PlotDialogId = Prefix + nameof(PlotDialogId);
        public const string PointSize = Prefix + nameof(PointSize);
        public const string PointSnap = Prefix + nameof(PointSnap);
        public const string RendererId = Prefix + nameof(RendererId);
        public const string RibbonOrder = Prefix + nameof(RibbonOrder);
        public const string SnapAngleDistance = Prefix + nameof(SnapAngleDistance);
        public const string SnapAngles = Prefix + nameof(SnapAngles);
        public const string SnapPointColor = Prefix + nameof(SnapPointColor);
        public const string SnapPointDistance = Prefix + nameof(SnapPointDistance);
        public const string SnapPointSize = Prefix + nameof(SnapPointSize);
        public const string TextCursorSize = Prefix + nameof(TextCursorSize);
    }
}
