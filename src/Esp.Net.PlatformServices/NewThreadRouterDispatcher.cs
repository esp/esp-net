#region copyright
// Copyright 2015 Keith Woods
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Threading;

namespace Esp.Net
{
    public class NewThreadRouterDispatcher : IRouterDispatcher
    {
        public static IRouterDispatcher Create()
        {
            return new NewThreadRouterDispatcher();
        }

        private NewThreadRouterDispatcher()
        {

            // var x = new System.Threading.Thread();
        }
        
        public bool CheckAccess()
        {
            return true;
        }

        public void EnsureAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("Router accessed on invalid thread");
            }
        }

        public void Dispatch(Action action)
        {
            throw new NotImplementedException();
        }

        private void Run()
        {
        }
    }
}