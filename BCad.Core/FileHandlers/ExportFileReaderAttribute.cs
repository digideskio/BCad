﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace BCad.FileHandlers
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportFileReaderAttribute : ExportAttribute, IFileReaderMetadata
    {
        public string DisplayName { get; private set; }

        public IEnumerable<string> FileExtensions { get; private set; }

        public ExportFileReaderAttribute(string displayName, params string[] fileExtensions)
            : base(typeof(IFileReader))
        {
            DisplayName = displayName;
            FileExtensions = fileExtensions;
        }
    }
}
