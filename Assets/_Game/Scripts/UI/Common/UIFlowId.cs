#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace UI.Common
{
    public abstract class UIFlowId
    {
        public abstract class WinFlow
        {
            public const int Default = 0;
            public const int ShowLeaderboard = 1;

#if UNITY_EDITOR
            public static ValueDropdownList<int> GetDropdownOptions()
            {
                return new ValueDropdownList<int>
                {
                    { "Default", Default },
                    { "Show Leaderboard", ShowLeaderboard },
                };
            }
#endif
        }
    }
}