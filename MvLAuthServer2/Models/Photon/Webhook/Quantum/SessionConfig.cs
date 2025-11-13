using System;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class SessionConfig
    {
        public int PlayerCount;
        public bool ChecksumCrossPlatformDeterminism;
        public bool LockstepSimulation;
        public bool InputDeltaCompression;
        public int UpdateFPS;
        public int ChecksumInterval;
        public int RollbackWindow;
        public int InputHardTolerance;
        public int InputRedundancy;
        public int InputRepeatMaxDistance;
        public int SessionStartTimeout;
        public int TimeCorrectionRate;
        public int MinTimecorrectionFrames;
        public int MinOffsetCorrectionDiff;
        public int TimeScaleMin;
        public int TimeScalePingMin;
        public int TimeScalePingMax;
        public int InputDelayMin;
        public int InputDelayMax;
        public int InputDelayPingStart;
        public int InputFixedSize;
    }
}