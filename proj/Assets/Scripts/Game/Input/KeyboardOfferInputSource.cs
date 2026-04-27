using UnityEngine.InputSystem;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class KeyboardOfferInputSource : IAttemptInputSource
    {
        public bool TryGetPreviewOfferIndex(Attempt attempt, out int offerIndex)
        {
            offerIndex = -1;
            if (attempt == null)
            {
                return false;
            }

            for (int index = 0; index < attempt.CurrentOffers.Count && index < 3; index++)
            {
                if (!WasOfferKeyPressed(index))
                {
                    continue;
                }

                offerIndex = index;
                return true;
            }

            return false;
        }

        public bool TryCreateRequest(Attempt attempt, out PlayerActionRequest request)
        {
            request = null;
            if (attempt == null)
            {
                return false;
            }

            if (WasEndTurnKeyPressed())
            {
                request = PlayerActionRequest.CreateEndTurnRequest(PlayerInputKind.Keyboard, "EndTurn");

                return true;
            }

            return false;
        }

        private static bool WasOfferKeyPressed(int index)
        {
            return index switch
            {
                0 => Keyboard.current != null &&
                    (Keyboard.current.digit1Key.wasPressedThisFrame ||
                     Keyboard.current.numpad1Key.wasPressedThisFrame),
                1 => Keyboard.current != null &&
                    (Keyboard.current.digit2Key.wasPressedThisFrame ||
                     Keyboard.current.numpad2Key.wasPressedThisFrame),
                2 => Keyboard.current != null &&
                    (Keyboard.current.digit3Key.wasPressedThisFrame ||
                     Keyboard.current.numpad3Key.wasPressedThisFrame),
                _ => false
            };
        }

        private static bool WasEndTurnKeyPressed()
        {
            return Keyboard.current != null &&
                (Keyboard.current.digit0Key.wasPressedThisFrame ||
                 Keyboard.current.numpad0Key.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.spaceKey.wasPressedThisFrame);
        }
    }
}
