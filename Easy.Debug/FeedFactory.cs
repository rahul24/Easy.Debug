using Easy.Debug.Feeds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.Debug
{
    public class FeedFactory
    {
        public static IFeed GetFeedInstance(FeedType feedtype)
        {
            IFeed instance = null;
            if(feedtype == FeedType.StackOverflow)
            {
                instance = new StackOverflowFeed();
            }

            return instance;
        }
    }

    public enum FeedType
    {
        StackOverflow
    }
}
