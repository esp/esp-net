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

#if ESP_EXPERIMENTAL
using System;

namespace Esp.Net.Plugins.HeldEvents
{
    public class HeldEventActionEvent
    {
        public HeldEventActionEvent(Guid eventId, HeldEventAction action)
        {
            Action = action;
            EventId = eventId;
        }

        public HeldEventAction Action { get; private set; }

        public Guid EventId { get; private set; }
    }
}
#endif