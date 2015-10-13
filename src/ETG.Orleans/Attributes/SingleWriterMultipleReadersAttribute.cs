using System;

namespace ETG.Orleans.Attributes
{
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

    /// <summary>
    /// This attribute can be placed on a grain to mark it as a read-write grain. Readonly methods can execute reads in parallel 
    /// if the number of replicas specified is higher than 1 (1 is the minium). Note that the number of calls that will execute
    /// in parallel is bounded by the hardware capacity (typically the number of cores). Also keep in mind that increasing the number 
    /// of replicas increases the memory usage (linearly).
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class SingleWriterMultipleReadersAttribute : Attribute
    {
        /// <summary>
        /// The number of read replicas to create.
        /// </summary>
        public int ReadReplicaCount { get; set; }
    }
}
