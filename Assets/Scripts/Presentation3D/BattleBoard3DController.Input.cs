using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RunicTower.Combat;
using RunicTower.Core;

namespace RunicTower.Presentation3D
{
    public sealed partial class BattleBoard3DController
    {
        private void HandleBoardInput()
        {
            if (ActiveCamera == null)
            {
                return;
            }

            if (!TryGetTapRay(ActiveCamera, out Ray ray, out int pointerId))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, interactionLayers);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            if (ritualPreviewBook != null)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (ritualPreviewBook.MatchesCollider(hit.collider))
                    {
                        ToggleRitualPreviewBook();
                        return;
                    }
                }
            }

            if (runeCardPrefab == null)
            {
                return;
            }

            if (IsPreviewBookBlockingInteraction)
            {
                return;
            }

            foreach (RaycastHit hit in hits)
            {
                RuneCard3D card = hit.collider.GetComponentInParent<RuneCard3D>();
                if (card == null)
                {
                    continue;
                }

                HandleCardTapped(card);
                return;
            }
        }

        private static bool TryGetTapRay(Camera camera, out Ray ray, out int pointerId)
        {
            ray = default;
            pointerId = -1;

            if (camera == null)
            {
                return false;
            }

            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch != null && primaryTouch.press.wasPressedThisFrame)
                {
                    pointerId = primaryTouch.touchId.ReadValue();
                    ray = camera.ScreenPointToRay(primaryTouch.position.ReadValue());
                    return true;
                }
            }

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return false;
            }

            ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return true;
        }

        private void HandleCardTapped(RuneCard3D card)
        {
            if (card == null || card.Owner != BoardCardOwner.Player)
            {
                return;
            }

            if (IsCardInFlight(card))
            {
                return;
            }

            if (battleController?.CurrentState?.Phase != BattlePhase.PlayerTurn || IsCastSequenceRunning)
            {
                return;
            }

            if (card.IsModifier)
            {
                if (card.Location == BoardCardLocation.Hand)
                {
                    if (_selectedModifier?.Definition != null || ritualPedestal?.GetModifierSpot() == null)
                    {
                        return;
                    }

                    StartCoroutine(AnimateModifierToRitual(card, card.BoundModifier));
                }
                else
                {
                    StartCoroutine(AnimateModifierToHand(card, card.BoundModifier));
                }

                return;
            }

            if (card.BoundRune?.Definition == null)
            {
                return;
            }

            TriggerRunePressAnimation();

            if (card.Location == BoardCardLocation.Hand)
            {
                int targetSlot = FindFirstEmptyRitualSlot();
                if (targetSlot < 0)
                {
                    return;
                }

                StartCoroutine(AnimateRuneToRitual(card, card.BoundRune, targetSlot));
            }
            else
            {
                int sourceSlot = FindSelectedRuneIndex(card.BoundRune);
                if (sourceSlot < 0)
                {
                    return;
                }

                StartCoroutine(AnimateRuneToHand(card, card.BoundRune, sourceSlot));
            }
        }

        private void HandleRoundLabelDoubleTap()
        {
            if (!allowRoundLabelDoubleTapRestart || roundLabel == null || battleController == null)
            {
                return;
            }

            if (!TryGetTapScreenPosition(out Vector2 screenPosition))
            {
                return;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    roundLabel.rectTransform,
                    screenPosition,
                    null))
            {
                return;
            }

            float currentTime = Time.unscaledTime;
            if (currentTime - _lastRoundLabelTapTime <= restartDoubleTapWindow)
            {
                RestartBattleForTesting();
                _lastRoundLabelTapTime = -10f;
                return;
            }

            _lastRoundLabelTapTime = currentTime;
        }

        private static bool TryGetTapScreenPosition(out Vector2 screenPosition)
        {
            screenPosition = default;

            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch != null && primaryTouch.press.wasPressedThisFrame)
                {
                    screenPosition = primaryTouch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}
