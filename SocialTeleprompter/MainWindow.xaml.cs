using Microsoft.CognitiveServices.SpeechRecognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Text.RegularExpressions;

namespace SocialTeleprompter
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsRecognizing;
        private MicrophoneRecognitionClient micClient;

        public MainWindow()
        {
            InitializeComponent();
            RecognizeResult.FontSize *= 2;
            RecognizeResult2.FontSize *= 2;
            WordHint.FontSize *= 2;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsRecognizing) {
                StartButton.Content = "Start";
                micClient.EndMicAndRecognition();
                micClient.Dispose();
                micClient = null;
            }
            else
            {
                StartButton.Content = "Stop";
                micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(SpeechRecognitionMode.LongDictation, "en-US", ApiKey1.Text);
                micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
                micClient.OnResponseReceived += OnMicDictationResponseReceivedHandler;
                micClient.StartMicAndRecognition();
            }
            IsRecognizing = !IsRecognizing;
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Dispatcher.Invoke(
            (Action)(() =>
            {
                RecognizeResult.Document.Blocks.Clear();
                RecognizeResult.AppendText(e.PartialResult);
                RecognizeResult.SelectAll();
                RecognizeResult.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                RecognizeResult.Selection.Select(RecognizeResult.Document.ContentEnd, RecognizeResult.Document.ContentEnd);
            }));
        }

        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke(
                    (Action)(() =>
                    {

                        this.micClient.EndMicAndRecognition();
                    }));
            }
            Dispatcher.Invoke(
                    (Action)(() =>
                    {
                        RecognizeResult.Document.Blocks.Clear();
                        if (e.PhraseResponse.Results.Length > 0)
                        {
                            RecognizeResult.AppendText(e.PhraseResponse.Results[0].DisplayText);
                        }
                        RecognizeResult.SelectAll();
                        RecognizeResult.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                        RecognizeResult.Selection.Select(RecognizeResult.Document.ContentEnd, RecognizeResult.Document.ContentEnd);
                    }));
            if (e.PhraseResponse.Results.Length > 0)
            {
                Task.Run(() =>
                {
                    {
                        using (var wc = new WebClient())
                        {
                            wc.Headers.Add("Ocp-Apim-Subscription-Key", "b53f42d2ebb64913861d26cfe65da32a");
                            var res = wc.UploadString("https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases", "{\"documents\":[{\"language\":\"en\",\"id\":\"1\",\"text\":\"?\"}]})".Replace("?", e.PhraseResponse.Results[0].DisplayText));
                            res = Regex.Match(res, "\"keyPhrases\":\\[(.*?)\\]").Groups[1].Value;
                            Dispatcher.Invoke(
        (Action)(() =>
        {
            RecognizeResult2.Document.Blocks.Clear();
            RecognizeResult.Selection.Select(RecognizeResult.Document.ContentStart, RecognizeResult.Document.ContentEnd);
            RecognizeResult2.AppendText(RecognizeResult.Selection.Text);
            RecognizeResult.Selection.Select(RecognizeResult.Document.ContentEnd, RecognizeResult.Document.ContentEnd);
            foreach (Match match in Regex.Matches(res, "\"(.+?)\""))
            {
                var word = match.Groups[1].Value;
                var si = e.PhraseResponse.Results[0].DisplayText.IndexOf(word);
                var sp = GetPoint(RecognizeResult2.Document.ContentStart,si);
                var ep = GetPoint(sp, word.Length);
                RecognizeResult2.Selection.Select(sp, ep);
                RecognizeResult2.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Colors.Yellow));
            }
        }));
                        }
                    }
                });

            }
        }
        private static TextPointer GetPoint(TextPointer start, int x)
        {
            var ret = start;
            var i = 0;
            while (i < x && ret != null)
            {
                if (ret.GetPointerContext(LogicalDirection.Backward) ==
        TextPointerContext.Text ||
                    ret.GetPointerContext(LogicalDirection.Backward) ==
        TextPointerContext.None)
                    i++;
                if (ret.GetPositionAtOffset(1,
        LogicalDirection.Forward) == null)
                    return ret;
                ret = ret.GetPositionAtOffset(1,
        LogicalDirection.Forward);
            }
            return ret;
        }
    }
}
