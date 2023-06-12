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

namespace Dynastream.Fit
{
    /// <summary>
    /// Implements the profile SplitType type as an enum
    /// </summary>
    public enum SplitType : byte
    {
        AscentSplit = 1,
        DescentSplit = 2,
        IntervalActive = 3,
        IntervalRest = 4,
        IntervalWarmup = 5,
        IntervalCooldown = 6,
        IntervalRecovery = 7,
        IntervalOther = 8,
        ClimbActive = 9,
        ClimbRest = 10,
        SurfActive = 11,
        RunActive = 12,
        RunRest = 13,
        WorkoutRound = 14,
        RwdRun = 17,
        RwdWalk = 18,
        WindsurfActive = 21,
        RwdStand = 22,
        Transition = 23,
        SkiLiftSplit = 28,
        SkiRunSplit = 29,
        Invalid = 0xFF


    }
}

