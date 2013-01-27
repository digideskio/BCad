﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BCad.Collections
{
    public class ReadOnlyTree<TKey, TValue> where TKey : IComparable
    {
        private class Node
        {
            private Node left;
            private Node right;

            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public Node Left
            {
                get { return left; }
                set
                {
                    left = value;
                    RecalculateHeight();
                }
            }
            public Node Right
            {
                get { return right; }
                set
                {
                    right = value;
                    RecalculateHeight();
                }
            }
            public int Height { get; private set; }

            public Node(TKey key, TValue value, Node left, Node right)
            {
                Key = key;
                Value = value;
                this.left = left;
                this.right = right;
                RecalculateHeight();
            }

            public Node Clone()
            {
                return new Node(Key, Value, Left, Right);
            }

            public int BalanceFactor
            {
                get { return GetHeight(Left) - GetHeight(Right); }
            }

            private void RecalculateHeight()
            {
                Height = Math.Max(GetHeight(Left), GetHeight(Right)) + 1;
            }

            public override string ToString()
            {
                return string.Format("{0}", Key);
            }

            private static int GetHeight(Node node)
            {
                if (node == null)
                    return 0;
                return node.Height;
            }
        }

        private Node root;

        public ReadOnlyTree()
            : this(null)
        {
        }

        private ReadOnlyTree(Node root)
        {
            this.root = root;
        }

        private ReadOnlyTree(TKey key, TValue value, bool isRed)
        {
            root = new Node(key, value, null, null);
        }

        public int Count
        {
            get
            {
                return GetCount(root);
            }
        }

        private static int GetCount(Node node)
        {
            if (node == null)
                return 0;
            return GetCount(node.Left) + GetCount(node.Right) + 1;
        }

        /// <summary>
        /// Insert the given values and return the new resultant tree.
        /// </summary>
        public ReadOnlyTree<TKey, TValue> Insert(TKey key, TValue value)
        {
            var current = root;
            var newNode = new Node(key, value, null, null);
            var path = new Stack<Node>();
            while (current != null)
            {
                var comp = key.CompareTo(current.Key);
                if (comp < 0)
                {
                    // go left
                    path.Push(current);
                    current = current.Left;
                }
                else if (comp > 0)
                {
                    // go right
                    path.Push(current);
                    current = current.Right;
                }
                else
                {
                    // replace.  parent is correct
                    newNode.Left = current.Left;
                    newNode.Right = current.Right;
                    break;
                }
            }

            // re-spine the tree
            var newRoot = BalanceAndReSpine(newNode, path, true);
            return new ReadOnlyTree<TKey, TValue>(newRoot);
        }

        /// <summary>
        /// Delete the specified value and return the resultant tree.
        /// </summary>
        public ReadOnlyTree<TKey, TValue> Delete(TKey key)
        {
            var current = root;
            var path = new Stack<Node>();
            while (current != null)
            {
                var comp = key.CompareTo(current.Key);
                if (comp < 0)
                {
                    // go left
                    path.Push(current);
                    current = current.Left;
                }
                else if (comp > 0)
                {
                    // go right
                    path.Push(current);
                    current = current.Right;
                }
                else
                {
                    // found it
                    break;
                }
            }

            if (current == null)
                return this; // key not found, no change

            Node newRoot;
            if (current.Left == null && current.Right == null)
            {
                // just delete this node
                if (path.Count == 0)
                {
                    // deleting the only node
                    newRoot = null;
                }
                else
                {
                    var parent = path.Pop().Clone();
                    if (current.Key.CompareTo(parent.Key) < 0)
                        parent.Left = null;
                    else
                        parent.Right = null;
                    newRoot = BalanceAndReSpine(parent, path, false);
                }
            }
            else if (current.Left == null && current.Right != null)
            {
                // the right child takes the node's place, and rebalance to the top
                var newNode = current.Right.Clone();
                newRoot = BalanceAndReSpine(newNode, path, false);
            }
            else if (current.Left != null && current.Right == null)
            {
                // the left child takes the node's place, and rebalance to the top
                var newNode = current.Left.Clone();
                newRoot = BalanceAndReSpine(newNode, path, false);
            }
            else
            {
                // both children are present.  replace node with immediate successor and custom respine
                var immediateSuccessor = current.Right;
                while (immediateSuccessor.Left != null)
                {
                    path.Push(immediateSuccessor);
                    immediateSuccessor = immediateSuccessor.Left;
                }

                current = immediateSuccessor;
                while (path.Count > 0)
                {
                    // recreate parent
                    var newParent = path.Pop().Clone();
                    if (newParent.Key.CompareTo(key) == 0)
                    {
                        // this is the replacement node
                        newParent.Key = immediateSuccessor.Key;
                        newParent.Value = immediateSuccessor.Value;
                    }
                    else
                    {
                        if (current.Key.CompareTo(newParent.Key) < 0)
                            newParent.Left = current;
                        else
                            newParent.Right = current;
                    }

                    current = Rebalance(newParent, false);
                }

                newRoot = current;
            }

            return new ReadOnlyTree<TKey, TValue>(newRoot);
        }

        private static Node BalanceAndReSpine(Node current, Stack<Node> ancestors, bool insert)
        {
            while (ancestors.Count > 0)
            {
                // re-create parent
                var newParent = ancestors.Pop().Clone();
                if (current.Key.CompareTo(newParent.Key) < 0)
                    newParent.Left = current;
                else
                    newParent.Right = current;
                current = Rebalance(newParent, insert);
            }

            return current;
        }

        private static Node Rebalance(Node node, bool insert)
        {
            var balanceFactor = node.BalanceFactor;
            if (Math.Abs(balanceFactor) <= 1)
                return node; // no balance necessary
            Debug.Assert(Math.Abs(balanceFactor) == 2);
            if (balanceFactor == -2) // right-right or right-left
                return RightRebalance(node, insert);
            else // left-left or left-right
                return LeftRebalance(node, insert);
        }

        private static Node RightRebalance(Node node, bool insert)
        {
            var r = node.Right;
            var bf = r.BalanceFactor;
            if (bf == -1 || (!insert && bf == 0))
                return RightRightRebalance(node);
            else
                return RightLeftRebalance(node);
        }

        private static Node RightRightRebalance(Node root)
        {
            var newLeftChild = root.Clone();
            newLeftChild.Right = root.Right.Left;
            var newRoot = root.Right.Clone();
            newRoot.Left = newLeftChild;
            return newRoot;
        }

        private static Node RightLeftRebalance(Node root)
        {
            var newRightGrandchild = root.Right.Clone();
            newRightGrandchild.Left = root.Right.Left.Right;
            var newRightChild = root.Right.Left.Clone();
            newRightChild.Right = newRightGrandchild;
            var newRoot = root.Clone();
            newRoot.Right = newRightChild;
            return RightRightRebalance(newRoot);
        }

        private static Node LeftRebalance(Node node, bool insert)
        {
            var l = node.Left;
            var bf = l.BalanceFactor;
            if (bf == 1 || (!insert && bf == 0))
                return LeftLeftRebalance(node);
            else
                return LeftRightRebalance(node);
        }

        private static Node LeftLeftRebalance(Node root)
        {
            var newRightChild = root.Clone();
            newRightChild.Left = root.Left.Right;
            var newRoot = root.Left.Clone();
            newRoot.Right = newRightChild;
            return newRoot;
        }

        private static Node LeftRightRebalance(Node root)
        {
            var newLeftGrandchild = root.Left.Clone();
            newLeftGrandchild.Right = root.Left.Right.Left;
            var newLeftChild = root.Left.Right.Clone();
            newLeftChild.Left = newLeftGrandchild;
            var newRoot = root.Clone();
            newRoot.Left = newLeftChild;
            return LeftLeftRebalance(newRoot);
        }

        public bool KeyExists(TKey key)
        {
            TValue temp;
            return TryFind(key, out temp);
        }

        public bool TryFind(TKey key, out TValue value)
        {
            var current = root;
            while (current != null)
            {
                var comp = key.CompareTo(current.Key);
                if (comp < 0)
                    current = current.Left;
                else if (comp > 0)
                    current = current.Right;
                else
                {
                    value = current.Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }
    }
}