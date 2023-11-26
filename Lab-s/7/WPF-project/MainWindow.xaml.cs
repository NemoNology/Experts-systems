using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using WPF_project.Implementations;

namespace WPF_project;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ResetProject(true);
        saveFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
        saveFileDialog.DefaultExt = ".xml";
        outputQuestions.ItemsSource = questions;
        outputResults.ItemsSource = results;
        inputQuestion.ItemsSource = questions;
        inputResult.ItemsSource = results;
        questions.CollectionChanged += OnQuestionsOrResultsChanged;
        results.CollectionChanged += OnQuestionsOrResultsChanged;
        OnQuestionsOrResultsChanged(null, null!);
    }

    readonly SaveFileDialog saveFileDialog = new();
    readonly ObservableCollection<Question> questions = new();
    readonly ObservableCollection<Result> results = new();
    int questionCounter;
    List<(int, int, bool, float)> impacts
    {
        get
        {
            List<(int, int, bool, float)> res = new();
            foreach (var result in results)
            {
                foreach ((int QuestionID, bool IsAnswerYes, float IncreasingValue) in result.Impacts)
                {
                    res.Add((result.ID, QuestionID, IsAnswerYes, IncreasingValue));
                }
            }

            return res;
        }
    }
    List<(string, float)> possibleResults
    {
        get
        {
            List<(string, float)> values = new();
            foreach (var result in results)
            {
                values.Add((result.Text, result.CurrentValue));
            }
            return values;
        }
    }

    private void OnQuestionsOrResultsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        outputImpacts.ItemsSource = impacts;
        outputPossibleResults.ItemsSource = possibleResults;
    }

    private void ResetProject(bool isDefaultProject = false)
    {
        questions.Clear();
        results.Clear();
        if (isDefaultProject)
        {
            questions.Add(new Question(0, "Win?"));
            results.Add(new Result(0, "Win"));
            results.Add(new Result(1, "Lose"));
            results[0].AddOrUpdateImpact(0, true, 1);
            results[1].AddOrUpdateImpact(0, false, 1);
        }
    }

    private void OnNewButton_Click(object sender, ExecutedRoutedEventArgs e)
    {
        if (MessageBox.Show(
            "If continue all unsaved date will be lost.\nContinue?",
            "Warning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            ResetProject();
            saveFileDialog.FileName = string.Empty;
        }
    }

    private void OnOpenButton_Click(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog()
        {
            Filter = saveFileDialog.Filter,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() is true)
        {
            if (MessageBox.Show(
                "If continue all unsaved date will be lost.\nContinue?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    GetProjectFromXMLDocument(dialog.FileName);
                    saveFileDialog.FileName = dialog.FileName;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetProject(true);
                }
            }
        }
    }

    private void OnSaveButton_Click(object sender, ExecutedRoutedEventArgs e)
    {
        if (saveFileDialog.FileName != string.Empty)
        {
            GetXMLDocument().Save(saveFileDialog.FileName);
        }
        else
        {
            if (saveFileDialog.ShowDialog() is true)
            {
                GetXMLDocument().Save(saveFileDialog.FileName);
            }
        }
    }

    private void OnSaveAsButton_Click(object sender, ExecutedRoutedEventArgs e)
    {
        if (saveFileDialog.ShowDialog() is true)
        {
            GetXMLDocument().Save(saveFileDialog.FileName);
        }
    }

    private void OnRunRestartClick(object sender, RoutedEventArgs e)
    {
        foreach (var result in results)
        {
            result.ResetCurrentValue();
        }
        OnQuestionsOrResultsChanged(null, null!);

        if (questions.Count > 0)
        {
            questionCounter = 0;
            outputCurrentQuestion.Text = questions[questionCounter].Text;
            outputExpertSystem.Visibility = Visibility.Visible;
        }
    }

    private XmlDocument GetXMLDocument()
    {
        XmlDocument doc = new();
        var root = doc.CreateElement("ExpertSystem");
        var qs = doc.CreateElement("Questions");
        var rs = doc.CreateElement("Results");
        foreach (var q in questions)
        {
            var qBuffer = doc.CreateElement("Question");
            var idBuffer = doc.CreateAttribute("ID");
            var textBuffer = doc.CreateAttribute("Text");
            idBuffer.Value = q.ID.ToString();
            textBuffer.Value = q.Text;
            qBuffer.Attributes.Append(idBuffer);
            qBuffer.Attributes.Append(textBuffer);
            qs.AppendChild(qBuffer);
        }
        foreach (var r in results)
        {
            var rBuffer = doc.CreateElement("Result");
            var idBuffer = doc.CreateAttribute("ID");
            var textBuffer = doc.CreateAttribute("Text");
            idBuffer.Value = r.ID.ToString();
            textBuffer.Value = r.Text;
            rBuffer.Attributes.Append(idBuffer);
            rBuffer.Attributes.Append(textBuffer);
            foreach (var i in r.Impacts)
            {
                var iBuffer = doc.CreateElement("Impact");
                var qIDBuffer = doc.CreateAttribute("QuestionID");
                var aBuffer = doc.CreateAttribute("Answer");
                var incValueBuffer = doc.CreateAttribute("IncreaseValue");
                qIDBuffer.Value = i.QuestionID.ToString();
                aBuffer.Value = i.IsAnswerYes.ToString();
                incValueBuffer.Value = i.IncreasingValue.ToString();
                iBuffer.Attributes.Append(qIDBuffer);
                iBuffer.Attributes.Append(aBuffer);
                iBuffer.Attributes.Append(incValueBuffer);
                rBuffer.AppendChild(iBuffer);
            }
            rs.AppendChild(rBuffer);
        }
        root.AppendChild(qs);
        root.AppendChild(rs);
        doc.AppendChild(root);
        return doc;
    }

    private void GetProjectFromXMLDocument(string filePath)
    {
        ResetProject();
        var doc = new XmlDocument();
        doc.Load(filePath);
        var root = doc.FirstChild;
        if (root is null)
        {
            throw new ArgumentException("XML document must contains root element");
        }
        else if (root.ChildNodes.Count != 2)
        {
            throw new ArgumentException("XML document root element must contains only questions and results");
        }

        var qs = root.FirstChild;
        var rs = root.LastChild;
        int id;
        if (qs is not null)
        {
            foreach (XmlNode question in qs)
            {
                if (question.NodeType is not XmlNodeType.Element)
                {
                    throw new ArgumentException(
                        $"Node {question.Name} must be XML element");
                }
                else if (question.Attributes!.Count < 2)
                {
                    throw new ArgumentException(
                        $"Node {question.Name} must contains at least 2 attributes: ID, text");
                }
                else if (!int.TryParse(question.Attributes![0].Value, out id))
                {
                    throw new ArgumentException(
                        $"Node {question.Name} must have valid integer ID, but have \'{question.Attributes![0].Value}\'");
                }

                questions.Add(new Question(id, question.Attributes![1].Value));
            }
        }
        if (rs is not null)
        {
            foreach (XmlNode result in rs)
            {
                if (result.NodeType is not XmlNodeType.Element)
                {
                    throw new ArgumentException(
                        $"Node {result.Name} must be XML element");
                }
                else if (result.Attributes!.Count < 2)
                {
                    throw new ArgumentException(
                        $"Node {result.Name} must contains at least 2 attributes: ID, Text");
                }
                else if (!int.TryParse(result.Attributes![0].Value, out id))
                {
                    throw new ArgumentException(
                        $"Node {result.Name} must have valid integer ID, but have \'{result.Attributes![0].Value}\'");
                }

                var resultBuffer = new Result(id, result.Attributes![1].Value);
                results.Add(resultBuffer);
                bool answerBuffer;
                float incValueBuffer;
                foreach (XmlNode impact in result.ChildNodes)
                {
                    if (impact.NodeType is not XmlNodeType.Element)
                    {
                        throw new ArgumentException($"Node {impact.Name} must be XML element");
                    }
                    else if (impact.Attributes!.Count < 3)
                    {
                        throw new ArgumentException(
                            $"Node {impact.Name} must contains at least 3 attributes: " +
                            "question ID, answer (0/1), increasing value (0.0 - 1.0)");
                    }
                    else if (!int.TryParse(impact.Attributes![0].Value, out id))
                    {
                        throw new ArgumentException(
                            $"Node {impact.Name} must have valid integer ID, but have \'{impact.Attributes![0].Value}\'");
                    }
                    else if (!bool.TryParse(impact.Attributes![1].Value, out answerBuffer))
                    {
                        throw new ArgumentException(
                            $"Node {impact.Name} must have valid bool answer, but have \'{impact.Attributes![1].Value}\'");
                    }
                    else if (!float.TryParse(
                        impact.Attributes![2].Value,
                        System.Globalization.NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out incValueBuffer))
                    {
                        throw new ArgumentException(
                            $"Node {impact.Name} must have valid float increase value, but have \'{impact.Attributes![2].Value}\'");
                    }

                    resultBuffer.AddOrUpdateImpact(id, answerBuffer, incValueBuffer);
                }
            }
        }

        OnQuestionsOrResultsChanged(null, null!);
    }

    private void OnAddQuestionClick(object sender, RoutedEventArgs e)
    {
        questions.Add(new Question(questions.Count < 1 ? 0 : questions.Last().ID + 1, inputQuestionText.Text));
    }

    private void OnRemoveSelectedQuestionClick(object sender, RoutedEventArgs e)
    {
        if (outputQuestions.SelectedIndex < 0)
            return;

        questions.RemoveAt(outputQuestions.SelectedIndex);
    }

    private void OnRemoveSelectedResultClick(object sender, RoutedEventArgs e)
    {
        if (outputResults.SelectedIndex < 0)
            return;

        results.RemoveAt(outputResults.SelectedIndex);
    }

    private void OnRemoveSelectedImpactClick(object sender, RoutedEventArgs e)
    {
        if (outputImpacts.SelectedIndex < 0)
            return;

        var (rID, qID, answer, incValue) = ((int, int, bool, float))outputImpacts.SelectedItem;
        results.First(x => x.ID == rID).RemoveImpact(qID);
    }

    private void OnImpactAddClick(object sender, RoutedEventArgs e)
    {
        if (inputQuestion.SelectedIndex < 0
            || InputAnswer.IsChecked is null
            || inputResult.SelectedIndex < 0)
            return;

        results[inputResult.SelectedIndex].AddOrUpdateImpact(
                questions[inputQuestion.SelectedIndex].ID,
                (bool)InputAnswer.IsChecked,
                (float)inputIncreaseValue.Value);
    }

    private void OnAddResultClick(object sender, RoutedEventArgs e)
    {
        results.Add(new Result(results.Count < 1 ? 0 : results.Last().ID + 1, inputResultText.Text));
    }

    private void OnQuestionsSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (outputQuestions.SelectedIndex < 0)
        {
            inputQuestionText.Text = "?";
            return;
        }

        inputQuestionText.Text = questions[outputQuestions.SelectedIndex].Text;
    }

    private void OnResultsSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (outputResults.SelectedIndex < 0)
        {
            inputResultText.Text = "!";
            return;
        }

        inputResultText.Text = results[outputResults.SelectedIndex].Text;
    }

    private void OnQuestionTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (outputQuestions.SelectedIndex < 0)
            return;

        questions[outputQuestions.SelectedIndex].Text = inputQuestionText.Text;
    }

    private void OnResultTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (outputResults.SelectedIndex < 0)
            return;
        results[outputResults.SelectedIndex].Text = inputResultText.Text;
    }

    private void OnAnswerClick(object sender, RoutedEventArgs e)
    {
        bool answer = ((Button)sender).Content is "Yes";
        foreach (var result in results)
        {
            result.Impact(questions[questionCounter].ID, answer);
        }

        OnQuestionsOrResultsChanged(null, null!);
        questionCounter += 1;
        if (questionCounter >= questions.Count)
        {
            outputExpertSystem.Visibility = Visibility.Collapsed;
            return;
        }

        outputCurrentQuestion.Text = questions[questionCounter].Text;
    }
}