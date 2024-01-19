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
    /// Implements the AntChannelId profile message.
    /// </summary>
    public class AntChannelIdMesg : Mesg
    {
        #region Fields
        #endregion

        /// <summary>
        /// Field Numbers for <see cref="AntChannelIdMesg"/>
        /// </summary>
        public sealed class FieldDefNum
        {
            public const byte ChannelNumber = 0;
            public const byte DeviceType = 1;
            public const byte DeviceNumber = 2;
            public const byte TransmissionType = 3;
            public const byte DeviceIndex = 4;
            public const byte Invalid = Fit.FieldNumInvalid;
        }

        #region Constructors
        public AntChannelIdMesg() : base(Profile.GetMesg(MesgNum.AntChannelId))
        {
        }

        public AntChannelIdMesg(Mesg mesg) : base(mesg)
        {
        }
        #endregion // Constructors

        #region Methods
        ///<summary>
        /// Retrieves the ChannelNumber field</summary>
        /// <returns>Returns nullable byte representing the ChannelNumber field</returns>
        public byte? GetChannelNumber()
        {
            Object val = GetFieldValue(0, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set ChannelNumber field</summary>
        /// <param name="channelNumber_">Nullable field value to be set</param>
        public void SetChannelNumber(byte? channelNumber_)
        {
            SetFieldValue(0, 0, channelNumber_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the DeviceType field</summary>
        /// <returns>Returns nullable byte representing the DeviceType field</returns>
        public byte? GetDeviceType()
        {
            Object val = GetFieldValue(1, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set DeviceType field</summary>
        /// <param name="deviceType_">Nullable field value to be set</param>
        public void SetDeviceType(byte? deviceType_)
        {
            SetFieldValue(1, 0, deviceType_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the DeviceNumber field</summary>
        /// <returns>Returns nullable ushort representing the DeviceNumber field</returns>
        public ushort? GetDeviceNumber()
        {
            Object val = GetFieldValue(2, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToUInt16(val));
            
        }

        /// <summary>
        /// Set DeviceNumber field</summary>
        /// <param name="deviceNumber_">Nullable field value to be set</param>
        public void SetDeviceNumber(ushort? deviceNumber_)
        {
            SetFieldValue(2, 0, deviceNumber_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the TransmissionType field</summary>
        /// <returns>Returns nullable byte representing the TransmissionType field</returns>
        public byte? GetTransmissionType()
        {
            Object val = GetFieldValue(3, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set TransmissionType field</summary>
        /// <param name="transmissionType_">Nullable field value to be set</param>
        public void SetTransmissionType(byte? transmissionType_)
        {
            SetFieldValue(3, 0, transmissionType_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the DeviceIndex field</summary>
        /// <returns>Returns nullable byte representing the DeviceIndex field</returns>
        public byte? GetDeviceIndex()
        {
            Object val = GetFieldValue(4, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set DeviceIndex field</summary>
        /// <param name="deviceIndex_">Nullable field value to be set</param>
        public void SetDeviceIndex(byte? deviceIndex_)
        {
            SetFieldValue(4, 0, deviceIndex_, Fit.SubfieldIndexMainField);
        }
        
        #endregion // Methods
    } // Class
} // namespace
