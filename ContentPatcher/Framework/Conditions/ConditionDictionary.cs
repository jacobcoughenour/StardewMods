using System.Collections.Generic;

namespace ContentPatcher.Framework.Conditions
{
    /// <summary>A set of conditions that can be checked against the context.</summary>
    internal class ConditionDictionary : Dictionary<ConditionKey, Condition>
    {
        /*********
        ** Properties
        *********/
        /// <summary>The valid condition values.</summary>
        private readonly IDictionary<ConditionKey, HashSet<string>> ValidValues;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="validValues">The valid condition values.</param>
        public ConditionDictionary(IDictionary<ConditionKey, HashSet<string>> validValues)
        {
            this.ValidValues = validValues;
        }

        /// <summary>Get the explicit values for a condition, or the implied range of values if not explicitly set.</summary>
        /// <param name="key">The condition key.</param>
        public IEnumerable<string> GetImpliedValues(ConditionKey key)
        {
            // explicit values
            if (this.TryGetValue(key, out Condition condition))
            {
                foreach (string value in condition.Values)
                    yield return value;
            }

            // implied range
            else
            {
                foreach (string value in this.ValidValues[key])
                    yield return value;
            }
        }
    }
}
