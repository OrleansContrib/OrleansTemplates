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
using System.Data.HashFunction;
using System.Text;

namespace ETG.Orleans.Swmr
{
    public class StaticTopology<T> : ITopology<T>
    {
        private readonly T[] _nodes;
        private readonly MurmurHash3 _murmurHash3;

        public StaticTopology(T[] nodes)
        {
            _murmurHash3 = new MurmurHash3();
            _nodes = nodes;
        }

        public IEnumerable<T> Nodes
        {
            get { return _nodes; }
        }

        public T GetNode(string key)
        {
            int index = Math.Abs(GetHashCode(key)) % _nodes.Length;
            return _nodes[index];
        }

        private int GetHashCode(string sessionId)
        {
            return BitConverter.ToInt32(_murmurHash3.ComputeHash(Encoding.ASCII.GetBytes(sessionId)), 0);
        }

        public void Add(T node)
        {
            throw new NotImplementedException();
        }

        public void Remove(T node)
        {
            throw new NotImplementedException();
        }
    }

}
