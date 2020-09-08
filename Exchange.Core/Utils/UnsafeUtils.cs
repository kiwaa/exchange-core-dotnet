using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Utils
{
    public sealed class UnsafeUtils
    {

        //private static readonly long OFFSET_RESULT_CODE;
        //        private static readonly long OFFSET_EVENT;

        //        static UnsafeUtils()
        //        {
        //            try
        //            {
        //                //OFFSET_RESULT_CODE = UNSAFE.objectFieldOffset(OrderCommand.class.getDeclaredField("resultCode"));
        //                OFFSET_EVENT = UNSAFE.objectFieldOffset(typeof(OrderCommand).getDeclaredField("matcherEvent"));
        //        } catch (Exception ex) {
        //            throw new InvalidOperationException(ex);
        //}
        //    }

        public static void setResultVolatile(OrderCommand cmd,
                                             bool result,
                                             CommandResultCode successCode,
                                             CommandResultCode failureCode)
        {

            CommandResultCode codeToSet = result ? successCode : failureCode;

            cmd.SetResultVolatile(codeToSet, failureCode);
        }

        public static void appendEventsVolatile(OrderCommand cmd,
                                                MatcherTradeEvent eventHead)
        {

            //MatcherTradeEvent.asList(eventHead).forEach(a -> log.info("in {}", a));

            cmd.AppendEventsVolatile(eventHead);
        }

        internal static unsafe CommandResultCode CompareExchange(ref CommandResultCode target, CommandResultCode value, CommandResultCode expected)
        {
            fixed (CommandResultCode* p = &target)
                return (CommandResultCode)Interlocked.CompareExchange(ref *(int*)p, (int)value, (int)expected);
        }
    }

}
