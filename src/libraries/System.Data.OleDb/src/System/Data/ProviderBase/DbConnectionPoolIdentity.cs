// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Runtime.Versioning;

namespace System.Data.ProviderBase
{
    internal sealed class DbConnectionPoolIdentity
    {
        private const int E_NotImpersonationToken = unchecked((int)0x8007051D);
        private const int Win32_CheckTokenMembership = 1;
        private const int Win32_CreateWellKnownSid = 5;

        public static readonly DbConnectionPoolIdentity NoIdentity = new DbConnectionPoolIdentity(string.Empty, false, true);

        private readonly string _sidString;
        private readonly bool _isRestricted;
        private readonly bool _isNetwork;
        private readonly int _hashCode;

        private DbConnectionPoolIdentity(string sidString, bool isRestricted, bool isNetwork)
        {
            _sidString = sidString;
            _isRestricted = isRestricted;
            _isNetwork = isNetwork;
            _hashCode = sidString == null ? 0 : sidString.GetHashCode();
        }

        internal bool IsRestricted
        {
            get { return _isRestricted; }
        }

        private static byte[] CreateWellKnownSid(WellKnownSidType sidType)
        {
            // Passing an array as big as it can ever be is a small price to pay for
            // not having to P/Invoke twice (once to get the buffer, once to get the data)

            uint length = (uint)SecurityIdentifier.MaxBinaryLength;

            // NOTE - We copied this code from System.Security.Principal.Win32.CreateWellKnownSid...

            if (0 == UnsafeNativeMethods.CreateWellKnownSid((int)sidType, null, out byte[] resultSid, ref length))
            {
                IntegratedSecurityError(Win32_CreateWellKnownSid);
            }
            return resultSid;
        }

        public override bool Equals(object? value)
        {
            bool result = ((this == NoIdentity) || (this == value));
            if (!result && (null != value))
            {
                DbConnectionPoolIdentity that = ((DbConnectionPoolIdentity)value);
                result = ((_sidString == that._sidString) && (_isRestricted == that._isRestricted) && (_isNetwork == that._isNetwork));
            }
            return result;
        }

        internal static DbConnectionPoolIdentity GetCurrent()
        {
            throw new PlatformNotSupportedException();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private static void IntegratedSecurityError(int caller)
        {
            // passing 1,2,3,4,5 instead of true/false so that with a debugger
            // we could determine more easily which Win32 method call failed
            int lastError = Marshal.GetHRForLastWin32Error();
            if ((Win32_CheckTokenMembership != caller) || (E_NotImpersonationToken != lastError))
            {
                Marshal.ThrowExceptionForHR(lastError); // will only throw if (hresult < 0)
            }
        }

    }
}
