// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.BCad.Json;
using Newtonsoft.Json;

namespace IxMilia.BCad.Server
{
    public class ServerAgent
    {
        private IWorkspace _workspace;
        public bool IsRunning { get; private set; }

        public ServerAgent(IWorkspace workspace)
        {
            _workspace = workspace;
            IsRunning = true;
        }

        public void ExecuteCommand(string command)
        {
            var _ = _workspace.ExecuteCommand(command).Result;
        }

        public string GetDrawing()
        {
            var jsonDrawing = new JsonDrawing(_workspace.Drawing);
            var json = JsonConvert.SerializeObject(jsonDrawing);
            return json;
        }

        public void ZoomIn()
        {
            _workspace.Update(activeViewPort: _workspace.ActiveViewPort.Update(viewHeight: _workspace.ActiveViewPort.ViewHeight * 0.8));
        }

        public void ZoomOut()
        {
            _workspace.Update(activeViewPort: _workspace.ActiveViewPort.Update(viewHeight: _workspace.ActiveViewPort.ViewHeight * 1.25));
        }
    }
}
