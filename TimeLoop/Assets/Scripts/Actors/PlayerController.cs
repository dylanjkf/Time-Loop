using TimeLoop.Grid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeLoop.Actors
{
    /// <summary>
    /// The live, player-controlled actor. Reads swipes/taps (touch) and arrow keys/WASD
    /// (editor/desktop) every render frame into a LiveInputProvider buffer; TimeLoopManager
    /// consumes exactly one buffered command per simulation tick and feeds it through
    /// GridWorld.Step alongside every active ghost. See docs/TECHNICAL_ARCHITECTURE.md section 4.2.
    /// </summary>
    [RequireComponent(typeof(ActorVisual))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _swipeThresholdPixels = 40f;

        public GridActor Actor { get; private set; }
        public LiveInputProvider InputProvider { get; private set; }

        private ActorVisual _visual;
        private Vector2 _touchStartScreenPos;
        private bool _touchActive;

        public void Initialize(string id, GridCoord spawnPosition)
        {
            Actor = new GridActor(id, spawnPosition);
            InputProvider = new LiveInputProvider();
            _visual = GetComponent<ActorVisual>();
            _visual.Bind(Actor);
        }

        /// <summary>Called by TimeLoopManager when a new loop begins; the player always returns to the spawn tile.</summary>
        public void ResetForNewLoop()
        {
            Actor.ResetToSpawn();
            _visual.Bind(Actor);
        }

        private void Update()
        {
            if (Actor == null) return;

            PollKeyboard();
            PollTouch();
            _visual.Tick(Time.deltaTime);
        }

        private void PollKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            {
                InputProvider.QueueDirection(Direction.Up);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            {
                InputProvider.QueueDirection(Direction.Down);
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            {
                InputProvider.QueueDirection(Direction.Left);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            {
                InputProvider.QueueDirection(Direction.Right);
            }

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                InputProvider.QueueInteract();
            }
        }

        private void PollTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return;

            var primaryTouch = touchscreen.primaryTouch;

            if (primaryTouch.press.wasPressedThisFrame)
            {
                _touchStartScreenPos = primaryTouch.position.ReadValue();
                _touchActive = true;
            }
            else if (_touchActive && primaryTouch.press.wasReleasedThisFrame)
            {
                _touchActive = false;
                var endPos = primaryTouch.position.ReadValue();
                var delta = endPos - _touchStartScreenPos;

                if (delta.magnitude < _swipeThresholdPixels)
                {
                    InputProvider.QueueInteract();
                    return;
                }

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    InputProvider.QueueDirection(delta.x > 0 ? Direction.Right : Direction.Left);
                }
                else
                {
                    InputProvider.QueueDirection(delta.y > 0 ? Direction.Up : Direction.Down);
                }
            }
        }

        /// <summary>Explicit direction entry point for the on-screen D-pad / accessibility controls.</summary>
        public void OnDirectionButtonPressed(Direction direction) => InputProvider.QueueDirection(direction);

        /// <summary>Explicit entry point for the on-screen Interact button.</summary>
        public void OnInteractButtonPressed() => InputProvider.QueueInteract();
    }
}
