namespace WPF_project.Implementations
{
    public class Question
    {
        public int ID { get; private set; }
        public string Text { get; set; }

        public Question(int id, string text)
        {
            ID = id;
            Text = text;
        }

        public override string ToString()
        {
            return $"{Text} (ID: {ID})";
        }
    }
}
