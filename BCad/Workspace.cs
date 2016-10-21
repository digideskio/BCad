﻿using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using IxMilia.Config;

namespace BCad
{
    [Export(typeof(IWorkspace)), Shared]
    internal class Workspace : WorkspaceBase
    {
        private const string SettingsFile = ".bcadconfig";
        private Regex SettingsPattern = new Regex(@"^/p:([a-zA-Z]+)=(.*)$");

        private string FullSettingsFile
        {
            get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SettingsFile); }
        }

        public Workspace()
        {
            Update(drawing: Drawing.Update(author: Environment.UserName));
        }

        protected override ISettingsManager LoadSettings()
        {
            var manager = new SettingsManager();
            try
            {
                var lines = File.ReadAllLines(FullSettingsFile);
                manager.DeserializeConfig(lines);
            }
            catch
            {
                // don't care if we can't read the existing file because it might not exist
            }

            // Override settings provided via the command line in the form of "/p:SettingName=SettingValue".
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args.Skip(1))
            {
                var match = SettingsPattern.Match(arg);
                if (match.Success)
                {
                    var settingName = match.Groups[1].Value;
                    var settingValue = match.Groups[2].Value;
                    manager.DeserializeProperty(settingName, settingValue);
                }
            }

            return manager;
        }

        public override void SaveSettings()
        {
            string[] lines = new string[0];
            try
            {
                lines = File.ReadAllLines(FullSettingsFile);
            }
            catch
            {
                // don't care if we can't read the existing file because it might not exist
            }

            var newContent = ((SettingsManager)SettingsManager).SerializeConfig(lines);
            File.WriteAllText(FullSettingsFile, newContent);
        }

        public override async Task<UnsavedChangesResult> PromptForUnsavedChanges()
        {
            var result = UnsavedChangesResult.Discarded;
            if (this.IsDirty)
            {
                string filename = Drawing.Settings.FileName ?? "(Untitled)";
                var dialog = MessageBox.Show(string.Format("Save changes to '{0}'?", filename),
                    "Unsaved changes",
                    MessageBoxButton.YesNoCancel);
                switch (dialog)
                {
                    case MessageBoxResult.Yes:
                        var fileName = Drawing.Settings.FileName;
                        if (fileName == null)
                            fileName = await FileSystemService.GetFileNameFromUserForSave();
                        if (fileName == null)
                            result = UnsavedChangesResult.Cancel;
                        else if (await FileSystemService.TryWriteDrawing(fileName, Drawing, ActiveViewPort))
                            result = UnsavedChangesResult.Saved;
                        else
                            result = UnsavedChangesResult.Cancel;
                        break;
                    case MessageBoxResult.No:
                        result = UnsavedChangesResult.Discarded;
                        break;
                    case MessageBoxResult.Cancel:
                        result = UnsavedChangesResult.Cancel;
                        break;
                }
            }
            else
            {
                result = UnsavedChangesResult.Saved;
            }

            return result;
        }
    }
}
