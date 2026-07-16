using Agrovesto.Backend.Infrastructure.Entities.Businesses.MandE;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services.MandE
{
    public interface IMandEFormulaEngine
    {
        decimal Evaluate(string formula, Dictionary<string, decimal> values);
        void Validate(
        string formula,
        string fieldKey,
        IEnumerable<MandEDataFieldDefinition> existingFields
    );
    }


    public class MandEFormulaEngine : IMandEFormulaEngine
    {
        private static readonly Regex TokenRegex =
        new(@"\b[a-zA-Z_][a-zA-Z0-9_]*\b", RegexOptions.Compiled);

        public void Validate(
        string formula,
        string fieldKey,
        IEnumerable<MandEDataFieldDefinition> fields)
        {
            var keys = fields.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var referencedKeys = TokenRegex.Matches(formula)
                .Select(m => m.Value)
                .Where(k => !decimal.TryParse(k, out _))
                .ToList();

            // Self-reference
            if (referencedKeys.Contains(fieldKey, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Formula cannot reference itself.");

            // Missing fields
            var missing = referencedKeys.Where(k => !keys.Contains(k)).Distinct().ToList();
            if (missing.Any())
                throw new InvalidOperationException($"Unknown fields in formula: {string.Join(", ", missing)}");

            // Circular dependency
            var computedFields = fields
                .Where(f => f.IsComputed)
                .ToDictionary(f => f.Key, f => f.Formula!);

            DetectCircular(fieldKey, formula, computedFields);
        }

        private void DetectCircular(
            string newKey,
            string formula,
            Dictionary<string, string> computed)
        {
            var visited = new HashSet<string>();

            void Visit(string key)
            {
                if (!computed.ContainsKey(key)) return;
                if (!visited.Add(key))
                    throw new InvalidOperationException("Circular formula dependency detected.");

                var deps = Regex.Matches(computed[key], @"\b[a-zA-Z_]\w*\b")
                    .Select(m => m.Value);

                foreach (var dep in deps)
                    Visit(dep);
            }

            computed[newKey] = formula;
            Visit(newKey);
        }
        public decimal Evaluate(string formula, Dictionary<string, decimal> values)
        {
            var expr = formula;

            foreach (var kv in values)
            {
                expr = Regex.Replace(
                    expr,
                    $@"\b{kv.Key}\b",
                    kv.Value.ToString(CultureInfo.InvariantCulture)
                );
            }

            var table = new DataTable();
            return Convert.ToDecimal(table.Compute(expr, ""));
        }

    }
}
