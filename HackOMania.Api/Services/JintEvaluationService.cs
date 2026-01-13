using Jint;

namespace HackOMania.Api.Services;

public interface IJintEvaluationService
{
    bool EvaluateStatement(string statement, Dictionary<string, object> parameters);
}

public class JintEvaluationService : IJintEvaluationService
{
    public bool EvaluateStatement(string statement, Dictionary<string, object> parameters)
    {
        try
        {
            var engine = new Engine();

            // Set all parameters in the Jint engine
            foreach (var param in parameters)
            {
                engine.SetValue(param.Key, param.Value);
            }

            // Evaluate the statement
            var result = engine.Evaluate(statement);

            // Convert to boolean
            return result.AsBoolean();
        }
        catch
        {
            // If evaluation fails, default to false for safety
            return false;
        }
    }
}
