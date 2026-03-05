//  Copyright (c) 2026 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alachisoft.NCache.Management.Monitoring.Events
{
    public class MetricServerEventPublisher
    {
        #region Fields
        private List<string> metricServerEventSources;
        private EventObserver _observer;
        private IMetricServer _metricServer;

        #region Static-Fields
        private static MetricServerEventPublisher _publisher;
        #endregion
        #endregion

        #region Properties
        public IMetricServer MetricServer { get => _metricServer; set => _metricServer = value; }
        #endregion

        #region Methods

        #region Constructor
        public static MetricServerEventPublisher Instance
        {
            get
            {
                if (_publisher == null)
                {
                    _publisher = new MetricServerEventPublisher();
                }
                return _publisher;
            }
        }

        private MetricServerEventPublisher()
        {
            Initialize();
            AddSources();
        }
        #endregion

        #region Protected-Methods

        protected IEnumerable<string> PopulateEventSources()
        {
            metricServerEventSources = new List<string>();
            metricServerEventSources.Add("NCache");
            metricServerEventSources.Add("NCacheSvc");
            metricServerEventSources.Add("NBridgeSvc");
            metricServerEventSources.Add("NCache Bridge");
            metricServerEventSources.Add("BridgeService");
            
            return metricServerEventSources;
        }

        protected virtual void OnEventAdded(object sender, EventAddedArguments e)
        {
            EventData data = new EventData
            {
                EventId = e.EventEntry.InstanceID,
                Level = SetEventLevel(e.EventEntry.EventType),
                Timestamp = e.EventEntry.TimeGenerated,
                Source = e.EventEntry.Source,
                Message = e.EventEntry.Message,            
                Publisher = Publisher.NCache,
                Version = "NCache-5.0-SP2",
            };


            if (e.EventEntry.Source == "NBridgeSvc"|| e.EventEntry.Source == "NCache Bridge" || e.EventEntry.Source == "BridgeService")
            {
                data.Publisher = Publisher.Bridge;
            }
        }

        protected EventsLevel SetEventLevel(EventLogEntryType eventType)
        {
            switch (eventType)
            {
                case EventLogEntryType.FailureAudit:
                    return EventsLevel.FailureAudit;
                case EventLogEntryType.Information:
                    return EventsLevel.Information;
                case EventLogEntryType.SuccessAudit:
                    return EventsLevel.SuccessAudit;
                case EventLogEntryType.Warning:
                    return EventsLevel.Warning;
                default:
                    return EventsLevel.Error;
            }
        }
        #endregion

        #region Public-Methods
        public void Initialize()
        {
            _observer = new EventObserver();
            _observer.EventAdded += OnEventAdded;
        }

        public void AddSources()
        {
            PopulateEventSources();
            _observer.RegisterEventViewerEvents(metricServerEventSources);
        }
        #endregion

        #endregion

    }
}

