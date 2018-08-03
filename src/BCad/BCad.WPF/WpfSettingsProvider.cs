// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.BCad.Settings;

namespace IxMilia.BCad
{
    [ExportSetting(AngleSnapShortcut, typeof(KeyboardShortcut), "None+F7")]
    [ExportSetting(DebugShortcut, typeof(KeyboardShortcut), "None+F12")]
    [ExportSetting(OrthoShortcut, typeof(KeyboardShortcut), "None+F8")]
    [ExportSetting(PointSnapShortcut, typeof(KeyboardShortcut), "None+F3")]
    public class WpfSettingsProvider
    {
        public const string Prefix = "UI.";
        public const string AngleSnapShortcut = Prefix + nameof(AngleSnapShortcut);
        public const string DebugShortcut = Prefix + nameof(DebugShortcut);
        public const string OrthoShortcut = Prefix + nameof(OrthoShortcut);
        public const string PointSnapShortcut = Prefix + nameof(PointSnapShortcut);
    }
}
