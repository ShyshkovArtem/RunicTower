using System.Collections.Generic;
using System.Linq;
using RunicTower.Data.Runtime;

namespace RunicTower.Services
{
    public sealed class DeckService
    {
        public List<RuneInstance> CloneDeck(IEnumerable<RuneInstance> source)
        {
            return source
                .Where(rune => rune?.Definition != null)
                .Select(rune => new RuneInstance(rune.Definition))
                .ToList();
        }

        public List<RuneInstance> Shuffle(IEnumerable<RuneInstance> source, System.Random random)
        {
            List<RuneInstance> result = source.ToList();

            for (int index = result.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (result[index], result[swapIndex]) = (result[swapIndex], result[index]);
            }

            return result;
        }
    }
}
