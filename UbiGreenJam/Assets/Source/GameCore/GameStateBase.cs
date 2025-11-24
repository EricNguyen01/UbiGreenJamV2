using UnityEngine;

namespace GameCore
{
    public abstract class GameStateBase
    {
        protected GameManager gameManager;

        protected GameStateBase(GameManager manager)
        {
            gameManager = manager;
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
    }
}
