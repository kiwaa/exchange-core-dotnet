using Exchange.Core.Common;
using Exchange.Core.Utils;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Tests.Utils
{
    public sealed class UserCurrencyAccountsGenerator
    {
        private static ILog log = LogManager.GetLogger(typeof(UserCurrencyAccountsGenerator));
        /**
         * Generates random users and different currencies they should have, so the total account is between
         * accountsToCreate and accountsToCreate+currencies.size()
         * <p>
         * In average each user will have account for 4 symbols (between 1 and currencies,size)
         *
         * @param accountsToCreate
         * @param currencies
         * @return n + 1 uid records with allowed currencies
         */
        public static List<BitSet> generateUsers(int accountsToCreate, IEnumerable<int> currencies)
        {
            log.Debug($"Generating users with {accountsToCreate} accounts ({currencies.Count()} currencies)...");
            ExecutionTime executionTime = new ExecutionTime();
            List<BitSet> result = new List<BitSet>();
            result.Add(new BitSet()); // uid=0 no accounts

            Random rand = new Random(1);

            ParetoDistribution paretoDistribution = new ParetoDistribution(new Random(0), 1, 1.5);
            int[] currencyCodes = currencies.ToArray();

            int totalAccountsQuota = accountsToCreate;
            do
            {
                // TODO prefer some currencies more
                int accountsToOpen = Math.Min(Math.Min(1 + (int)paretoDistribution.Sample(), currencyCodes.Length), totalAccountsQuota);
                BitSet bitSet = new BitSet();
                do
                {
                    int currencyCode = currencyCodes[rand.Next(currencyCodes.Length)];
                    bitSet.Set(currencyCode);
                } while (bitSet.Cardinality() != accountsToOpen);

                totalAccountsQuota -= accountsToOpen;
                result.Add(bitSet);

                //            log.debug("{}", bitSet);

            } while (totalAccountsQuota > 0);

            log.Debug($"Generated {result.Count} users with {accountsToCreate} accounts up to {currencies.Count()} different currencies in {executionTime.getTimeFormatted()}");
            return result;
        }

        public static int[] createUserListForSymbol(List<BitSet> users2currencies, CoreSymbolSpecification spec, int symbolMessagesExpected)
        {

            // we would prefer to choose from same number of users as number of messages to be generated in tests
            // at least 2 users are required, but not more than half of all users provided
            int numUsersToSelect = Math.Min(users2currencies.Count, Math.Max(2, symbolMessagesExpected / 5));

            List<int> uids = new List<int>();
            Random rand = new Random(spec.SymbolId);
            int uid = 1 + rand.Next(users2currencies.Count - 1);
            int c = 0;
            do
            {
                BitSet accounts = users2currencies[uid];
                if (accounts.Get(spec.QuoteCurrency) && (spec.Type == SymbolType.FUTURES_CONTRACT || accounts.Get(spec.BaseCurrency)))
                {
                    uids.Add(uid);
                }
                if (++uid == users2currencies.Count)
                {
                    uid = 1;
                }
                //uid = 1 + rand.nextInt(users2currencies.size() - 1);

                c++;
            } while (uids.Count < numUsersToSelect && c < users2currencies.Count);

            //        int expectedUsers = symbolMessagesExpected / 20000;
            //        if (uids.size() < Math.max(2, expectedUsers)) {
            //            // less than 2 uids
            //            throw new IllegalStateException("Insufficient accounts density - can not find more than " + uids.size() + " matching users for symbol " + spec.symbolId
            //                    + " total users:" + users2currencies.size()
            //                    + " symbolMessagesExpected=" + symbolMessagesExpected
            //                    + " numUsersToSelect=" + numUsersToSelect);
            //        }

            //        log.debug("sym: " + spec.symbolId + " " + spec.type + " uids:" + uids.size() + " msg=" + symbolMessagesExpected + " numUsersToSelect=" + numUsersToSelect + " c=" + c);

            return uids.ToArray();
        }


    }
}