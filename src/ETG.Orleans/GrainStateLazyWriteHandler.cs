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
using Orleans;
using Orleans.Runtime;

namespace ETG.Orleans
{
    /// <summary>
    /// Handles lazy write of Grain State. This class has been designed to allow connecting a Grain state lazy write handler to a timer.
    /// </summary>
    public class GrainStateLazyWriteHandler
    {
      private IGrainState _currentState;
        private readonly string _grainId;
        private readonly Logger _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="logger"></param>
        public GrainStateLazyWriteHandler(Grain grain, Logger logger)
        {
            _grainId = GetGrainIdAsString(grain);
            _logger = logger;
        }

        /// <summary>
        /// Can be used to connect an Orleans timer to a lazy write of the state.
        /// </summary>
        /// <param name="stateObj"></param>
        /// <returns></returns>
        public async Task WriteState(object stateObj)
        {
            IGrainState state = stateObj as IGrainState;
            if (state == null)
            {
                throw new ArgumentException("stateObj should be an implementation of IGrainState");    
            }

            await WriteState(state);
        }

        /// <summary>
        /// Will write the given state if it's different than the previously persisted state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task WriteState(IGrainState state)
        {
            if (!state.Equals(_currentState))
            {
                try
                {
                    await state.WriteStateAsync();
                    _currentState = state;
                }
                catch (Exception e)
                {
                    _logger.Error(0, String.Format("Failed to write the State of Grain {0}", _grainId), e);
                }
            }
        }

        private static string GetGrainIdAsString(Grain grain)
        {
            if (grain is IGrainWithStringKey)
            {
                return ((IGrainWithStringKey) grain).GetPrimaryKeyString();
            }
            if (grain is IGrainWithIntegerKey)
            {
                return grain.GetPrimaryKeyLong().ToString();
            }
            return grain.GetPrimaryKey().ToString();
        }
    }
}
