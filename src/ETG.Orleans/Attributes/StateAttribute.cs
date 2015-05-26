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
using Orleans;
using Orleans.Providers;

namespace ETG.Orleans.Attributes
{
    /// <summary>
    /// This attribute can be placed on a grain implementation to indicate that the grain has a State and allows periodic writes of the grain state
    /// to storage in a lazy fashion. By default, the lazy write is turned off.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StateAttribute : Attribute
    {
        /// <summary>
        /// Default constructor: The Period is set to 5 and LazyWrite is turned off.
        /// </summary>
        public StateAttribute()
        {
            LazyWrite = false;
            Period = 5;
        }

        /// <summary>
        /// The type of the grain state which must be a subclass of <see cref="IGrainState"/>.
        /// Note that the Type MUST be specified using the typeof() operator right in the attribute definition; otherwise it won't work.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Turning ON the LazyWrite will tell the runtime to persist the State to storage every T seconds. The default value of T is 5s but this value
        /// can be change by setting the <see cref="Period"/> property.
        /// </summary>
        public bool LazyWrite { get; set; }

        /// <summary>
        /// The frequency at which the lazy write of the state will be performed. This property has no effect if <see cref="LazyWrite"/> is turned off.
        /// </summary>
        public long Period { get; set; }

        /// <summary>
        /// Used to define which storage provider to use for persistence of grain state. Using this property is equivalent to using the <see cref="StorageProviderAttribute "/> attribute.
        /// </summary>
        public String StorageProvider { get; set; }
    }
}
