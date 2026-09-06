using System;
using System.Collections.Generic;
using _Game.Scripts.Common;
using AssetsHolder;
using Design;
using Titipi.MocaLib.Runtime.Services;

namespace Game.Leaderboard
{
    public class LeaderboardMangerHelper
    {
        private const int EstimatedLevelsPerDay = 20;
        private const int MonthsPerYear = 12;
        private static readonly DateTime DailyChallengeStartDate = new(2026, 1, 1);

        private static readonly string[] FakeFirstNames =
        {
            // Nature & Elements
            "Ash", "River", "Storm", "Blaze", "Frost", "Ember", "Gale", "Flint",
            "Reed", "Cliff", "Brook", "Glen", "Vale", "Heath", "Lark", "Fern",
            // Celestial
            "Nova", "Luna", "Sol", "Orion", "Lyra", "Vega", "Atlas", "Aurora",
            "Comet", "Draco", "Phoebe", "Ciel", "Stella", "Zenith", "Castor", "Rigel",
            // Short & punchy
            "Kai", "Ace", "Rex", "Zoe", "Mia", "Leo", "Eva", "Nox",
            "Ivy", "Jay", "Sky", "Ray", "Max", "Ada", "Eli", "Nia",
            // Classic unisex
            "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Quinn", "Avery",
            "Blake", "Drew", "Jamie", "Skyler", "Reese", "Logan", "Peyton", "Sage",
            // Modern
            "Kai", "Zara", "Milo", "Ezra", "Nora", "Ivan", "Lena", "Seth",
            "Aria", "Iris", "Finn", "Vera", "Jace", "Ruby", "Theo", "Elsa",
            // Mythic
            "Rune", "Thor", "Odin", "Freya", "Circe", "Hades", "Clio", "Ajax",
            "Dion", "Echo", "Hera", "Ares", "Nike", "Zephyr", "Hermes", "Iris"
        };

        private static readonly string[] FakeLastNames =
        {
            // Nature
            "Storm", "Frost", "Ash", "Blaze", "Stone", "Reed", "Glen", "Vale",
            "Shore", "Peak", "Crest", "Marsh", "Thorn", "Moor", "Cliff", "Dale",
            // Animals
            "Fox", "Wolf", "Hawk", "Raven", "Drake", "Lynx", "Crane", "Hare",
            "Viper", "Bison", "Falcon", "Colt", "Wren", "Bear", "Finch", "Stag",
            // Traits
            "Steel", "Swift", "Bold", "Bright", "Sharp", "Keen", "Fierce", "Brave",
            "Dusk", "Dawn", "Grit", "Iron", "Jade", "Onyx", "Flint", "Ember",
            // Short surnames
            "Ray", "Cole", "Kane", "Wade", "Knox", "Gage", "Troy", "Hale",
            "Lane", "Ford", "Beck", "Wick", "Cruz", "Vega", "Moon", "King",
            // Fantasy
            "Knight", "Hunter", "Archer", "Ranger", "Blade", "Shadow", "Phantom", "Rogue",
            "Cipher", "Wraith", "Specter", "Titan", "Voidwalker", "Sentinel", "Warden", "Nomad",
            // Celestial
            "Starr", "Solaris", "Lunaire", "Ashford", "Stormcroft", "Nightfall", "Dawnridge", "Skyborn",
            "Ironveil", "Coldwater", "Blackwood", "Whitmore", "Greywood", "Redford", "Goldwyn", "Silverstone"
        };

        public static string GenerateFakeDisplayName(string leaderboardId, int targetRank)
        {
            var seed = HashCode.Combine(leaderboardId, targetRank, "name");
            var random = new Random(seed);
            var firstName = FakeFirstNames[random.Next(FakeFirstNames.Length)];
            var lastName = FakeLastNames[random.Next(FakeLastNames.Length)];
            var number = random.Next(10, 9999);

            return random.Next(20) switch
            {
                0  => $"{firstName}{lastName}",                          // AlexStorm
                1  => $"{firstName}_{lastName}",                         // Alex_Storm
                2  => $"{firstName}{number % 1000}",                     // Alex847
                3  => $"{firstName}.{lastName}",                         // Alex.Storm
                4  => $"{lastName}{firstName}",                          // StormAlex
                5  => $"{firstName}{lastName}{number % 100}",            // AlexStorm42
                6  => $"x{firstName}{lastName}x",                        // xAlexStormx
                7  => $"{firstName}_{number % 100}",                     // Alex_42
                8  => $"{lastName}_{firstName}{number % 10}",            // Storm_Alex7
                9  => $"{firstName.ToLower()}{lastName}",                // alexStorm
                10 => $"{firstName}{lastName.ToLower()}",                // Alexstorm
                11 => $"{firstName[0]}{lastName}{number % 100}",         // AStorm42
                12 => $"{firstName}{lastName[0]}{number % 1000}",        // AlexS847
                13 => $"_{firstName}{lastName}_",                        // _AlexStorm_
                14 => $"{lastName}{number % 100}{firstName}",            // Storm42Alex
                15 => $"{firstName.ToLower()}{number % 10000}",          // alex2847
                16 => $"{firstName}{lastName}.{number % 100}",           // AlexStorm.42
                17 => $"{lastName}.{firstName}",                         // Storm.Alex
                18 => $"{firstName[0]}{lastName[0]}{number}",            // AS2847
                19 => $"{firstName}{lastName}_{number % 1000}",          // AlexStorm_847
                _  => $"{firstName}{lastName}"
            };
        }

        public static LeaderboardEntry CreateFakeEntryAroundRank(
            string leaderboardId,
            string playerId,
            int userScore,
            int targetRank,
            int userRank,
            PlayerProfile currentProfile,
            bool isUser)
        {
            if (isUser)
            {
                var userEntry = new LeaderboardEntry
                {
                    UserId = playerId,
                    Score = userScore,
                    DisplayName = currentProfile?.DisplayName ?? "You",
                    Avatar = currentProfile?.Avatar,
                    Metadata = new()
                };
                if (currentProfile?.Metadata != null)
                {
                    foreach (var item in currentProfile.Metadata)
                        userEntry.Metadata[item.Key] = item.Value;
                }
                return userEntry;
            }

            var rankDistance = targetRank - userRank; // negative = above, positive = below
            var seed = HashCode.Combine(leaderboardId, playerId, userRank, targetRank);
            var random = new Random(seed);

            var baseStep = Math.Max(1, userScore / 100);
            var maxJitter = Math.Max(1, baseStep / 2);
            var jitter = random.Next(0, maxJitter);

            var distance = Math.Abs(rankDistance);
            var scoreDelta = distance * baseStep + jitter;

            var fakeScore = rankDistance < 0
                ? userScore + scoreDelta          // above user => higher score
                : Math.Max(1, userScore - scoreDelta); // below user => lower score

            var avatarCount = ResourcesHolder.Instance?.AvatarIcons?.TotalSprites ?? 0;
            var avatarId = random.Next(0, avatarCount);
            var avatarFrameCount = ResourcesHolder.Instance?.AvatarFrames?.TotalSprites ?? 0;
            var profileBannerCount = ResourcesHolder.Instance?.ProfileBanners?.TotalSprites ?? 0;
            var estimatedPlayedDays = Math.Max(0, fakeScore) / EstimatedLevelsPerDay;

            var eligibleAvatarFrameIds = GetEligibleAvatarFrameIds(estimatedPlayedDays, avatarFrameCount);
            var avatarFrameId = eligibleAvatarFrameIds[random.Next(eligibleAvatarFrameIds.Count)];

            var eligibleProfileBannerCount = GetEligibleProfileBannerCount(profileBannerCount);
            var profileBannerId = random.Next(0, eligibleProfileBannerCount);

            return new LeaderboardEntry
            {
                UserId = $"sim_{leaderboardId}_{targetRank}",
                Score = fakeScore,
                DisplayName = GenerateFakeDisplayName(leaderboardId, targetRank),
                Avatar = $"local:{avatarId}",
                Metadata = new()
                {
                    {UserProfileKey.AvatarFrame, avatarFrameId},
                    {UserProfileKey.ProfileBannerImage, profileBannerId}
                }
            };
        }

        private static List<int> GetEligibleAvatarFrameIds(int estimatedPlayedDays, int totalFrameCount)
        {
            var eligibleIds = new List<int> { 0 };
            for (var i = 1; i < totalFrameCount; i++)
            {
                eligibleIds.Add(i);
            }

            return eligibleIds;
        }

        private static int GetEligibleProfileBannerCount(int totalBannerCount)
        {
            return Math.Max(1, totalBannerCount);
        }
    }
}
