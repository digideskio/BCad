﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BCad.Entities;
using BCad.EventArguments;
using BCad.Primitives;

namespace BCad.Services
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

        public ValueOrDirective<double> GetDistance(double? defaultDistance = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Distance | InputType.Point));
            var prompt = "Offset distance or first point" +
                (defaultDistance.HasValue ? string.Format(" [{0}]", defaultDistance.Value) : "");
            WaitFor(InputType.Distance | InputType.Point, new UserDirective(prompt), null);
            ValueOrDirective<double> result;
            switch (lastType)
            {
                case PushedValueType.Cancel:
                    result = ValueOrDirective<double>.GetCancel();
                    break;
                case PushedValueType.None:
                    result = new ValueOrDirective<double>();
                    break;
                case PushedValueType.Distance:
                    result = new ValueOrDirective<double>(pushedDistance);
                    break;
                case PushedValueType.Point:
                    var first = pushedPoint;
                    WaitDone();
                    var second = GetPoint(new UserDirective("Second point of offset distance"), p =>
                        {
                            return new[] { new PrimitiveLine(first, p) };
                        });
                    if (second.HasValue)
                    {
                        var dist = (second.Value - first).Length;
                        result = new ValueOrDirective<double>(dist);
                    }
                    else if (second.Directive != null)
                    {
                        result = new ValueOrDirective<double>(second.Directive);
                    }
                    else
                    {
                        result = ValueOrDirective<double>.GetCancel();
                    }
                    break;
                default:
                    throw new Exception("Unexpected pushed value");
            }

            WaitDone();
            return result;
        }

        public ValueOrDirective<Point> GetPoint(UserDirective directive, RubberBandGenerator onCursorMove = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Point | InputType.Directive));
            WaitFor(InputType.Point | InputType.Directive, directive, onCursorMove);
            ValueOrDirective<Point> result;
            switch (lastType)
            {
                case PushedValueType.Cancel:
                    result = ValueOrDirective<Point>.GetCancel();
                    break;
                case PushedValueType.None:
                    result = new ValueOrDirective<Point>();
                    break;
                case PushedValueType.Directive:
                    result = new ValueOrDirective<Point>(pushedDirective);
                    break;
                case PushedValueType.Point:
                    result = new ValueOrDirective<Point>(pushedPoint);
                    break;
                default:
                    throw new Exception("Unexpected pushed value");
            }

            WaitDone();
            return result;
        }

        public ValueOrDirective<SelectedEntity> GetEntity(UserDirective directive, RubberBandGenerator onCursorMove = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Entity | InputType.Directive));
            WaitFor(InputType.Entity | InputType.Directive, directive, onCursorMove);
            ValueOrDirective<SelectedEntity> result;
            switch (lastType)
            {
                case PushedValueType.None:
                    result = new ValueOrDirective<SelectedEntity>();
                    break;
                case PushedValueType.Cancel:
                    result = ValueOrDirective<SelectedEntity>.GetCancel();
                    break;
                case PushedValueType.Entity:
                    result = new ValueOrDirective<SelectedEntity>(pushedEntity);
                    break;
                case PushedValueType.Directive:
                    result = new ValueOrDirective<SelectedEntity>(pushedDirective);
                    break;
                default:
                    throw new Exception("Unexpected pushed value");
            }

            WaitDone();
            return result;
        }

        public ValueOrDirective<IEnumerable<Entity>> GetEntities(string prompt = null, RubberBandGenerator onCursorMove = null)
        {
            OnValueRequested(new ValueRequestedEventArgs(InputType.Entities | InputType.Directive));
            ValueOrDirective<IEnumerable<Entity>>? result = null;
            HashSet<Entity> entities = new HashSet<Entity>();
            bool awaitingMore = true;
            while (awaitingMore)
            {
                WaitFor(InputType.Entities | InputType.Directive, new UserDirective(prompt ?? "Select entities", "all"), onCursorMove);
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
                        entities.Add(pushedEntity.Entity);
                        Workspace.SelectedEntities.Add(pushedEntity.Entity);
                        // TODO: print status
                        break;
                    case PushedValueType.Entities:
                        foreach (var e in pushedEntities)
                        {
                            entities.Add(e);
                            Workspace.SelectedEntities.Add(e);
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
            AllowedInputTypes = type;
            lastType = PushedValueType.None;
            pushedPoint = default(Point);
            pushedEntity = null;
            pushedDirective = null;
            PrimitiveGenerator = onCursorMove;
            pushValueDone.Reset();
            pushValueDone.WaitOne();
        }

        private void WaitDone()
        {
            AllowedInputTypes = InputType.Command;
            lastType = PushedValueType.None;
            pushedPoint = default(Point);
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
            Directive,
            Distance
        }

        private object inputGate = new object();
        private PushedValueType lastType = PushedValueType.None;
        private Point pushedPoint = default(Point);
        private double pushedDistance = 0.0;
        private string pushedDirective = null;
        private SelectedEntity pushedEntity = null;
        private IEnumerable<Entity> pushedEntities = null;
        private ManualResetEvent pushValueDone = new ManualResetEvent(false);

        public void Cancel()
        {
            lock (inputGate)
            {
                lastType = PushedValueType.Cancel;
                AllowedInputTypes = InputType.Command;
                pushValueDone.Set();
            }
        }

        public void PushNone()
        {
            lock (inputGate)
            {
                if (AllowedInputTypes == InputType.Command)
                {
                    PushCommand(null);
                }
                else
                {
                    lastType = PushedValueType.None;
                    OnValueReceived(new ValueReceivedEventArgs());
                }
            }
        }

        public void PushCommand(string commandName)
        {
            lock (inputGate)
            {
                OnValueReceived(new ValueReceivedEventArgs(commandName, InputType.Command));
                Workspace.ExecuteCommand(commandName);
            }
        }

        public void PushDirective(string directive)
        {
            if (directive == null)
            {
                throw new ArgumentNullException("directive");
            }

            lock (inputGate)
            {
                if (AllowedInputTypes.HasFlag(InputType.Directive))
                {
                    if (currentDirective.AllowableDirectives.Contains(directive))
                    {
                        pushedDirective = directive;
                        lastType = PushedValueType.Directive;
                        OnValueReceived(new ValueReceivedEventArgs(directive, InputType.Directive));
                    }
                    else
                    {
                        WriteLine("Bad value or directive '{0}'", directive);
                    }
                }
            }
        }

        public void PushDistance(double distance)
        {
            lock (inputGate)
            {
                if (AllowedInputTypes.HasFlag(InputType.Distance))
                {
                    pushedDistance = distance;
                    lastType = PushedValueType.Distance;
                    OnValueReceived(new ValueReceivedEventArgs(distance));
                }
            }
        }

        public void PushEntity(SelectedEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            lock (inputGate)
            {
                if (AllowedInputTypes.HasFlag(InputType.Entity))
                {
                    pushedEntity = entity;
                    lastType = PushedValueType.Entity;
                    OnValueReceived(new ValueReceivedEventArgs(entity));
                }
            }
        }

        public void PushEntities(IEnumerable<Entity> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }

            lock (inputGate)
            {
                if (AllowedInputTypes.HasFlag(InputType.Entities))
                {
                    pushedEntities = entities;
                    lastType = PushedValueType.Entities;
                    OnValueReceived(new ValueReceivedEventArgs(entities));
                }
            }
        }

        public void PushPoint(Point point)
        {
            if (point == null)
            {
                throw new ArgumentNullException("point");
            }

            lock (inputGate)
            {
                if (AllowedInputTypes.HasFlag(InputType.Point))
                {
                    pushedPoint = point;
                    lastType = PushedValueType.Point;
                    LastPoint = point;
                    OnValueReceived(new ValueReceivedEventArgs(point));
                }
            }
        }

        public InputType AllowedInputTypes { get; private set; }

        public IEnumerable<string> AllowedDirectives
        {
            get
            {
                if (currentDirective == null)
                {
                    return new string[0];
                }
                else
                {
                    return currentDirective.AllowableDirectives;
                }
            }
        }

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
            AllowedInputTypes = InputType.Command;
            SetPrompt("Command");
        }

        public event ValueRequestedEventHandler ValueRequested;

        protected virtual void OnValueRequested(ValueRequestedEventArgs e)
        {
            AllowedInputTypes = e.InputType;
            if (ValueRequested != null)
                ValueRequested(this, e);
        }

        public event ValueReceivedEventHandler ValueReceived;

        protected virtual void OnValueReceived(ValueReceivedEventArgs e)
        {
            if (ValueReceived != null)
                ValueReceived(this, e);
            pushValueDone.Set();
        }

        public event RubberBandGeneratorChangedEventHandler RubberBandGeneratorChanged;

        protected virtual void OnRubberBandGeneratorChanged(RubberBandGeneratorChangedEventArgs e)
        {
            if (RubberBandGeneratorChanged != null)
                RubberBandGeneratorChanged(this, e);
        }
    }
}