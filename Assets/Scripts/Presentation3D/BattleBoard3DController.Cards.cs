using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RunicTower.Data.Runtime;

namespace RunicTower.Presentation3D
{
    public sealed partial class BattleBoard3DController
    {
        private sealed class DesiredCardState
        {
            public RuneInstance Rune;
            public ModifierInstance Modifier;
            public BoardCardOwner Owner;
            public BoardCardLocation Location;
            public bool Selected;
            public bool Interactable;
            public Transform Target;
            public bool IsModifier => Modifier?.Definition != null;
            public string InstanceId => IsModifier ? Modifier.InstanceId : Rune?.InstanceId;
        }

        private void RebuildCards(BattleState state)
        {
            if (state == null || runeCardPrefab == null)
            {
                DestroySpawnedCards();
                return;
            }

            List<DesiredCardState> desiredCards = BuildDesiredCards(state);
            List<RuneCard3D> availableCards = _spawnedCards.Where(card => card != null).ToList();
            List<RuneCard3D> syncedCards = new(desiredCards.Count);

            foreach (DesiredCardState desired in desiredCards)
            {
                RuneCard3D card = AcquireReusableCard(availableCards, desired) ??
                                  Instantiate(runeCardPrefab, cardRoot != null ? cardRoot : transform);
                bool animateUpcomingToHand = card != null &&
                                             !desired.IsModifier &&
                                             desired.Location == BoardCardLocation.Hand &&
                                             card.Location == BoardCardLocation.Upcoming &&
                                             card.BoundRune?.InstanceId == desired.InstanceId;

                if (desired.IsModifier)
                {
                    card.BindModifier(desired.Modifier, desired.Owner, desired.Location, desired.Selected, desired.Interactable);
                }
                else
                {
                    card.BindRune(desired.Rune, desired.Owner, desired.Location, desired.Selected, desired.Interactable);
                }

                ApplyCardPresentationSettings(card);
                card.SetSelected(desired.Selected);
                card.SetInteractable(desired.Interactable && !IsCardInFlight(card));
                if (animateUpcomingToHand)
                {
                    StartCoroutine(AnimateUpcomingRuneToHand(card, desired.Target));
                }
                else
                {
                    card.SetPose(desired.Target);
                }

                syncedCards.Add(card);
            }

            foreach (RuneCard3D leftover in availableCards)
            {
                if (leftover != null)
                {
                    Destroy(leftover.gameObject);
                }
            }

            _spawnedCards.Clear();
            _spawnedCards.AddRange(syncedCards);
        }

        private List<DesiredCardState> BuildDesiredCards(BattleState state)
        {
            List<DesiredCardState> desiredCards = new();
            AddPlayerHandDesiredCards(desiredCards, state.PlayerHand?.AvailableRunes);
            AddPlayerUpcomingDesiredCards(desiredCards, state.PlayerHand?.UpcomingRunes);
            AddHandDesiredCards(desiredCards, state.EnemyHand?.AvailableRunes, enemyHandElementSpots, BoardCardOwner.Enemy, false);

            if (_selectedModifier?.Definition == null)
            {
                AddModifierDesiredCard(
                    desiredCards,
                    state.PlayerHand?.ActiveModifier,
                    playerModifierSpot,
                    BoardCardOwner.Player,
                    BoardCardLocation.Hand,
                    false,
                    true);
            }

            AddModifierDesiredCard(
                desiredCards,
                state.EnemyHand?.ActiveModifier,
                enemyModifierSpot,
                BoardCardOwner.Enemy,
                BoardCardLocation.Hand,
                false,
                false);

            AddSelectedRuneDesiredCards(desiredCards);
            AddSelectedModifierDesiredCard(desiredCards);
            return desiredCards;
        }

        private RuneCard3D AcquireReusableCard(List<RuneCard3D> availableCards, DesiredCardState desired)
        {
            for (int index = 0; index < availableCards.Count; index++)
            {
                RuneCard3D card = availableCards[index];
                if (card == null)
                {
                    availableCards.RemoveAt(index);
                    index--;
                    continue;
                }

                bool sameIdentity = card.Owner == desired.Owner &&
                                    card.IsModifier == desired.IsModifier &&
                                    ((desired.IsModifier && card.BoundModifier?.InstanceId == desired.InstanceId) ||
                                     (!desired.IsModifier && card.BoundRune?.InstanceId == desired.InstanceId));
                if (!sameIdentity)
                {
                    continue;
                }

                availableCards.RemoveAt(index);
                return card;
            }

            if (availableCards.Count == 0)
            {
                return null;
            }

            RuneCard3D fallback = availableCards[0];
            availableCards.RemoveAt(0);
            return fallback;
        }

        private void AddPlayerHandDesiredCards(List<DesiredCardState> desiredCards, IReadOnlyList<RuneInstance> runes)
        {
            if (runes == null || playerHandElementSpots == null)
            {
                return;
            }

            HashSet<string> selectedRuneIds = _selectedRunes
                .Where(rune => rune?.Definition != null)
                .Select(rune => rune.InstanceId)
                .ToHashSet();
            HashSet<string> currentHandRuneIds = runes
                .Where(rune => rune?.Definition != null)
                .Select(rune => rune.InstanceId)
                .ToHashSet();

            List<string> staleMappings = _playerHandSlotByRuneId.Keys
                .Where(id => !currentHandRuneIds.Contains(id) && !selectedRuneIds.Contains(id))
                .ToList();
            foreach (string staleId in staleMappings)
            {
                _playerHandSlotByRuneId.Remove(staleId);
            }

            bool[] occupiedSlots = new bool[playerHandElementSpots.Length];

            foreach (RuneInstance rune in runes)
            {
                if (rune?.Definition == null || !_playerHandSlotByRuneId.TryGetValue(rune.InstanceId, out int mappedIndex))
                {
                    continue;
                }

                if (mappedIndex >= 0 && mappedIndex < occupiedSlots.Length)
                {
                    occupiedSlots[mappedIndex] = true;
                }
            }

            foreach (RuneInstance rune in runes)
            {
                if (rune?.Definition == null)
                {
                    continue;
                }

                if (_selectedRunes.Any(selected => selected?.InstanceId == rune.InstanceId))
                {
                    continue;
                }

                int slotIndex = GetOrAssignPlayerHandSlotIndex(rune, occupiedSlots);
                if (slotIndex < 0 || slotIndex >= playerHandElementSpots.Length)
                {
                    continue;
                }

                RuneSpot3D spot = playerHandElementSpots[slotIndex];
                if (spot == null)
                {
                    continue;
                }

                desiredCards.Add(new DesiredCardState
                {
                    Rune = rune,
                    Owner = BoardCardOwner.Player,
                    Location = BoardCardLocation.Hand,
                    Selected = false,
                    Interactable = true,
                    Target = spot.Anchor
                });
                spot.SetOccupied(true);
            }
        }

        private int GetOrAssignPlayerHandSlotIndex(RuneInstance rune, bool[] occupiedSlots)
        {
            if (rune?.Definition == null || occupiedSlots == null)
            {
                return -1;
            }

            if (_playerHandSlotByRuneId.TryGetValue(rune.InstanceId, out int existingIndex) &&
                existingIndex >= 0 &&
                existingIndex < occupiedSlots.Length)
            {
                return existingIndex;
            }

            for (int index = 0; index < occupiedSlots.Length; index++)
            {
                if (occupiedSlots[index])
                {
                    continue;
                }

                occupiedSlots[index] = true;
                _playerHandSlotByRuneId[rune.InstanceId] = index;
                return index;
            }

            return -1;
        }

        private void AddPlayerUpcomingDesiredCards(List<DesiredCardState> desiredCards, IReadOnlyList<RuneInstance> runes)
        {
            AddHandDesiredCards(
                desiredCards,
                runes,
                playerUpcomingRuneSpots,
                BoardCardOwner.Player,
                BoardCardLocation.Upcoming,
                false);
        }

        private void AddHandDesiredCards(
            List<DesiredCardState> desiredCards,
            IReadOnlyList<RuneInstance> runes,
            RuneSpot3D[] spots,
            BoardCardOwner owner,
            bool interactable)
        {
            AddHandDesiredCards(
                desiredCards,
                runes,
                spots,
                owner,
                BoardCardLocation.Hand,
                interactable);
        }

        private void AddHandDesiredCards(
            List<DesiredCardState> desiredCards,
            IReadOnlyList<RuneInstance> runes,
            RuneSpot3D[] spots,
            BoardCardOwner owner,
            BoardCardLocation location,
            bool interactable)
        {
            if (runes == null || spots == null)
            {
                return;
            }

            int count = Mathf.Min(runes.Count, spots.Length);
            for (int index = 0; index < count; index++)
            {
                RuneInstance rune = runes[index];
                RuneSpot3D spot = spots[index];
                if (rune?.Definition == null || spot == null)
                {
                    continue;
                }

                desiredCards.Add(new DesiredCardState
                {
                    Rune = rune,
                    Owner = owner,
                    Location = location,
                    Selected = false,
                    Interactable = interactable,
                    Target = spot.Anchor
                });
                spot.SetOccupied(true);
            }
        }

        private void AddModifierDesiredCard(
            List<DesiredCardState> desiredCards,
            ModifierInstance modifier,
            RuneSpot3D spot,
            BoardCardOwner owner,
            BoardCardLocation location,
            bool selected,
            bool interactable)
        {
            if (modifier?.Definition == null || spot == null)
            {
                return;
            }

            desiredCards.Add(new DesiredCardState
            {
                Modifier = modifier,
                Owner = owner,
                Location = location,
                Selected = selected,
                Interactable = interactable,
                Target = spot.Anchor
            });
            spot.SetOccupied(true);
        }

        private void AddSelectedRuneDesiredCards(List<DesiredCardState> desiredCards)
        {
            if (ritualPedestal == null)
            {
                return;
            }

            for (int index = 0; index < _selectedRunes.Length; index++)
            {
                RuneInstance rune = _selectedRunes[index];
                RuneSpot3D spot = ritualPedestal.GetElementalSpot(index);
                if (rune?.Definition == null || spot == null)
                {
                    continue;
                }

                desiredCards.Add(new DesiredCardState
                {
                    Rune = rune,
                    Owner = BoardCardOwner.Player,
                    Location = BoardCardLocation.Ritual,
                    Selected = true,
                    Interactable = true,
                    Target = spot.Anchor
                });
                spot.SetOccupied(true);
            }
        }

        private void AddSelectedModifierDesiredCard(List<DesiredCardState> desiredCards)
        {
            if (_selectedModifier?.Definition == null || ritualPedestal == null)
            {
                return;
            }

            RuneSpot3D spot = ritualPedestal.GetModifierSpot();
            if (spot == null)
            {
                return;
            }

            desiredCards.Add(new DesiredCardState
            {
                Modifier = _selectedModifier,
                Owner = BoardCardOwner.Player,
                Location = BoardCardLocation.Ritual,
                Selected = true,
                Interactable = true,
                Target = spot.Anchor
            });
            spot.SetOccupied(true);
        }

        private RuneCard3D FindSpawnedCard(string instanceId, BoardCardOwner owner, BoardCardLocation location, bool isModifier)
        {
            return _spawnedCards.FirstOrDefault(card =>
                card != null &&
                card.Owner == owner &&
                card.Location == location &&
                card.IsModifier == isModifier &&
                ((isModifier && card.BoundModifier?.InstanceId == instanceId) ||
                 (!isModifier && card.BoundRune?.InstanceId == instanceId)));
        }

        private int FindFirstEmptyRitualSlot()
        {
            for (int index = 0; index < _selectedRunes.Length; index++)
            {
                if (_selectedRunes[index]?.Definition == null)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindSelectedRuneIndex(RuneInstance rune)
        {
            if (rune == null)
            {
                return -1;
            }

            for (int index = 0; index < _selectedRunes.Length; index++)
            {
                if (_selectedRunes[index]?.InstanceId == rune.InstanceId)
                {
                    return index;
                }
            }

            return -1;
        }

        private int GetReturnHandIndex(RuneInstance returningRune)
        {
            BattleState state = battleController != null ? battleController.CurrentState : null;
            if (state?.PlayerHand?.AvailableRunes == null || returningRune == null)
            {
                return 0;
            }

            if (_playerHandSlotByRuneId.TryGetValue(returningRune.InstanceId, out int mappedIndex) &&
                IsValidPlayerHandSlot(mappedIndex))
            {
                return mappedIndex;
            }

            int fallbackIndex = state.PlayerHand.AvailableRunes.FindIndex(rune => rune?.InstanceId == returningRune.InstanceId);
            fallbackIndex = FindAvailableReturnSlot(returningRune, fallbackIndex);
            _playerHandSlotByRuneId[returningRune.InstanceId] = fallbackIndex;
            return fallbackIndex;
        }

        private int FindAvailableReturnSlot(RuneInstance returningRune, int preferredIndex)
        {
            if (playerHandElementSpots == null || playerHandElementSpots.Length == 0)
            {
                return 0;
            }

            bool[] occupiedSlots = new bool[playerHandElementSpots.Length];
            foreach (KeyValuePair<string, int> mapping in _playerHandSlotByRuneId)
            {
                if (mapping.Key == returningRune.InstanceId || !IsValidPlayerHandSlot(mapping.Value))
                {
                    continue;
                }

                occupiedSlots[mapping.Value] = true;
            }

            if (preferredIndex >= 0 &&
                preferredIndex < occupiedSlots.Length &&
                !occupiedSlots[preferredIndex])
            {
                return preferredIndex;
            }

            for (int index = 0; index < occupiedSlots.Length; index++)
            {
                if (!occupiedSlots[index])
                {
                    return index;
                }
            }

            return Mathf.Clamp(preferredIndex, 0, occupiedSlots.Length - 1);
        }

        private bool IsValidPlayerHandSlot(int index)
        {
            return playerHandElementSpots != null &&
                   index >= 0 &&
                   index < playerHandElementSpots.Length;
        }

        private IEnumerator AnimateUpcomingRuneToHand(RuneCard3D card, Transform target)
        {
            if (card == null || target == null)
            {
                yield break;
            }

            card.SetIdleEnabled(false);
            card.SetInteractable(false);
            card.SetVisualScale(card.GetLocationScale(BoardCardLocation.Upcoming));
            RegisterCardFlight(card);
            _activeFlightCount++;
            RefreshButtons();

            yield return PlayWorldFlight(
                card.transform,
                target,
                card.GetLocationScale(BoardCardLocation.Upcoming),
                card.GetLocationScale(BoardCardLocation.Hand),
                card.SetVisualScale,
                false,
                card.GetPoseRotation(target));

            card.SetLocation(BoardCardLocation.Hand);
            card.SetSelected(false);
            card.SetInteractable(true);
            UnregisterCardFlight(card);
            _activeFlightCount = Mathf.Max(0, _activeFlightCount - 1);
            FinalizeFlightVisualState();
        }

        private IEnumerator AnimateEnemyRunesToPedestal(RitualBuild build)
        {
            if (build == null || ritualPedestal == null)
            {
                yield break;
            }

            for (int index = 0; index < build.SelectedRunes.Count && index < 3; index++)
            {
                RuneInstance rune = build.SelectedRunes[index];
                if (rune?.Definition == null)
                {
                    continue;
                }

                RuneCard3D card = FindSpawnedCard(rune.InstanceId, BoardCardOwner.Enemy, BoardCardLocation.Hand, false);
                RuneSpot3D targetSpot = ritualPedestal.GetElementalSpot(index);
                if (card == null || targetSpot == null)
                {
                    continue;
                }

                TriggerEnemyRunePressAnimation();
                card.SetIdleEnabled(false);
                card.SetInteractable(false);
                StartCoroutine(PlayEnemyRuneToPedestal(card, targetSpot));

                yield return WaitForSecondsRealtimeSafe(enemyRunePlacementStepDelay);
            }

            yield return WaitForSecondsRealtimeSafe(cardMoveDuration);
        }

        private IEnumerator PlayEnemyRuneToPedestal(RuneCard3D card, RuneSpot3D targetSpot)
        {
            if (card == null || targetSpot == null)
            {
                yield break;
            }

            yield return PlayWorldFlight(
                    card.transform,
                    targetSpot.Anchor,
                    card.GetLocationScale(BoardCardLocation.Hand),
                    card.GetLocationScale(BoardCardLocation.Ritual),
                    card.SetVisualScale,
                    true,
                    card.GetPoseRotation(targetSpot.Anchor));
            card.SetLocation(BoardCardLocation.Ritual);
            card.SetInteractable(false);
        }

        private IEnumerator AnimateEnemyModifierToPedestal(ModifierInstance modifier)
        {
            if (modifier?.Definition == null || ritualPedestal == null)
            {
                yield break;
            }

            RuneCard3D card = FindSpawnedCard(modifier.InstanceId, BoardCardOwner.Enemy, BoardCardLocation.Hand, true);
            RuneSpot3D targetSpot = ritualPedestal.GetModifierSpot();
            if (card == null || targetSpot == null)
            {
                yield break;
            }

            card.SetIdleEnabled(false);
            card.SetInteractable(false);
            yield return PlayWorldFlight(
                card.transform,
                targetSpot.Anchor,
                card.GetLocationScale(BoardCardLocation.Hand),
                card.GetLocationScale(BoardCardLocation.Ritual),
                card.SetVisualScale,
                true,
                card.GetPoseRotation(targetSpot.Anchor));
            card.SetLocation(BoardCardLocation.Ritual);
            card.SetInteractable(false);
        }

        private IEnumerator AnimateRuneToRitual(RuneCard3D card, RuneInstance rune, int targetSlotIndex)
        {
            RuneSpot3D targetSpot = ritualPedestal != null ? ritualPedestal.GetElementalSpot(targetSlotIndex) : null;
            if (card == null || rune?.Definition == null || targetSpot == null)
            {
                yield break;
            }

            if (debugFlightLogs)
            {
                Debug.Log(
                    $"[BattleBoard3D] Rune to ritual | Rune={rune.Definition.DisplayName} | Slot={targetSlotIndex} | From={card.transform.position} | To={targetSpot.Anchor.position} | Duration={cardMoveDuration}",
                    this);
            }

            _selectedRunes[targetSlotIndex] = rune;
            _lastValidation = null;
            _lastPreview = null;
            card.SetIdleEnabled(false);
            card.SetInteractable(false);
            RegisterCardFlight(card);
            _activeFlightCount++;
            RecalculatePreview();
            RefreshButtons();
            yield return PlayWorldFlight(
                card.transform,
                targetSpot.Anchor,
                card.GetLocationScale(BoardCardLocation.Hand),
                card.GetLocationScale(BoardCardLocation.Ritual),
                card.SetVisualScale,
                true,
                card.GetPoseRotation(targetSpot.Anchor));
            card.SetLocation(BoardCardLocation.Ritual);
            card.SetSelected(true);
            card.SetInteractable(true);
            UnregisterCardFlight(card);
            _activeFlightCount = Mathf.Max(0, _activeFlightCount - 1);
            FinalizeFlightVisualState();
        }

        private IEnumerator AnimateRuneToHand(RuneCard3D card, RuneInstance rune, int sourceSlotIndex)
        {
            int handIndex = GetReturnHandIndex(rune);
            RuneSpot3D targetSpot = playerHandElementSpots != null && handIndex >= 0 && handIndex < playerHandElementSpots.Length
                ? playerHandElementSpots[handIndex]
                : null;

            if (card == null || rune?.Definition == null || targetSpot == null)
            {
                yield break;
            }

            if (debugFlightLogs)
            {
                Debug.Log(
                    $"[BattleBoard3D] Rune to hand | Rune={rune.Definition.DisplayName} | Slot={sourceSlotIndex} | From={card.transform.position} | To={targetSpot.Anchor.position} | Duration={cardMoveDuration}",
                    this);
            }

            if (sourceSlotIndex >= 0 && sourceSlotIndex < _selectedRunes.Length &&
                _selectedRunes[sourceSlotIndex]?.InstanceId == rune.InstanceId)
            {
                _selectedRunes[sourceSlotIndex] = null;
            }

            _lastValidation = null;
            _lastPreview = null;
            card.SetIdleEnabled(false);
            card.SetInteractable(false);
            RegisterCardFlight(card);
            _activeFlightCount++;
            RecalculatePreview();
            RefreshButtons();
            yield return PlayWorldFlight(
                card.transform,
                targetSpot.Anchor,
                card.GetLocationScale(BoardCardLocation.Ritual),
                card.GetLocationScale(BoardCardLocation.Hand),
                card.SetVisualScale,
                false,
                card.GetPoseRotation(targetSpot.Anchor));
            card.SetLocation(BoardCardLocation.Hand);
            card.SetSelected(false);
            card.SetInteractable(true);
            UnregisterCardFlight(card);
            _activeFlightCount = Mathf.Max(0, _activeFlightCount - 1);
            FinalizeFlightVisualState();
        }

        private IEnumerator AnimateModifierToRitual(RuneCard3D card, ModifierInstance modifier)
        {
            RuneSpot3D targetSpot = ritualPedestal != null ? ritualPedestal.GetModifierSpot() : null;
            if (card == null || modifier?.Definition == null || targetSpot == null)
            {
                yield break;
            }

            if (debugFlightLogs)
            {
                Debug.Log(
                    $"[BattleBoard3D] Modifier to ritual | Modifier={modifier.Definition.DisplayName} | From={card.transform.position} | To={targetSpot.Anchor.position} | Duration={cardMoveDuration}",
                    this);
            }

            _selectedModifier = modifier;
            _lastValidation = null;
            _lastPreview = null;
            card.SetIdleEnabled(false);
            card.SetInteractable(false);
            RegisterCardFlight(card);
            _activeFlightCount++;
            RecalculatePreview();
            RefreshButtons();
            yield return PlayWorldFlight(
                card.transform,
                targetSpot.Anchor,
                card.GetLocationScale(BoardCardLocation.Hand),
                card.GetLocationScale(BoardCardLocation.Ritual),
                card.SetVisualScale,
                true,
                card.GetPoseRotation(targetSpot.Anchor));
            card.SetLocation(BoardCardLocation.Ritual);
            card.SetSelected(true);
            card.SetInteractable(true);
            UnregisterCardFlight(card);
            _activeFlightCount = Mathf.Max(0, _activeFlightCount - 1);
            FinalizeFlightVisualState();
        }

        private IEnumerator AnimateModifierToHand(RuneCard3D card, ModifierInstance modifier)
        {
            if (card == null || modifier?.Definition == null || playerModifierSpot == null)
            {
                yield break;
            }

            if (debugFlightLogs)
            {
                Debug.Log(
                    $"[BattleBoard3D] Modifier to hand | Modifier={modifier.Definition.DisplayName} | From={card.transform.position} | To={playerModifierSpot.Anchor.position} | Duration={cardMoveDuration}",
                    this);
            }

            if (_selectedModifier?.InstanceId == modifier.InstanceId)
            {
                _selectedModifier = null;
            }

            _lastValidation = null;
            _lastPreview = null;
            card.SetIdleEnabled(false);
            card.SetInteractable(false);
            RegisterCardFlight(card);
            _activeFlightCount++;
            RecalculatePreview();
            RefreshButtons();
            yield return PlayWorldFlight(
                card.transform,
                playerModifierSpot.Anchor,
                card.GetLocationScale(BoardCardLocation.Ritual),
                card.GetLocationScale(BoardCardLocation.Hand),
                card.SetVisualScale,
                false,
                card.GetPoseRotation(playerModifierSpot.Anchor));
            card.SetLocation(BoardCardLocation.Hand);
            card.SetSelected(false);
            card.SetInteractable(true);
            UnregisterCardFlight(card);
            _activeFlightCount = Mathf.Max(0, _activeFlightCount - 1);
            FinalizeFlightVisualState();
        }

        private IEnumerator PlayWorldFlight(
            Transform movingTransform,
            Transform target,
            Vector3 startScale,
            Vector3 endScale,
            System.Action<Vector3> applyScale,
            bool playLandingSound = true,
            Quaternion? endRotationOverride = null)
        {
            if (movingTransform == null || target == null)
            {
                yield break;
            }

            Vector3 startPosition = movingTransform.position;
            Quaternion startRotation = movingTransform.rotation;
            Vector3 endPosition = target.position;
            Quaternion endRotation = endRotationOverride ?? target.rotation;
            float totalDuration = Mathf.Max(0.01f, cardMoveDuration);
            float clampedLiftRatio = Mathf.Clamp01(liftPhaseRatio);
            float clampedLandRatio = Mathf.Clamp01(landPhaseRatio);
            float travelRatio = Mathf.Max(0.05f, 1f - clampedLiftRatio - clampedLandRatio);
            float normalizedLiftEnd = clampedLiftRatio;
            float normalizedTravelEnd = clampedLiftRatio + travelRatio;
            Vector3 peakStart = startPosition + Vector3.up * flightArcHeight;
            Vector3 peakEnd = endPosition + Vector3.up * flightArcHeight;
            float elapsed = 0f;

            audioController?.PlayRuneFly(startPosition);

            if (debugFlightLogs)
            {
                Debug.Log(
                    $"[BattleBoard3D] Flight start | Object={movingTransform.name} | Start={startPosition} | PeakStart={peakStart} | PeakEnd={peakEnd} | End={endPosition} | TotalDuration={totalDuration}",
                    this);
            }

            while (elapsed < totalDuration)
            {
                if (movingTransform == null || target == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / totalDuration);
                float eased = EaseOutCubic(t);
                Vector3 position;

                if (eased <= normalizedLiftEnd)
                {
                    float localT = normalizedLiftEnd <= 0.0001f ? 1f : eased / normalizedLiftEnd;
                    position = Vector3.Lerp(startPosition, peakStart, EaseOutCubic(localT));
                }
                else if (eased <= normalizedTravelEnd)
                {
                    float localT = travelRatio <= 0.0001f ? 1f : (eased - normalizedLiftEnd) / travelRatio;
                    position = Vector3.Lerp(peakStart, peakEnd, localT);
                }
                else
                {
                    float landRatio = Mathf.Max(0.0001f, 1f - normalizedTravelEnd);
                    float localT = (eased - normalizedTravelEnd) / landRatio;
                    position = Vector3.Lerp(peakEnd, endPosition, EaseInCubic(localT));
                }

                movingTransform.position = position;
                movingTransform.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
                applyScale?.Invoke(Vector3.Lerp(startScale, endScale, eased));

                if (debugFlightLogs)
                {
                    Debug.Log(
                        $"[BattleBoard3D] Flight tick | Object={movingTransform.name} | t={t:0.00} | eased={eased:0.00} | Pos={position}",
                        this);
                }

                yield return null;
            }

            if (movingTransform == null || target == null)
            {
                yield break;
            }

            movingTransform.SetPositionAndRotation(endPosition, endRotation);
            applyScale?.Invoke(endScale);
            if (playLandingSound)
            {
                audioController?.PlayRunePlace(endPosition);
            }

            if (debugFlightLogs)
            {
                Debug.Log(
                    $"[BattleBoard3D] Flight end | Object={movingTransform.name} | FinalPos={movingTransform.position}",
                    this);
            }
        }

        private void DestroySpawnedCards()
        {
            foreach (RuneCard3D card in _spawnedCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _spawnedCards.Clear();
            _cardFlightKeys.Clear();
        }

        private void RegisterCardFlight(RuneCard3D card)
        {
            string key = GetCardFlightKey(card);
            if (!string.IsNullOrWhiteSpace(key))
            {
                _cardFlightKeys.Add(key);
            }
        }

        private void UnregisterCardFlight(RuneCard3D card)
        {
            string key = GetCardFlightKey(card);
            if (!string.IsNullOrWhiteSpace(key))
            {
                _cardFlightKeys.Remove(key);
            }
        }

        private bool IsCardInFlight(RuneCard3D card)
        {
            string key = GetCardFlightKey(card);
            return !string.IsNullOrWhiteSpace(key) && _cardFlightKeys.Contains(key);
        }

        private static string GetCardFlightKey(RuneCard3D card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            string instanceId = card.IsModifier
                ? card.BoundModifier?.InstanceId
                : card.BoundRune?.InstanceId;
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return string.Empty;
            }

            return $"{card.Owner}:{(card.IsModifier ? "Modifier" : "Rune")}:{instanceId}";
        }

        private void FinalizeFlightVisualState()
        {
            if (HasActiveFlights)
            {
                RefreshButtons();
                return;
            }

            RecalculatePreview();
            RefreshButtons();
        }

        private void ClearSpotOccupancy()
        {
            SetSpotsOccupied(playerHandElementSpots, false);
            SetSpotsOccupied(playerUpcomingRuneSpots, false);
            SetSpotsOccupied(enemyHandElementSpots, false);
            playerModifierSpot?.SetOccupied(false);
            enemyModifierSpot?.SetOccupied(false);
            ritualPedestal?.ClearOccupancy();
        }

        private static void SetSpotsOccupied(IEnumerable<RuneSpot3D> spots, bool occupied)
        {
            if (spots == null)
            {
                return;
            }

            foreach (RuneSpot3D spot in spots)
            {
                spot?.SetOccupied(occupied);
            }
        }
    }
}
