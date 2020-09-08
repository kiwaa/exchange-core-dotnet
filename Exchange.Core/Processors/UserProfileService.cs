using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Utils;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    /**
     * Stateful (!) User profile service
     * <p>
     * TODO make multi instance
     */
    public sealed class UserProfileService : IWriteBytesMarshallable, IStateHash
    {
        private static ILog log = LogManager.GetLogger(typeof(BinaryCommandsProcessor));

        /*
         * State: uid to UserProfile
         */
        public Dictionary<long, UserProfile> userProfiles { get; }

        public UserProfileService()
        {
            this.userProfiles = new Dictionary<long, UserProfile>(1024);
        }

        public UserProfileService(IBytesIn bytes)
        {
            this.userProfiles = SerializationUtils.readLongHashMap(bytes, bytesIn => new UserProfile(bytesIn));
        }

        /**
         * Find user profile
         *
         * @param uid uid
         * @return user profile
         */
        public UserProfile getUserProfile(long uid)
        {
            if (userProfiles.TryGetValue(uid, out UserProfile profile))
                return profile;
            return null;
        }

        public UserProfile getUserProfileOrAddSuspended(long uid)
        {
            if (userProfiles.TryGetValue(uid, out UserProfile profile))
                return profile;
            profile = new UserProfile(uid, UserStatus.SUSPENDED);
            userProfiles.Add(uid, profile);
            return profile;
        }


        /**
         * Perform balance adjustment for specific user
         *
         * @param uid                  uid
         * @param currency             account currency
         * @param amount               balance difference
         * @param fundingTransactionId transaction id (should increment only)
         * @return result code
         */
        public CommandResultCode balanceAdjustment(long uid, int currency, long amount, long fundingTransactionId)
        {

            UserProfile userProfile = getUserProfile(uid);
            if (userProfile == null)
            {
                log.Warn($"User profile {uid} not found");
                return CommandResultCode.AUTH_INVALID_USER;
            }

            //        if (amount == 0) {
            //            return CommandResultCode.USER_MGMT_ACCOUNT_BALANCE_ADJUSTMENT_ZERO;
            //        }

            // double settlement protection
            if (userProfile.adjustmentsCounter == fundingTransactionId)
            {
                return CommandResultCode.USER_MGMT_ACCOUNT_BALANCE_ADJUSTMENT_ALREADY_APPLIED_SAME;
            }
            if (userProfile.adjustmentsCounter > fundingTransactionId)
            {
                return CommandResultCode.USER_MGMT_ACCOUNT_BALANCE_ADJUSTMENT_ALREADY_APPLIED_MANY;
            }

            // validate balance for withdrawals
            if (amount < 0 && (userProfile.accounts[currency] + amount < 0))
            {
                return CommandResultCode.USER_MGMT_ACCOUNT_BALANCE_ADJUSTMENT_NSF;
            }

            userProfile.adjustmentsCounter = fundingTransactionId;
            userProfile.accounts.AddValue(currency, amount);

            //log.debug("FUND: {}", userProfile);
            return CommandResultCode.SUCCESS;
        }

        /**
         * Create a new user profile with known unique uid
         *
         * @param uid uid
         * @return true if user was added
         */
        public bool addEmptyUserProfile(long uid)
        {
            if (!userProfiles.TryGetValue(uid, out UserProfile profile))
//            if (userProfiles.get(uid) == null)
            {
                userProfiles[uid] = new UserProfile(uid, UserStatus.ACTIVE);
                return true;
            }
            else
            {
                log.Debug($"Can not add user, already exists: {uid}");
                return false;
            }
        }

        /**
         * Suspend removes inactive clients profile from the core in order to increase performance.
         * Account balances should be first adjusted to zero with BalanceAdjustmentType=SUSPEND.
         * No open margin positions allowed in the suspended profile.
         * However in some cases profile can come back with positions and non-zero balances,
         * if pending orders or pending commands was not processed yet.
         * Therefore resume operation must be able to merge profile.
         *
         * @param uid client id
         * @return result code
         */
        public CommandResultCode suspendUserProfile(long uid)
        {
            if (!userProfiles.TryGetValue(uid, out UserProfile userProfile))
//          if (userProfile == null)
            {
                return CommandResultCode.USER_MGMT_USER_NOT_FOUND;

            }
            else if (userProfile.userStatus == UserStatus.SUSPENDED)
            {
                return CommandResultCode.USER_MGMT_USER_ALREADY_SUSPENDED;

            }
            else if (userProfile.positions.Any(pos=> !pos.Value.isEmpty()))
            {
                return CommandResultCode.USER_MGMT_USER_NOT_SUSPENDABLE_HAS_POSITIONS;

            }
            else if (userProfile.accounts.Any(acc=> acc.Value != 0))
            {
                return CommandResultCode.USER_MGMT_USER_NOT_SUSPENDABLE_NON_EMPTY_ACCOUNTS;

            }
            else
            {
                log.Debug($"Suspended user profile: {userProfile}");
                userProfiles.Remove(uid);
                // TODO pool UserProfile objects
                return CommandResultCode.SUCCESS;
            }
        }

        public CommandResultCode resumeUserProfile(long uid)
        {
            if (!userProfiles.TryGetValue(uid, out UserProfile userProfile))
//          UserProfile userProfile = userProfiles.get(uid);
//          if (userProfile == null)
            {
                // create new empty user profile
                // account balance adjustments should be applied later
                userProfiles[uid] = new UserProfile(uid, UserStatus.ACTIVE);
                return CommandResultCode.SUCCESS;
            }
            else if (userProfile.userStatus != UserStatus.SUSPENDED)
            {
                // attempt to resume non-suspended account (or resume twice)
                return CommandResultCode.USER_MGMT_USER_NOT_SUSPENDED;
            }
            else
            {
                // resume existing suspended profile (can contain non empty positions or accounts)
                userProfile.userStatus = UserStatus.ACTIVE;
                log.Debug($"Resumed user profile: {userProfile}");
                return CommandResultCode.SUCCESS;
            }
        }

        /**
         * Reset module - for testing only
         */
        public void reset()
        {
            userProfiles.Clear();
        }

        public void writeMarshallable(IBytesOut bytes)
        {

            // write symbolSpecs
            SerializationUtils.marshallLongHashMap(userProfiles, bytes);
        }

        public int stateHash()
        {
            return HashingUtils.stateHash(userProfiles);
        }

    }
}
