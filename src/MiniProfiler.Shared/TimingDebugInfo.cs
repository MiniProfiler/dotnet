using System.Diagnostics;
using System.Runtime.Serialization;
using StackExchange.Profiling.Helpers;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Debug info for a timing, only present when EnableDebugMode is set in options
    /// </summary>
    [DataContract]
    public class TimingDebugInfo
    {
        /// <summary>
        /// An (already-encoded) HTML representation of the call stack.
        /// </summary>
        /// <remarks>
        /// Repetitive, but pays the prettification cost on fetch.
        /// We'll want to do diff with the parent timing here in highlight or something.
        /// </remarks>
        [DataMember(Order = 1)]
        public string RichHtmlStack => StackTraceUtils.HtmlPrettify(RawStack.ToString(), CommonStackStart);

        /// <summary>
        /// The index of the stack frame that common frames with parent start at (e.g. happened in the parent timing, before this).
        /// </summary>
        [DataMember(Order = 2)]
        public int? CommonStackStart { get; }

        private Timing ParentTiming { get; }
        private StackTrace RawStack { get; }

        internal TimingDebugInfo(Timing parent, int debugStackShave = 0)
        {
            ParentTiming = parent;
            RawStack = new StackTrace(4 + debugStackShave, true);

            if (parent.ParentTiming?.DebugInfo?.RawStack is StackTrace parentStack)
            {
                // Seek a common end in frames
                int myIndex, parentIndex;
                for (myIndex = RawStack.FrameCount - 1, parentIndex = parentStack.FrameCount - 1;
                     myIndex >= 0 && parentIndex >= 0;
                     myIndex--, parentIndex--)
                {
                    StackFrame myFrame = RawStack.GetFrame(myIndex),
                               parentFrame = parentStack.GetFrame(parentIndex);
                    if (myFrame.GetILOffset() == parentFrame.GetILOffset() && myFrame.GetMethod() == parentFrame.GetMethod())
                    {
                        CommonStackStart = myIndex;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
