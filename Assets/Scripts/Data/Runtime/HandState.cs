using System;
using System.Collections.Generic;

namespace RunicTower.Data.Runtime
{
    [Serializable]
    public sealed class HandState
    {
        private const int UpcomingPreviewSize = 2;

        public List<RuneInstance> AvailableRunes = new();
        public List<RuneInstance> DrawQueue = new();
        public ModifierInstance ActiveModifier;

        public IReadOnlyList<RuneInstance> UpcomingRunes
        {
            get
            {
                if (DrawQueue == null)
                {
                    return Array.Empty<RuneInstance>();
                }

                return DrawQueue.GetRange(0, Math.Min(UpcomingPreviewSize, DrawQueue.Count));
            }
        }

        public void ClearTransientDraw()
        {
            AvailableRunes.Clear();
            DrawQueue.Clear();
            ActiveModifier = null;
        }
    }
}
