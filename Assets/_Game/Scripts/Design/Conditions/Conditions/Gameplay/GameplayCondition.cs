using Gameplay;

namespace Design.Conditions.Gameplay
{
    public abstract class GameplayCondition : Condition
    {
        public GameMode GameMode;

        protected GameplayCondition()
        {

        }

        protected GameplayCondition(GameMode gameMode)
        {
            GameMode = gameMode;
        }
    }
}