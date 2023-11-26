using System.Security.Policy;

namespace WPF_project.Implementations
{
    public class Result
    {
        public int ID { get; private set; }
        public string Text { get; set; }
        public float CurrentValue { get; private set; } = 0;
        public List<(int QuestionID, bool IsAnswerYes, float IncreasingValue)> Impacts { get; private set; } = new();

        public Result(int id, string text)
        {
            ID = id;
            Text = text;
        }
    
        public void ResetCurrentValue()
        {
            CurrentValue = 0;
        }

        public void Impact(int questionID, bool isAnswerYes)
        {
            foreach (var impact in Impacts)
            {
                if (impact.QuestionID == questionID
                    && impact.IsAnswerYes == isAnswerYes)
                {
                    CurrentValue += impact.IncreasingValue;
                    if (CurrentValue > 1)
                        CurrentValue = 1;
                    return;
                }
            }
        }
    
        public void AddOrUpdateImpact(int questionID, bool isAnswerYes, float increasingValue)
        {
            var oldImpactIndex = Impacts.FindIndex(x => x.QuestionID == questionID);
            if (oldImpactIndex < 0)
            {
                Impacts.Add((questionID, isAnswerYes, increasingValue));
            }
            else
            {
                Impacts[oldImpactIndex] = (questionID, isAnswerYes, increasingValue);
            }
        }
    
        public void RemoveImpact(int questionID) 
        {
            var oldImpactIndex = Impacts.FindIndex(x => x.QuestionID == questionID);
            if (oldImpactIndex < 0)
                return;

            Impacts.RemoveAt(oldImpactIndex);
        }

        public override string ToString()
        {
            return $"{Text} (ID: {ID})";
        }
    }
}
