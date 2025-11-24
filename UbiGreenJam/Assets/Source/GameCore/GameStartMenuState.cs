using UnityEngine;
using System.Collections;

namespace GameCore
{
    public class GameStartMenuState : GameStateBase
    {
        public GameStartMenuState(GameManager manager) : base(manager) {}

        public override void OnEnter()
        {
            Debug.Log("Enter Start Menu State");
            // trigger UI open event (UI should subscribe to OnStateChanged)
            // Show main menu UI...
        }
    }

    public class PreparePhaseState : GameStateBase
    {
        private float prepareTime = 5f; // example, could be from ScriptableData
        private float elapsed;

        public PreparePhaseState(GameManager manager) : base(manager) {}

        public override void OnEnter()
        {
            Debug.Log("Enter Prepare Phase");
            elapsed = 0f;

            // Reset or init phase variables
            gameManager.CurrentStorm?.Reset(); // optional
            // Notify UI to show prepare UI
        }

        public override void OnUpdate()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= prepareTime)
            {
                gameManager.StartStormPhase();
            }
        }

        public override void OnExit()
        {
            Debug.Log("Exit Prepare Phase");
        }
    }

    public class StormPhaseState : GameStateBase
    {
        public StormPhaseState(GameManager manager) : base(manager) {}

        public override void OnEnter()
        {
            Debug.Log("Enter Storm Phase");
            // Start storm runtime logic
            if (gameManager.CurrentStorm != null)
                gameManager.CurrentStorm.StartStorm();
        }

        public override void OnUpdate()
        {
            if (gameManager.CurrentStorm != null)
            {
                gameManager.CurrentStorm.Tick(Time.deltaTime);

                if (gameManager.CurrentStorm.IsFinished)
                {
                    gameManager.StartStormEndPhase();
                }
            }
        }

        public override void OnExit()
        {
            Debug.Log("Exit Storm Phase");
        }
    }

    public class StormEndPhaseState : GameStateBase
    {
        private float wrapTime = 2f;
        private float t;

        public StormEndPhaseState(GameManager manager) : base(manager) {}

        public override void OnEnter()
        {
            Debug.Log("Enter Storm End Phase");
            t = 0f;
            // finalize scores, update gameStats
        }

        public override void OnUpdate()
        {
            t += Time.deltaTime;
            if (t >= wrapTime)
            {
                // decide next state: start next prepare or end game
                // Example: continue loop
                gameManager.ChangeState(new PreparePhaseState(gameManager));
            }
        }
    }

    public class GamePauseState : GameStateBase
    {
        public GameStateBase RestoreState { get; private set; }

        public GamePauseState(GameManager manager, GameStateBase restoreFrom) : base(manager)
        {
            RestoreState = restoreFrom;
        }

        public override void OnEnter()
        {
            Debug.Log("Enter Pause");
            Time.timeScale = 0f;
            // show pause UI
        }

        public override void OnExit()
        {
            Time.timeScale = 1f;
            Debug.Log("Exit Pause");
            // hide pause UI
        }
    }

    public class GameEndState : GameStateBase
    {
        public GameEndState(GameManager manager) : base(manager) {}

        public override void OnEnter()
        {
            Debug.Log("Enter Game End");
            // show end screen, save stats, etc.
        }
    }
}
