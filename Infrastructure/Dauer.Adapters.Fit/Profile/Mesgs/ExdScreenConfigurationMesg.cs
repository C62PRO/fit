#region Copyright
/////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2023 Garmin International, Inc.
// Licensed under the Flexible and Interoperable Data Transfer (FIT) Protocol License; you
// may not use this file except in compliance with the Flexible and Interoperable Data
// Transfer (FIT) Protocol License.
/////////////////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  This file is auto-generated!  Do NOT edit this file.
// Profile Version = 21.105Release
// Tag = production/release/21.105.00-0-gdc65d24
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
    /// Implements the ExdScreenConfiguration profile message.
    /// </summary>
    public class ExdScreenConfigurationMesg : Mesg
    {
        #region Fields
        #endregion

        /// <summary>
        /// Field Numbers for <see cref="ExdScreenConfigurationMesg"/>
        /// </summary>
        public sealed class FieldDefNum
        {
            public const byte ScreenIndex = 0;
            public const byte FieldCount = 1;
            public const byte Layout = 2;
            public const byte ScreenEnabled = 3;
            public const byte Invalid = Fit.FieldNumInvalid;
        }

        #region Constructors
        public ExdScreenConfigurationMesg() : base(Profile.GetMesg(MesgNum.ExdScreenConfiguration))
        {
        }

        public ExdScreenConfigurationMesg(Mesg mesg) : base(mesg)
        {
        }
        #endregion // Constructors

        #region Methods
        ///<summary>
        /// Retrieves the ScreenIndex field</summary>
        /// <returns>Returns nullable byte representing the ScreenIndex field</returns>
        public byte? GetScreenIndex()
        {
            Object val = GetFieldValue(0, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set ScreenIndex field</summary>
        /// <param name="screenIndex_">Nullable field value to be set</param>
        public void SetScreenIndex(byte? screenIndex_)
        {
            SetFieldValue(0, 0, screenIndex_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the FieldCount field
        /// Comment: number of fields in screen</summary>
        /// <returns>Returns nullable byte representing the FieldCount field</returns>
        public byte? GetFieldCount()
        {
            Object val = GetFieldValue(1, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set FieldCount field
        /// Comment: number of fields in screen</summary>
        /// <param name="fieldCount_">Nullable field value to be set</param>
        public void SetFieldCount(byte? fieldCount_)
        {
            SetFieldValue(1, 0, fieldCount_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the Layout field</summary>
        /// <returns>Returns nullable ExdLayout enum representing the Layout field</returns>
        public ExdLayout? GetLayout()
        {
            object obj = GetFieldValue(2, 0, Fit.SubfieldIndexMainField);
            ExdLayout? value = obj == null ? (ExdLayout?)null : (ExdLayout)obj;
            return value;
        }

        /// <summary>
        /// Set Layout field</summary>
        /// <param name="layout_">Nullable field value to be set</param>
        public void SetLayout(ExdLayout? layout_)
        {
            SetFieldValue(2, 0, layout_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ScreenEnabled field</summary>
        /// <returns>Returns nullable Bool enum representing the ScreenEnabled field</returns>
        public Bool? GetScreenEnabled()
        {
            object obj = GetFieldValue(3, 0, Fit.SubfieldIndexMainField);
            Bool? value = obj == null ? (Bool?)null : (Bool)obj;
            return value;
        }

        /// <summary>
        /// Set ScreenEnabled field</summary>
        /// <param name="screenEnabled_">Nullable field value to be set</param>
        public void SetScreenEnabled(Bool? screenEnabled_)
        {
            SetFieldValue(3, 0, screenEnabled_, Fit.SubfieldIndexMainField);
        }
        
        #endregion // Methods
    } // Class
} // namespace
