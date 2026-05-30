// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    internal class ElasticsearchRallyState : State
    {
        public ElasticsearchRallyState(IDictionary<string, IConvertible> properties = null)
            : base(properties)
        {
        }

        public bool ElasticsearchStarted
        {
            get
            {
                return this.Properties.GetValue<bool>(nameof(ElasticsearchRallyState.ElasticsearchStarted), false);
            }

            set
            {
                this.Properties[nameof(ElasticsearchRallyState.ElasticsearchStarted)] = value;
            }
        }

        public bool RallyConfigured
        {
            get
            {
                return this.Properties.GetValue<bool>(nameof(ElasticsearchRallyState.RallyConfigured), false);
            }

            set
            {
                this.Properties[nameof(ElasticsearchRallyState.RallyConfigured)] = value;
            }
        }
    }
}
