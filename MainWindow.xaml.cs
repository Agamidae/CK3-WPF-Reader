using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech.Synthesis;
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.DirectoryServices.ActiveDirectory;


public class SpeechExample
{
    // The _synthesizer variable is declared at the class level, allowing it to be accessed by any method within the class.
    private SpeechSynthesizer _synthesizer;


    // Constructor
    public SpeechExample()
    {
        _synthesizer = new SpeechSynthesizer();
    }

    // Public property, This property returns the _synthesizer instance, allowing external classes to access it.
    public SpeechSynthesizer Synthesizer
    {
        get { return _synthesizer; }
    }

    // Method to speak a given text
    public void Speak(string text)
    {
        if (_synthesizer != null)
        {
            _synthesizer.Speak(text);
        }
    }
}

namespace CK3_WPF_Reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        int rate = Properties.Settings.Default.Rate;
        int volume = Properties.Settings.Default.Volume;
        int PercentRate = 0;
        string TextRate = "";
        string TextVolume = "";
        string voice = "";
        bool launched = false;
        bool reading = true;

        private SpeechExample _speechExample;

        private Thread _udpThread;

        string documents = "";
        string logPath = "";
        string errorLog = "";
        string debugLog = "";

        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();

            DebugButton.IsChecked = (Properties.Settings.Default.log == "debug");
            ErrorButton.IsChecked = (Properties.Settings.Default.log == "error");
            rate = Properties.Settings.Default.Rate;
            volume = Properties.Settings.Default.Volume;
            volumeSlider.Value = Properties.Settings.Default.Volume;
            speechSlider.Value = Properties.Settings.Default.Rate;

            DisplayRate();

            _speechExample = new SpeechExample();
            _speechExample.Synthesizer.Rate = rate;
            _speechExample.Synthesizer.Volume = volume;

            _speechExample.Synthesizer.SetOutputToDefaultAudioDevice();
            int voiceCounter = -1;

            foreach (InstalledVoice voice in _speechExample.Synthesizer.GetInstalledVoices())
            {
                VoiceBox.Items.Add(voice.VoiceInfo.Name);
                voiceCounter++;

                if (Properties.Settings.Default.Voice.Length > 0)
                {
                    if (Properties.Settings.Default.Voice == voice.VoiceInfo.Name)
                    {
                        VoiceBox.SelectedIndex = voiceCounter;
                    }
                }
                else
                {
                    VoiceBox.SelectedIndex = 0;
                }
            }

            if (Properties.Settings.Default.Voice.Length > 0 )
            {
                _speechExample.Synthesizer.SelectVoice(Properties.Settings.Default.Voice);
            }

            _speechExample.Synthesizer.SpeakAsync("Launching CK3 Reader");

            launched = true;

            string documents = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string logPath = documents + "\\Paradox Interactive\\Crusader Kings III\\logs\\";
            string errorLog = logPath + "error.log";
            string debugLog = logPath + "debug.log";

            if (System.IO.File.Exists(errorLog))
            {
                lblStatus.Text = "✔️ ready, reading "+ Properties.Settings.Default.log + " log";
                lblStatus.Foreground = Brushes.Green;

            }

            Loaded += MainWindow_Loaded; // Subscribe to the Loaded event
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => RunLoop(_cancellationTokenSource.Token));
        }

        private async void Restart_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => RunLoop(_cancellationTokenSource.Token));
        }

        private void RunLoop(CancellationToken token)
        {
            string counter = string.Empty;
            string line = "";
            string documents = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string logPath = documents + "\\Paradox Interactive\\Crusader Kings III\\logs\\";
            string errorLog = logPath + "error.log";
            string debugLog = logPath + "debug.log";
            string beginPattern = "<event-text>";
            string endPattern = "</event-text>";
            string stopReading = "<stop reading>";
            string eventText = "";
            bool startMessage = false;
            string[] formatting = [
                @"\bL\b",
                @"TOOLTIP:\w+,\w+,\d+",
                @"ONCLICK:\w+,\d+",
                @"ONCLICK:\w+,\w+",
                @"TOOLTIP:\w+,\w+",
                @"TOOLTIP:\w+,\d+",
                @"positive_value",
                @"negative_value",
                @"COLOR_\w_\w",
                @"COLOR_\w",
                @"_icon",
                @"skill_",
                @"\w+ ",
                @"\w;",
                @"\w+",
                @". ",
                @".",
                @"!",
                @"\w;",
                @";",
                @"stress_\w+",
                @"_",
                "   ",
                "  "
            ];


            // Update the variable

            using (FileStream stream = new FileStream(errorLog, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                StreamReader reader = new StreamReader(stream);
                stream.Seek(0, SeekOrigin.End);

     
                //long fileLength = stream.Length;
                //if (fileLength == 0)
                //{
                //    Console.WriteLine("The file is empty.");
                //    return;
                //}

                //// Start from the end of the file
                //long fileLength = stream.Length;


                //// Buffer to hold the current line
                //StringBuilder currentLine = new StringBuilder();
                //int lineCount = 0;

                while (true)
                {
                    line = reader.ReadLine();

                    counter += ".";
                    if (counter.Length > 6)
                    {
                        counter = string.Empty;
                    }


                    // Check if cancellation is requested
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (line != null)
                    {
                        if (line.Contains("<stop reading>"))
                        {
                            StopSpeech();
                        }
                        else
                        {
                            if (line.Contains(beginPattern))
                            {
                                eventText = string.Empty;
                                startMessage = true;   
                            }
                            if (startMessage == true)
                            {
                                eventText += Regex.Replace(line, @".*\<event-text\>", "");

                                foreach (var format in formatting)
                                {
                                    eventText = Regex.Replace(eventText, format, " ");
                                }
                                if (line.EndsWith(endPattern) || eventText.Length > 1000)
                                {
                                    eventText = eventText.Replace(endPattern, "");
                                    startMessage = false;
                                    _speechExample.Synthesizer.SpeakAsync(eventText);
                                }
                            }
                            // Update the UI
                            Dispatcher.Invoke(() =>
                            {
                                txtLastLine.Text = "Last read line: " + line;
                                txtEvent.Text = "Event: " + eventText;
                            });
                        }

                    }
     
                    if (startMessage == false)
                    {
                        Thread.Sleep(Properties.Settings.Default.refresh);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        txtCounter.Text = counter;
                    });
                }
            }

            if (eventText.Length > 10000)
            {
                startMessage = false;
            }

            //string[] lines = System.IO.File.ReadAllLines(errorLog);
            //if (lines.Length > 0)
            //{
            //    line = lines[lines.Length-1];
            //}

        }

        // Optional: You can add a method to stop the loop if needed
        private void StopLoop()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void DisplayRate()
        {
            PercentRate = rate * 10 + 100;
            TextRate = "Speech Rate: " + PercentRate.ToString() + "%";
            SpeechRate.Content = TextRate;
        }

        public void DisplayVolume()
        {
            TextVolume = "Speech Volume: " + volume.ToString() + "%";
            SpeechVolume.Content = TextVolume;
        }

        public void StopSpeech()
        {
            _speechExample.Synthesizer.SpeakAsyncCancelAll();
            reading = false;
            //_speechExample.Synthesizer.Dispose();
        }

        // controls

        

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopSpeech();
            //StopButton.IsEnabled = (_speechExample.Synthesizer.State.ToString() == "Speaking");
        }

        public void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopSpeech();
            StopLoop();
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {

            _speechExample.Synthesizer.SpeakAsyncCancelAll();

            _speechExample.Synthesizer.SpeakAsync("Crusader Kings 3");

        }

        public void Error_radio(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.log = "error";
            Properties.Settings.Default.Save();
            ErrorButton.IsChecked = (Properties.Settings.Default.log == "error");
            lblStatus.Text = "✔️ ready, reading " + Properties.Settings.Default.log + " log";
            lblStatus.Foreground = Brushes.Green;
        }

        public void Debug_radio(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.log = "debug";
            Properties.Settings.Default.Save();
            DebugButton.IsChecked = (Properties.Settings.Default.log == "debug");
            lblStatus.Text = "✔️ ready, reading " + Properties.Settings.Default.log + " log";
            lblStatus.Foreground = Brushes.Green;
        }

        private void speechSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            rate = (int)e.NewValue;
            Properties.Settings.Default.Rate = rate;
            Properties.Settings.Default.Save();
            DisplayRate();
            if (_speechExample != null)
            {
                _speechExample.Synthesizer.Rate = rate;
                _speechExample.Synthesizer.SpeakAsyncCancelAll();
                _speechExample.Synthesizer.SpeakAsync(PercentRate.ToString());
            }
        }
        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volume = (int)e.NewValue;
            Properties.Settings.Default.Volume = volume;
            Properties.Settings.Default.Save();
            DisplayVolume();
            if (_speechExample != null)
            {
                _speechExample.Synthesizer.Volume = volume;
                _speechExample.Synthesizer.SpeakAsyncCancelAll();
                _speechExample.Synthesizer.SpeakAsync(volume.ToString());
            }
        }

        public void VoiceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            voice = VoiceBox.SelectedItem.ToString();
            _speechExample.Synthesizer.SelectVoice(voice);
            if (launched == true)
            {
                _speechExample.Synthesizer.SpeakAsync(voice);
            }
            Properties.Settings.Default.Voice = voice;
            Properties.Settings.Default.Save();
            //synth.Dispose();

        }

        private void Window_closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopSpeech();
            _speechExample.Synthesizer.Dispose();
            StopLoop();
        }

        private void RefreshNormal_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.refresh = 100;
            Properties.Settings.Default.Save();
            RefreshNormal.IsChecked = (Properties.Settings.Default.refresh == 100);
        }

        private void RefreshFast_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.refresh = 10;
            Properties.Settings.Default.Save();
            RefreshFast.IsChecked = (Properties.Settings.Default.refresh == 10);
        }

        public void StopTalkingG(Object sender, ExecutedRoutedEventArgs e)
        {
            StopSpeech();
        }
    }
}