// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace IxMilia.BCad.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().RunAsync().GetAwaiter().GetResult();
        }

        private async Task RunAsync()
        {
            //Console.WriteLine("starting run");
            var workspace = new RpcServerWorkspace();
            CompositionContainer.Container.SatisfyImports(workspace);

            var server = new ServerAgent(workspace);
            var serverRpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput(), server);
            //Console.WriteLine("server listening");
            ((FileSystemService)workspace.FileSystemService).Rpc = serverRpc;

            while (server.IsRunning)
            {
                await Task.Delay(50);
            }
        }
    }
}
