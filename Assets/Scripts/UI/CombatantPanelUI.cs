using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RunicTower.Data.Runtime;

namespace RunicTower.UI
{
    public sealed class CombatantPanelUI : MonoBehaviour
    {
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private Image manaFillImage;
        [SerializeField] private TMP_Text manaLabel;
        [SerializeField] private GameObject shieldBadge;
        [SerializeField] private Image shieldBadgeIcon;
        [SerializeField] private TMP_Text shieldBadgeValue;
        [SerializeField] private Sprite shieldStatusIcon;
        [SerializeField] private GameObject regenerationBadge;
        [SerializeField] private Image regenerationBadgeIcon;
        [SerializeField] private TMP_Text regenerationBadgeValue;
        [SerializeField] private Sprite regenerationStatusIcon;
        [SerializeField] private GameObject burnBadge;
        [SerializeField] private Image burnBadgeIcon;
        [SerializeField] private TMP_Text burnBadgeValue;
        [SerializeField] private Sprite burnStatusIcon;

        public void Render(CombatantState state)
        {
            if (state == null)
            {
                Clear();
                return;
            }

            SetFill(healthFillImage, state.CurrentHp, state.MaxHp);
            SetText(healthLabel, $"HP: {state.CurrentHp}/{state.MaxHp}");
            SetFill(manaFillImage, state.CurrentMana, state.MaxMana);
            SetText(manaLabel, $"Mana: {state.CurrentMana}/{state.MaxMana}");
            SetStatusBadge(shieldBadge, shieldBadgeIcon, shieldBadgeValue, shieldStatusIcon, state.Shield);
            SetStatusBadge(regenerationBadge, regenerationBadgeIcon, regenerationBadgeValue, regenerationStatusIcon, state.GetDisplayedRegeneration());
            SetStatusBadge(burnBadge, burnBadgeIcon, burnBadgeValue, burnStatusIcon, state.GetDisplayedBurn());
        }

        public void Clear()
        {
            SetFill(healthFillImage, 0, 1);
            SetText(healthLabel, "HP: -");
            SetFill(manaFillImage, 0, 1);
            SetText(manaLabel, "Mana: -");
            SetStatusBadge(shieldBadge, shieldBadgeIcon, shieldBadgeValue, shieldStatusIcon, 0);
            SetStatusBadge(regenerationBadge, regenerationBadgeIcon, regenerationBadgeValue, regenerationStatusIcon, 0);
            SetStatusBadge(burnBadge, burnBadgeIcon, burnBadgeValue, burnStatusIcon, 0);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static void SetFill(Image image, int current, int max)
        {
            if (image == null)
            {
                return;
            }

            float fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            image.fillAmount = fillAmount;
        }

        private static void SetStatusBadge(GameObject badgeRoot, Image badgeIcon, TMP_Text badgeValue, Sprite iconSprite, int value)
        {
            if (badgeRoot != null)
            {
                badgeRoot.SetActive(value > 0);
            }

            if (badgeIcon != null)
            {
                badgeIcon.sprite = iconSprite;
                badgeIcon.enabled = iconSprite != null && value > 0;
            }

            if (badgeValue != null)
            {
                badgeValue.text = value.ToString();
            }
        }
    }
}
