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
using System.Threading.Tasks;
using ETG.Orleans;
using ETG.Orleans.Attributes;
using GrainInterfaces;
using Orleans;
using Orleans.Concurrency;

namespace Grains
{
    [Reentrant]
    public class MyGrain : MyGrainBase, IMyGrainInterface
    {
        public override Task OnActivateAsync()
        {
            RegisterTimer(new GrainStateLazyWriteHandler(this, GetLogger()).WriteState, State, TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5));

            Console.WriteLine(State.AsDictionary());
            return base.OnActivateAsync();
        }

        public Task SetValue(string value)
        {
            State.Value = value;
            return TaskDone.Done;
        }
    }
}
