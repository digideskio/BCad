// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition.Hosting;
using System.Reflection;
using IxMilia.BCad.FileHandlers;

namespace IxMilia.BCad
{
    public class CompositionContainer : IDisposable
    {
        public static CompositionHost Container { get; private set; }

        static CompositionContainer()
        {
            var configuration = new ContainerConfiguration()
                .WithAssemblies(new[]
                {
                    typeof(CompositionContainer).GetTypeInfo().Assembly, // this
                    Assembly.GetEntryAssembly(), // e.g., WPF host
                    typeof(Drawing).GetTypeInfo().Assembly, // BCad.Core.dll
                    typeof(DxfFileHandler).GetTypeInfo().Assembly // BCad.FileHandlers.dll
                });
            Container = configuration.CreateContainer();
        }

        public void Dispose()
        {
            if (Container != null)
            {
                Container.Dispose();
                Container = null;
            }
        }
    }
}
