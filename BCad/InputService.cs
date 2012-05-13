﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BCad.Entities;
using BCad.EventArguments;

namespace BCad
{
    [Export(typeof(IInputService))]
    internal class InputService : IInputService, IPartImportsSatisfiedNotification
    {
        [Import]
        private IWorkspace Workspace = null;

        public InputService()
        {
            PrimitiveGenerator = null;
            LastPoint = Point.Origin;
            Reset();
        }

        public void OnImportsSatisfied()
        {
            Workspace.CommandExecuted += Workspace_CommandExecuted;
        }

        void Workspace_CommandExecuted(object sender, CommandExecutedEventArgs e)
        {
            Workspace.SelectedEntities.Clear();
            SetPrompt("Command");
        }

        public void WriteLine(string text)
        {
            OnLineWritten(new WriteLineEventArgs(text));
        }

        public void WriteLine(string text, params object[] param)
        {
            WriteLine(string.Format(text, param));
        }

        private object callbackGate = new object();

        private RubberBandGenerator primitiveGenerator = null;

        public RubberBandGenerator PrimitiveGenerator
        {
            get { return primitiveGenerator; }
            private set
            {
                primitiveGenerator = value;
                OnRubberBandGeneratorChanged(new RubberBandGeneratorChangedEventArgs(primitiveGenerator));
            }
        }

        public Point LastPoint { get; private set; }

        public bool IsDrawing { get { return this.PrimitiveGenerator != null; } }

        private UserDirective currentDirective = null;

        public ValueOrDirective<Point> GetPoint(UserDirective directive, RubberBandGenerator onCursorMove = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Point));
            WaitFor(InputType.Point, directive, onCursorMove);

            ValueOrDirective<Point> result;
            switch (lastType)
            {
                case PushedValueType.None:
                    result = new ValueOrDirective<Point>();
                    break;
                case PushedValueType.Cancel:
                    result = ValueOrDirective<Point>.GetCancel();
                    break;
                case PushedValueType.Point:
                    result = new ValueOrDirective<Point>(pushedPoint);
                    LastPoint = pushedPoint;
                    break;
                case PushedValueType.Directive:
                    result = new ValueOrDirective<Point>(pushedDirective);
                    break;
                default:
                    throw new Exception("Unexpected pushed value");
            }

            WaitDone();
            return result;
        }

        public ValueOrDirective<Entity> GetEntity(UserDirective directive, RubberBandGenerator onCursorMove = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Entity));
            WaitFor(InputType.Entity, directive, onCursorMove);

            ValueOrDirective<Entity> result;
            switch (lastType)
            {
                case PushedValueType.None:
                    result = new ValueOrDirective<Entity>();
                    break;
                case PushedValueType.Cancel:
                    result = ValueOrDirective<Entity>.GetCancel();
                    break;
                case PushedValueType.Entity:
                    result = new ValueOrDirective<Entity>(pushedEntity);
                    break;
                case PushedValueType.Entities:
                    result = new ValueOrDirective<Entity>(pushedEntities.First());
                    break;
                case PushedValueType.Directive:
                    result = new ValueOrDirective<Entity>(pushedDirective);
                    break;
                default:
                    throw new Exception("Unexpected pushed value");
            }

            WaitDone();
            return result;
        }

        public ValueOrDirective<IEnumerable<Entity>> GetEntities(RubberBandGenerator onCursorMove = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Entities));

            ValueOrDirective<IEnumerable<Entity>>? result = null;
            HashSet<Entity> entities = new HashSet<Entity>();
            bool awaitingMore = true;
            while (awaitingMore)
            {
                WaitFor(InputType.Entities, new UserDirective("Select entities", "all"), onCursorMove);
                switch (lastType)
                {
                    case PushedValueType.Cancel:
                        result = ValueOrDirective<IEnumerable<Entity>>.GetCancel();
                        awaitingMore = false;
                        break;
                    case PushedValueType.Directive:
                        Debug.Fail("TODO: allow 'all' directive and 'r' to remove objects from selection");
                        break;
                    case PushedValueType.None:
                        result = new ValueOrDirective<IEnumerable<Entity>>(entities);
                        awaitingMore = false;
                        break;
                    case PushedValueType.Entity:
                        entities.Add(pushedEntity);
                        Workspace.SelectedEntities.Add(pushedEntity.Id);
                        // TODO: print status
                        break;
                    case PushedValueType.Entities:
                        foreach (var e in pushedEntities)
                        {
                            entities.Add(e);
                            Workspace.SelectedEntities.Add(e.Id);
                        }
                        // TODO: print status
                        break;
                    default:
                        throw new Exception("Unexpected pushed value");
                }
            }

            WaitDone();
            Debug.Assert(result != null, "result should never be null");
            return result.Value;
        }

        private void WaitFor(InputType type, UserDirective directive, RubberBandGenerator onCursorMove)
        {
            SetPrompt(directive.Prompt);
            currentDirective = directive;
            DesiredInputType = type;
            lastType = PushedValueType.None;
            pushedPoint = default(Point);
            pushedText = null;
            pushedEntity = null;
            pushedDirective = null;
            PrimitiveGenerator = onCursorMove;
            pushValueDone.Reset();
            pushValueDone.WaitOne();
        }

        private void WaitDone()
        {
            lastType = PushedValueType.None;
            pushedPoint = default(Point);
            pushedText = null;
            pushedEntity = null;
            pushedDirective = null;
            PrimitiveGenerator = null;
            currentDirective = null;
        }

        private enum PushedValueType
        {
            None,
            Cancel,
            Point,
            Entity,
            Entities,
            Directive
        }

        private object inputGate = new object();
        private PushedValueType lastType = PushedValueType.None;
        private Point pushedPoint = default(Point);
        private string pushedDirective = null;
        private Entity pushedEntity = null;
        private IEnumerable<Entity> pushedEntities = null;
        private string pushedText = null;
        private ManualResetEvent pushValueDone = new ManualResetEvent(false);
        private string lastCommand = null;

        public void Cancel()
        {
            lock (inputGate)
            {
                lastType = PushedValueType.Cancel;
                DesiredInputType = InputType.Command;
                pushValueDone.Set();
            }
        }

        public void PushValue(object value)
        {
            lock (inputGate)
            {
                bool valueReceived = false;
                if (DesiredInputType == InputType.Command)
                {
                    if (value == null && lastCommand != null)
                        Workspace.ExecuteCommand(lastCommand);
                    else if (value is string)
                    {
                        Workspace.ExecuteCommand((string)value);
                        lastCommand = (string)value;
                    }
                    OnValueReceived(new ValueReceivedEventArgs(lastCommand, InputType.Command));
                }
                else
                {
                    if (value is string && DesiredInputType != InputType.Text)
                    {
                        var directive = (string)value;
                        if (currentDirective.AllowableDirectives.Contains(directive))
                        {
                            lastType = PushedValueType.Directive;
                            pushedDirective = directive;
                            valueReceived = true;
                            OnValueReceived(new ValueReceivedEventArgs(pushedDirective, InputType.Directive));
                        }
                        else
                        {
                            WriteLine("Bad value or directive '{0}'", directive);
                        }
                    }
                    else
                    {
                        if (value == null)
                        {
                            lastType = PushedValueType.None;
                            valueReceived = true;
                            OnValueReceived(new ValueReceivedEventArgs());
                        }
                        else
                        {
                            switch (DesiredInputType)
                            {
                                case InputType.Point:
                                    lastType = PushedValueType.Point;
                                    pushedPoint = value as Point;
                                    valueReceived = true;
                                    OnValueReceived(new ValueReceivedEventArgs(pushedPoint));
                                    this.LastPoint = pushedPoint;
                                    break;
                                case InputType.Entity:
                                    lastType = PushedValueType.Entity;
                                    pushedEntity = value as Entity;
                                    valueReceived = true;
                                    OnValueReceived(new ValueReceivedEventArgs(pushedEntity));
                                    break;
                                case InputType.Entities:
                                    var enumerable = value as IEnumerable<Entity>;
                                    var entity = value as Entity;
                                    if (enumerable != null)
                                    {
                                        lastType = PushedValueType.Entities;
                                        pushedEntities = enumerable;
                                        valueReceived = true;
                                        OnValueReceived(new ValueReceivedEventArgs(pushedEntities));
                                    }
                                    else if (entity != null)
                                    {
                                        lastType = PushedValueType.Entity;
                                        pushedEntity = entity;
                                        valueReceived = true;
                                        OnValueReceived(new ValueReceivedEventArgs(pushedEntity));
                                    }
                                    else
                                    {
                                        Debug.Fail("Value was not Entity or IEnumerable<Entity>");
                                    }
                                    break;
                                case InputType.Text:
                                    lastType = PushedValueType.Directive;
                                    pushedText = value as string;
                                    valueReceived = true;
                                    OnValueReceived(new ValueReceivedEventArgs(pushedText, InputType.Text));
                                    break;
                            }
                        }
                    }
                }

                if (valueReceived)
                {
                    DesiredInputType = InputType.Command;
                    pushValueDone.Set();
                }
            }
        }

        public InputType DesiredInputType { get; private set; }

        private void SetPrompt(string prompt)
        {
            OnPromptChanged(new PromptChangedEventArgs(prompt));
        }

        public event PromptChangedEventHandler PromptChanged;

        protected virtual void OnPromptChanged(PromptChangedEventArgs e)
        {
            if (PromptChanged != null)
                PromptChanged(this, e);
        }

        public event WriteLineEventHandler LineWritten;

        protected virtual void OnLineWritten(WriteLineEventArgs e)
        {
            if (LineWritten != null)
                LineWritten(this, e);
        }

        public void Reset()
        {
            DesiredInputType = InputType.Command;
            SetPrompt("Command");
        }

        public event ValueRequestedEventHandler ValueRequested;

        protected virtual void OnValueRequested(ValueRequestedEventArgs e)
        {
            if (ValueRequested != null)
                ValueRequested(this, e);
        }

        public event ValueReceivedEventHandler ValueReceived;

        protected virtual void OnValueReceived(ValueReceivedEventArgs e)
        {
            if (ValueReceived != null)
                ValueReceived(this, e);
        }

        public event RubberBandGeneratorChangedEventHandler RubberBandGeneratorChanged;

        protected virtual void OnRubberBandGeneratorChanged(RubberBandGeneratorChangedEventArgs e)
        {
            if (RubberBandGeneratorChanged != null)
                RubberBandGeneratorChanged(this, e);
        }
    }
}
