using System;
using System.Collections.Generic;
using RunicTower.Core;
using RunicTower.Data.Definitions;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class CombatantState
    {
        public string DisplayName = string.Empty;
        public int MaxHp;
        public int CurrentHp;
        public int MaxMana;
        public int CurrentMana;
        public int ManaRegenPerTurn;
        public int Shield;
        public int ShieldRoundsRemaining;
        public string ShieldVfxKey = string.Empty;
        public bool HasShieldElement;
        public ElementType ShieldElement;
        public int Regeneration;
        public int RegenerationRoundsRemaining;
        public int PendingRegeneration;
        public int PendingRegenerationRounds;
        public int DefenseBreak;
        public int DefenseBreakRoundsRemaining;
        public int DamageBoost;
        public int DamageBoostRoundsRemaining;
        public int DamageWeakness;
        public int DamageWeaknessRoundsRemaining;
        public int Burn;
        public int BurnTicksRemaining;
        public int PendingBurn;
        public int PendingBurnTicks;
        public bool IsPlayer;
        public EnemyDefinition EnemyDefinition;
        public List<RuneInstance> Deck = new();

        public bool IsAlive => CurrentHp > 0;

        public void ClampResources()
        {
            if (CurrentHp > MaxHp)
            {
                CurrentHp = MaxHp;
            }

            if (CurrentMana > MaxMana)
            {
                CurrentMana = MaxMana;
            }

            if (CurrentHp < 0)
            {
                CurrentHp = 0;
            }

            if (CurrentMana < 0)
            {
                CurrentMana = 0;
            }
        }

        public void RestoreManaForTurn()
        {
            CurrentMana += ManaRegenPerTurn;
            ClampResources();
        }

        public void PromotePendingStatuses()
        {
            if (PendingRegeneration > 0)
            {
                Regeneration += PendingRegeneration;
                RegenerationRoundsRemaining = Math.Max(RegenerationRoundsRemaining, PendingRegenerationRounds);
                PendingRegeneration = 0;
                PendingRegenerationRounds = 0;
            }

            if (PendingBurn > 0)
            {
                Burn = Math.Max(Burn, PendingBurn);
                BurnTicksRemaining = Math.Max(BurnTicksRemaining, PendingBurnTicks);
                PendingBurn = 0;
                PendingBurnTicks = 0;
            }

            ClampResources();
        }

        public void ApplyEndOfRoundStatuses()
        {
            if (Regeneration > 0 && RegenerationRoundsRemaining > 0)
            {
                CurrentHp += Regeneration;
                RegenerationRoundsRemaining--;
                if (RegenerationRoundsRemaining <= 0)
                {
                    Regeneration = 0;
                }
            }

            if (Burn > 0 && BurnTicksRemaining > 0)
            {
                CurrentHp -= Burn;
                BurnTicksRemaining--;
                if (BurnTicksRemaining <= 0)
                {
                    Burn = 0;
                }
            }

            if (DamageBoost > 0 && DamageBoostRoundsRemaining > 0)
            {
                DamageBoostRoundsRemaining--;
                if (DamageBoostRoundsRemaining <= 0)
                {
                    DamageBoost = 0;
                }
            }

            if (DamageWeakness > 0 && DamageWeaknessRoundsRemaining > 0)
            {
                DamageWeaknessRoundsRemaining--;
                if (DamageWeaknessRoundsRemaining <= 0)
                {
                    DamageWeakness = 0;
                }
            }

            if (DefenseBreak > 0 && DefenseBreakRoundsRemaining > 0)
            {
                DefenseBreakRoundsRemaining--;
                if (DefenseBreakRoundsRemaining <= 0)
                {
                    DefenseBreak = 0;
                }
            }

            if (Shield > 0 && ShieldRoundsRemaining > 0)
            {
                ShieldRoundsRemaining--;
                if (ShieldRoundsRemaining <= 0)
                {
                    Shield = 0;
                    ClearShieldState();
                }
            }

            ClampResources();
        }

        public void AddShield(
            int amount,
            int rounds,
            string shieldVfxKey = "",
            bool hasShieldElement = false,
            ElementType shieldElement = default)
        {
            if (amount <= 0)
            {
                return;
            }

            bool canStack = Shield <= 0 ||
                            (HasShieldElement &&
                             hasShieldElement &&
                             ShieldElement == shieldElement);

            if (canStack)
            {
                Shield += amount;
                ShieldRoundsRemaining = ShieldRoundsRemaining > 0
                    ? ShieldRoundsRemaining + 1
                    : Math.Max(rounds, 0);
            }
            else
            {
                Shield = amount;
                ShieldRoundsRemaining = Math.Max(rounds, 0);
                ShieldVfxKey = string.Empty;
                HasShieldElement = false;
            }

            if (!string.IsNullOrWhiteSpace(shieldVfxKey))
            {
                ShieldVfxKey = shieldVfxKey.Trim();
            }

            if (hasShieldElement)
            {
                HasShieldElement = true;
                ShieldElement = shieldElement;
            }

            if (hasShieldElement && shieldElement == ElementType.Water)
            {
                ClearBurn();
            }
        }

        public void ClearShieldVfxIfEmpty()
        {
            if (Shield <= 0)
            {
                ClearShieldState();
            }
        }

        private void ClearShieldState()
        {
            ShieldVfxKey = string.Empty;
            HasShieldElement = false;
        }

        public void AddDamageBoost(int amount, int rounds)
        {
            if (amount <= 0)
            {
                return;
            }

            DamageBoost += amount;
            DamageBoostRoundsRemaining = Math.Max(rounds, 0);
        }

        public void AddDamageWeakness(int amount, int rounds)
        {
            if (amount <= 0)
            {
                return;
            }

            DamageWeakness = Math.Min(50, DamageWeakness + amount);
            DamageWeaknessRoundsRemaining = Math.Max(rounds, 0);
        }

        public void AddDefenseBreak(int amount, int rounds)
        {
            if (amount <= 0)
            {
                return;
            }

            DefenseBreak = Math.Min(50, DefenseBreak + amount);
            DefenseBreakRoundsRemaining = Math.Max(rounds, 0);
        }

        public void ClearDefenseBreak()
        {
            DefenseBreak = 0;
            DefenseBreakRoundsRemaining = 0;
        }

        public void AddPendingBurn(int amount, int ticks)
        {
            if (amount <= 0 || HasActiveShieldElement(ElementType.Water))
            {
                return;
            }

            int activeBurn = Math.Max(Burn, PendingBurn);
            PendingBurn = Math.Min(5, activeBurn + amount);
            PendingBurnTicks = Math.Max(ticks, 0);
        }

        public void ExtendBurn(int rounds)
        {
            if (rounds <= 0 || (Burn <= 0 && PendingBurn <= 0))
            {
                return;
            }

            if (BurnTicksRemaining > 0)
            {
                BurnTicksRemaining += rounds;
            }

            if (PendingBurnTicks > 0)
            {
                PendingBurnTicks += rounds;
            }
        }

        public void ApplyImmediateHealing(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int missingHp = Math.Max(0, MaxHp - CurrentHp);
            int restoredHp = Math.Min(missingHp, amount);

            CurrentHp += restoredHp;
            ClampResources();
        }

        public int GetDisplayedRegeneration()
        {
            return Regeneration + PendingRegeneration;
        }

        public int GetDisplayedBurn()
        {
            return Math.Max(Burn, PendingBurn);
        }

        public bool HasActiveShieldElement(ElementType element)
        {
            return Shield > 0 && HasShieldElement && ShieldElement == element;
        }

        public void ClearBurn()
        {
            Burn = 0;
            BurnTicksRemaining = 0;
            PendingBurn = 0;
            PendingBurnTicks = 0;
        }
    }
}
