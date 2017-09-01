// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using BCad.Entities;
using System.Threading.Tasks;

namespace BCad.Server
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

        public string GetDrawing(int width, int height)
        {
            var transform = _workspace.ActiveViewPort.GetTransformationMatrixWindowsStyle(width, height);
            var sb = new StringBuilder();
            //sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"0 0 {width} {height}\">");
            sb.Append($"<svg width=\"{width}\" height=\"{height}\">");
            sb.Append("<g stroke=\"black\">");
            foreach (var line in _workspace.Drawing.GetEntities().OfType<Line>())
            {
                var p1 = transform.Transform(line.P1);
                var p2 = transform.Transform(line.P2);
                sb.Append($"<line x1=\"{p1.X}\" y1=\"{p1.Y}\" x2=\"{p2.X}\" y2=\"{p2.Y}\" />");
            }

            sb.Append("</g>");
            sb.Append("</svg>");
            return sb.ToString();
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
