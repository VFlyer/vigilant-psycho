using System.Collections;
using System.Collections.Generic;
using System.Linq;

public struct PermutationGenerator {

	public static List<IEnumerable<int>> GenerateFactorialPermutations(IEnumerable<int> inputs)
    {
        var output = new List<IEnumerable<int>>() { new int[0] };
        for (var x = 0; x < inputs.Count(); x++)
        {
            var nextOutput = new List<IEnumerable<int>>();
            foreach (var curOutput in output)
            {
                var possibleCombinations = inputs.Where(a => !curOutput.Contains(a));
                foreach (var item in possibleCombinations)
                {
                    var newCombo = curOutput.Concat(new[] { item }).ToArray();
                    nextOutput.Add(newCombo);
                }
            }
            output = nextOutput;
        }

        return output;
    }
}
