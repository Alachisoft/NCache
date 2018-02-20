using Alachisoft.NCache.EntityFrameworkCore.NCache;
using System.Linq.Expressions;
using System.Text;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    class OQLQueryAnalyzer : QueryAnalyzer
    {
        protected internal OQLQueryAnalyzer(Expression rootNode) : base(rootNode) { }

        internal override ValidationResult ValidateQuery()
        {
            bool valid = true;
            StringBuilder reasonBuilder = new StringBuilder();

            valid = valid && (WasThisUsed("orderbydescending") ? CheckOrderByDescending(reasonBuilder) : true);
            valid = valid && (WasThisUsed("orderby") ? CheckOrderBy(reasonBuilder) : true);
            valid = valid && (WasThisUsed("groupby") ? CheckGroupBy(reasonBuilder) : true);
            valid = valid && (WasThisUsed("select") ? CheckSelect(reasonBuilder) : true);

            ValidationResult result = new ValidationResult
            {
                IsValid = valid,
                Reason = reasonBuilder.ToString()
            };

            Logger.Log(
                "OQL Query Validation Result: " + result.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return result;
        }

        private bool WasThisUsed(string functionName) => functions[functionName] > 0;

        private bool CheckGroupBy(StringBuilder reasonBuilder) => BasicImplementation(reasonBuilder, "GROUP BY cannot be used without its argument being projected.", "select");

        private bool CheckOrderBy(StringBuilder reasonBuilder)
        {
            if (functions["orderbydescending"] > 0)
            {
                reasonBuilder.AppendLine("The ORDER BY operation (no matter ascending or descending) can be used once only.");
                return false;
            }

            return BasicImplementation(reasonBuilder, "ORDER BY cannot be used without GROUP BY and its argument being projected.", "groupby", "select");
        }

        private bool CheckOrderByDescending(StringBuilder reasonBuilder)
        {
            if (functions["orderby"] > 0)
            {
                reasonBuilder.AppendLine("The ORDER BY operation (no matter ascending or descending) can be used once only.");
                return false;
            }

            return BasicImplementation(reasonBuilder, "ORDER BY cannot be used without GROUP BY and its argument being projected.", "groupby", "select");
        }

        private bool CheckSelect(StringBuilder reasonBuilder)
        {
            bool isValid = true;

            if (multipleProjectionFlag)
            {
                isValid = false;
                reasonBuilder.AppendLine("Multiple projections are not supported.");
            }
            else
            {
                isValid = functions["groupby"] > 0 && (functions["min"] > 0 || functions["max"] > 0 ||
                functions["sum"] > 0 || functions["count"] > 0 || functions["average"] > 0);

                if (!isValid)
                {
                    reasonBuilder.AppendLine("Projection can only be used with GROUP BY operation and an aggregate function.");
                }
            }
            return isValid;
        }

        private bool BasicImplementation(StringBuilder reasonBuilder, string reason, params string[] functionNames)
        {
            bool isValid = true;

            foreach (string functionName in functionNames)
            {
                isValid = isValid && (functions[functionName] > 0);
            }
            if (!isValid)
            {
                reasonBuilder.AppendLine(reason);
            }
            return isValid;
        }
    }
}
