﻿/* Copyright 2013 Michael Braude and individual contributors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using OneBusAway.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneBusAway.ViewModels
{
    public class StopSelectedEventArgs : EventArgs
    {
        public StopSelectedEventArgs(string stopName, string selectedStopId, string direction, double latitude, double longitude)
        {
            this.StopName = stopName;
            this.SelectedStopId = selectedStopId;
            this.Direction = direction;
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public double Latitude
        {
            get;
            private set;
        }

        public double Longitude
        {
            get;
            private set;
        }

        public string StopName
        {
            get;
            private set;
        }

        public string SelectedStopId
        {
            get;
            private set;
        }

        public string Direction
        {
            get;
            private set;
        }
    }
}
