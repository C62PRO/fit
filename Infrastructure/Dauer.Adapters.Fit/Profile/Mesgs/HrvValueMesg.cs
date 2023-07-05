#region Copyright
/////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2023 Garmin International, Inc.
// Licensed under the Flexible and Interoperable Data Transfer (FIT) Protocol License; you
// may not use this file except in compliance with the Flexible and Interoperable Data
// Transfer (FIT) Protocol License.
/////////////////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  This file is auto-generated!  Do NOT edit this file.
// Profile Version = 21.115Release
// Tag = production/release/21.115.00-0-gfe0a7f8
/////////////////////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;

namespace Dynastream.Fit
{
    /// <summary>
    /// Implements the HrvValue profile message.
    /// </summary>
    public class HrvValueMesg : Mesg
    {
        #region Fields
        #endregion

        /// <summary>
        /// Field Numbers for <see cref="HrvValueMesg"/>
        /// </summary>
        public sealed class FieldDefNum
        {
            public const byte Timestamp = 253;
            public const byte Value = 0;
            public const byte Invalid = Fit.FieldNumInvalid;
        }

        #region Constructors
        public HrvValueMesg() : base(Profile.GetMesg(MesgNum.HrvValue))
        {
        }

        public HrvValueMesg(Mesg mesg) : base(mesg)
        {
        }
        #endregion // Constructors

        #region Methods
        ///<summary>
        /// Retrieves the Timestamp field</summary>
        /// <returns>Returns DateTime representing the Timestamp field</returns>
        public DateTime GetTimestamp()
        {
            Object val = GetFieldValue(253, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return TimestampToDateTime(Convert.ToUInt32(val));
            
        }

        /// <summary>
        /// Set Timestamp field</summary>
        /// <param name="timestamp_">Nullable field value to be set</param>
        public void SetTimestamp(DateTime timestamp_)
        {
            SetFieldValue(253, 0, timestamp_.GetTimeStamp(), Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the Value field
        /// Units: ms
        /// Comment: 5 minute RMSSD</summary>
        /// <returns>Returns nullable float representing the Value field</returns>
        public float? GetValue()
        {
            Object val = GetFieldValue(0, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToSingle(val));
            
        }

        /// <summary>
        /// Set Value field
        /// Units: ms
        /// Comment: 5 minute RMSSD</summary>
        /// <param name="value_">Nullable field value to be set</param>
        public void SetValue(float? value_)
        {
            SetFieldValue(0, 0, value_, Fit.SubfieldIndexMainField);
        }
        
        #endregion // Methods
    } // Class
} // namespace
