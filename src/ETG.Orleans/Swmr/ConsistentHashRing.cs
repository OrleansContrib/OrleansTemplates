/*
Copyright (c) Microsoft Corporation
 
All rights reserved.
 
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the ""Software""), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETG.Orleans.Swmr
{
    [Serializable]
    public class ConsistentHashRing : ITopology
    {
        private SortedDictionary<int, string> _ring = new SortedDictionary<int, string>();
        private int _replicate = 100;   //default _replicate count
        private int[] _ayKeys = null;    //cache the ordered keys for better performance

        public ConsistentHashRing(int nodeCount)
        {
            var nodes = new List<string>();
            for (int i = 0; i < nodeCount; ++i)
            {
                nodes.Add(Convert.ToString(i));
            }
            Init(nodes, _replicate);
        }

        //it's better you override the GetHashCode() of T.
        //we will use GetHashCode() to identify different node.
        public ConsistentHashRing(ICollection<string> nodes)
        {
            Init(nodes, _replicate);
        }

        // we need this for the auto-serialization to work
        public SortedDictionary<int, string> Ring
        {
            get { return _ring; }
            set
            {
                _ring = value;
                _ayKeys = _ring.Keys.ToArray();
            }
        }

        // we need this for the auto-serialization to work
        public int Replicate
        {
            get { return _replicate; }
            set { _replicate = value; }
        }

        public void Init(IEnumerable<string> nodes, int replicate)
        {
            _replicate = replicate;

            foreach (var node in nodes)
            {
                this.Add(node, false);
            }
            _ayKeys = _ring.Keys.ToArray();
        }

        public void Add(string node)
        {
            Add(node, true);
        }

        private void Add(string node, bool updateKeyArray)
        {
            for (var i = 0; i < _replicate; i++)
            {
                var hash = BetterHash(node.GetHashCode().ToString() + i);
                _ring[hash] = node;
            }

            if (updateKeyArray)
            {
                _ayKeys = _ring.Keys.ToArray();
            }
        }

        public void Remove(string node)
        {
            for (var i = 0; i < _replicate; i++)
            {
                var hash = BetterHash(node.GetHashCode().ToString() + i);
                if (!_ring.Remove(hash))
                {
                    throw new Exception("can not remove a node that not added");
                }
            }
            _ayKeys = _ring.Keys.ToArray();
        }

        //return the index of first item that >= val.
        //if not exist, return 0;
        //ay should be ordered array.
        static int First_ge(IReadOnlyList<int> ay, int val)
        {
            var begin = 0;
            var end = ay.Count - 1;

            if (ay[end] < val || ay[0] > val)
            {
                return 0;
            }

            while (end - begin > 1)
            {
                var mid = (end + begin) / 2;
                if (ay[mid] >= val)
                {
                    end = mid;
                }
                else
                {
                    begin = mid;
                }
            }

            if (ay[begin] > val || ay[end] < val)
            {
                throw new Exception("should not happen");
            }

            return end;
        }

        public IEnumerable<string> Nodes
        {
            get { return _ring.Values; }
        }

        public string GetNode(string key)
        {
            //return GetNode_slow(key);

            var hash = BetterHash(key);

            var first = First_ge(_ayKeys, hash);

            //int diff = circle.Keys[first] - hash;

            return _ring[_ayKeys[first]];
        }

        //default String.GetHashCode() can't well spread strings like "1", "2", "3"
        public static int BetterHash(string key)
        {
            var hash = MurmurHash2.Hash(Encoding.ASCII.GetBytes(key));
            return (int)hash;
        }
    }
}
