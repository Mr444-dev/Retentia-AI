using System;

namespace Retentia.Data
{
    [Serializable]
    public struct UserProfile
    {
        public string UserId;
        public int TotalSessions;
        public float AvgSessionDuration; // in minutes
        public int DaysSinceLastLogin;
        public float TotalSpent;
        public int SupportTicketsOpen;
        public int AdsWatched;
        public int LevelsCompleted;
        public int SocialShares;
        public int WinRatePercent;
        public int PlatformId; // 0: Android, 1: iOS

        // ML-related fields
        public float ChurnProbability;
        public float DifficultyScore;
        public float LatencyAvgMs;
        public int PlayerSegmentId;

        // Returns normalized array for AI (order matters!)
        public float[] ToTensorArray()
        {
            return new float[] {
                TotalSessions, AvgSessionDuration, DaysSinceLastLogin, TotalSpent,
                SupportTicketsOpen, AdsWatched, LevelsCompleted, SocialShares,
                WinRatePercent, PlatformId
            };
        }

        // Returns extended array with all features
        public float[] ToExtendedTensorArray()
        {
            return new float[] {
                TotalSessions, AvgSessionDuration, DaysSinceLastLogin, TotalSpent,
                SupportTicketsOpen, AdsWatched, LevelsCompleted, SocialShares,
                WinRatePercent, PlatformId, DifficultyScore, LatencyAvgMs
            };
        }
    }
}