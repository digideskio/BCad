﻿using System.ComponentModel.Composition;
using BCad.Objects;

namespace BCad.Commands
{
    [ExportCommand("Draw.Line", "line", "l")]
    internal class DrawLineCommand : ICommand
    {
        [Import]
        private IInputService InputService = null;

        [Import]
        private IWorkspace Workspace = null;

        [Import]
        private IUndoRedoService UndoRedoService = null;

        public bool Execute(params object[] parameters)
        {
            var input = InputService.GetPoint(new UserDirective("From"));
            if (input.Cancel) return false;
            var first = input.Value;
            Point last = first;
            while (true)
            {
                var current = InputService.GetPoint(new UserDirective("Next or [c]lose", "c"), (p) =>
                {
                    return new[] { new Line(last, p, Color.Default) };
                });
                if (current.Cancel) break;
                if (current.HasValue)
                {
                    UndoRedoService.SetSnapshot();
                    Workspace.AddToCurrentLayer(new Line(last, current.Value, Color.Default));
                    last = current.Value;
                    if (last == first) break; // closed
                }
                else
                {
                    if (current.Directive == "c")
                    {
                        if (last != first)
                        {
                            UndoRedoService.SetSnapshot();
                            Workspace.AddToCurrentLayer(new Line(last, first, Color.Default));
                        }
                        break;
                    }
                }
            }

            return true;
        }

        public string DisplayName
        {
            get { return "LINE"; }
        }
    }
}
