using Alachisoft.NCache.Runtime.Caching.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Client
{
    internal class TopicIdentity
    {
        public string TopicName
        {
            get; set;
        }

        public TopicSearchOptions SearchOption
        {
            get; set;
        }

        public TopicIdentity(string topicName, TopicSearchOptions searchOption)
        {
            if (topicName == null)
                throw new ArgumentNullException(nameof(topicName));

            this.TopicName = topicName;
            this.SearchOption = searchOption;
        }

        public override int GetHashCode()
        {
            return this.TopicName.ToLower().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as TopicIdentity;

            if (other == default(TopicIdentity))
                return false;

            return this.TopicName.Equals(other.TopicName, StringComparison.InvariantCultureIgnoreCase) && this.SearchOption == other.SearchOption;
        }
    }
}
